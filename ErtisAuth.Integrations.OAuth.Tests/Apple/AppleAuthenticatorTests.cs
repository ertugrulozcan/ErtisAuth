using System.Net;
using System.Text.Json;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Integrations.OAuth.Apple;
using ErtisAuth.Integrations.OAuth.Tests.Helpers;

namespace ErtisAuth.Integrations.OAuth.Tests.Apple;

/// <summary>
/// Apple / AppleNative login: the authorization code is exchanged at Apple's token endpoint (server to server, over TLS);
/// the identity (sub, email) must come from the id_token of that response, never from the client payload.
/// </summary>
public class AppleAuthenticatorTests
{
	#region Constants
	
	private const string TokenEndpoint = "https://appleid.apple.com/auth/token";
	private const string AppleIssuer = "https://appleid.apple.com";
	private const string ClientId = "com.example.app";
	private const string AppleSub = "000123.apple-sub-of-code-owner.0456";
	private const string AppleEmail = "code.owner@privaterelay.appleid.com";
	
	#endregion
	
	#region Fields
	
	private readonly OAuthTestServices _services = new();
	
	private readonly TestJwt _appleKey = new("apple-key-1");
	
	#endregion
	
	#region Helpers
	
	private IAppleAuthenticator Authenticator => this._services.Get<IAppleAuthenticator>();
	
	private static AppleProvider CreateAppleProvider()
	{
		return new AppleProvider
		{
			MembershipId = "5f8a1b2c3d4e5f6a7b8c9d00",
			IsActive = true,
			AppClientId = ClientId,
			TeamId = "TEAM123456",
			PrivateKeyId = "KEY1234567",
			PrivateKey = TestJwt.CreateApplePrivateKeyPem(),
			RedirectUri = "https://app.example.com/apple/callback"
		};
	}
	
	private static AppleNativeProvider CreateAppleNativeProvider()
	{
		return new AppleNativeProvider
		{
			MembershipId = "5f8a1b2c3d4e5f6a7b8c9d00",
			IsActive = true,
			AppClientId = ClientId,
			TeamId = "TEAM123456",
			PrivateKeyId = "KEY1234567",
			PrivateKey = TestJwt.CreateApplePrivateKeyPem(),
			RedirectUri = "https://app.example.com/apple/callback"
		};
	}
	
	/// <summary>
	/// The login payload as the client posts it (AppleLoginModel), with an id_token and user the client fully controls.
	/// </summary>
	private static AppleLoginRequestBase CreateClientRequest(string clientSub, string? clientEmail, bool isNative = false)
	{
		var clientIdToken = TestJwt.CreateUnsignedToken(new Dictionary<string, object>
		{
			["iss"] = AppleIssuer,
			["aud"] = ClientId,
			["sub"] = clientSub,
			["exp"] = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds()
		});
		
		var model = new AppleLoginModel
		{
			User = new AppleUserModel
			{
				Name = new AppleUserNameModel { FirstName = "Client", LastName = "Name" },
				EmailAddress = clientEmail
			},
			Authorization = new AppleUserAuthorizationModel
			{
				Code = "authorization-code",
				IdToken = clientIdToken
			}
		};
		
		return model.ToLoginRequest(isNative);
	}
	
	private void RespondWithAppleIdToken(IDictionary<string, object> claims, string audience = ClientId, string issuer = AppleIssuer, DateTime? expires = null)
	{
		var idToken = this._appleKey.CreateToken(issuer, audience, claims, expires);
		this._services.Handler.Respond(TokenEndpoint, JsonSerializer.Serialize(new
		{
			access_token = "apple-access-token",
			token_type = "Bearer",
			expires_in = 3600,
			refresh_token = "apple-refresh-token",
			id_token = idToken
		}));
	}
	
	private static Dictionary<string, object> AppleClaims(object? emailVerified = null)
	{
		var claims = new Dictionary<string, object>
		{
			["sub"] = AppleSub,
			["email"] = AppleEmail
		};
		
		if (emailVerified != null)
		{
			claims["email_verified"] = emailVerified;
		}
		
		return claims;
	}
	
	#endregion
	
	#region Code Exchange
	
	[Fact]
	public async Task VerifyTokenAsync_ExchangesTheCodeWithTheProviderCredentials()
	{
		this.RespondWithAppleIdToken(AppleClaims());
		var request = CreateClientRequest(AppleSub, AppleEmail);
		
		await this.Authenticator.VerifyTokenAsync(request, CreateAppleProvider(), TestContext.Current.CancellationToken);
		
		var exchange = this._services.Handler.SingleRequestTo(TokenEndpoint);
		Assert.Equal(HttpMethod.Post, exchange.Method);
		Assert.Equal("authorization-code", exchange.Form("code"));
		Assert.Equal(ClientId, exchange.Form("client_id"));
		Assert.Equal("authorization_code", exchange.Form("grant_type"));
		Assert.False(string.IsNullOrEmpty(exchange.Form("client_secret")));
	}
	
	[Fact]
	public async Task VerifyTokenAsync_WhenAppleRejectsTheCode_ThrowsUnauthorized()
	{
		this._services.Handler.Respond(TokenEndpoint, """{"error":"invalid_grant"}""", HttpStatusCode.BadRequest);
		var request = CreateClientRequest(AppleSub, AppleEmail);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.Authenticator.VerifyTokenAsync(request, CreateAppleProvider(), TestContext.Current.CancellationToken));
		
		Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
	}
	
	[Fact]
	public async Task VerifyTokenAsync_WithValidCode_Succeeds()
	{
		this.RespondWithAppleIdToken(AppleClaims());
		var request = CreateClientRequest(AppleSub, AppleEmail);
		
		Assert.True(await this.Authenticator.VerifyTokenAsync(request, CreateAppleProvider(), TestContext.Current.CancellationToken));
	}
	
	#endregion
	
	#region Errors Other Than A Rejected Code
	
	/// <summary>
	/// Apple not answering is not a rejected login: 503 ProviderUnavailable (the client can retry).
	/// </summary>
	[Fact]
	public async Task VerifyTokenAsync_WhenAppleIsUnreachable_ThrowsProviderUnavailable()
	{
		this._services.Handler.Fail(TokenEndpoint, new HttpRequestException("Name or service not known"));
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.Authenticator.VerifyTokenAsync(CreateClientRequest(AppleSub, AppleEmail), CreateAppleProvider(), TestContext.Current.CancellationToken));
		
		Assert.Equal("ProviderUnavailable", exception.ErrorCode);
		Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
	}
	
	[Theory]
	[InlineData(HttpStatusCode.InternalServerError, "{}")]
	[InlineData(HttpStatusCode.ServiceUnavailable, "")]
	[InlineData(HttpStatusCode.OK, "<html>maintenance</html>")]
	public async Task VerifyTokenAsync_WhenAppleFails_ThrowsProviderUnavailable(HttpStatusCode statusCode, string body)
	{
		this._services.Handler.Respond(TokenEndpoint, body, statusCode);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.Authenticator.VerifyTokenAsync(CreateClientRequest(AppleSub, AppleEmail), CreateAppleProvider(), TestContext.Current.CancellationToken));
		
		Assert.Equal("ProviderUnavailable", exception.ErrorCode);
	}
	
	/// <summary>
	/// Apple rejecting the provider's credentials is a configuration error, not a failed login of the user.
	/// </summary>
	[Theory]
	[InlineData("invalid_client")]
	[InlineData("unauthorized_client")]
	public async Task VerifyTokenAsync_WhenAppleRejectsTheClient_ThrowsProviderNotConfiguredCorrectly(string error)
	{
		this._services.Handler.Respond(TokenEndpoint, $$"""{"error":"{{error}}"}""", HttpStatusCode.BadRequest);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.Authenticator.VerifyTokenAsync(CreateClientRequest(AppleSub, AppleEmail), CreateAppleProvider(), TestContext.Current.CancellationToken));
		
		Assert.Equal("ProviderNotConfiguredCorrectly", exception.ErrorCode);
	}
	
	[Fact]
	public async Task VerifyTokenAsync_WithUnreadablePrivateKey_ThrowsProviderNotConfiguredCorrectly()
	{
		var provider = CreateAppleProvider();
		provider.PrivateKey = "-----BEGIN PRIVATE KEY-----\nnot a key\n-----END PRIVATE KEY-----";
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.Authenticator.VerifyTokenAsync(CreateClientRequest(AppleSub, AppleEmail), provider, TestContext.Current.CancellationToken));
		
		Assert.Equal("ProviderNotConfiguredCorrectly", exception.ErrorCode);
		Assert.Empty(this._services.Handler.Requests);
	}
	
	[Fact]
	public async Task VerifyTokenAsync_WithoutRedirectUri_ThrowsProviderNotConfiguredCorrectly()
	{
		var provider = CreateAppleProvider();
		provider.RedirectUri = null;
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.Authenticator.VerifyTokenAsync(CreateClientRequest(AppleSub, AppleEmail), provider, TestContext.Current.CancellationToken));
		
		Assert.Equal("ProviderNotConfiguredCorrectly", exception.ErrorCode);
	}
	
	#endregion
	
	#region Identity
	
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task VerifyTokenAsync_TakesTheIdentityFromApplesIdToken_NotFromTheClient(bool isNative)
	{
		// Attack: a valid code of the attacker's own Apple account + a forged client id_token / email of the victim
		this.RespondWithAppleIdToken(AppleClaims());
		var request = CreateClientRequest("victim-apple-sub", "victim@example.com", isNative);
		
		Provider provider = isNative ? CreateAppleNativeProvider() : CreateAppleProvider();
		var isVerified = await this.Authenticator.VerifyTokenAsync(request, provider, TestContext.Current.CancellationToken);
		
		Assert.True(isVerified);
		Assert.Equal(AppleSub, request.UserId);
		Assert.Equal(AppleEmail, request.EmailAddress);
	}
	
	[Fact]
	public async Task VerifyTokenAsync_KeepsTheNameSentByTheClient()
	{
		// Apple shares the name only once, with the client; the id_token has no name claims
		this.RespondWithAppleIdToken(AppleClaims());
		var request = CreateClientRequest(AppleSub, AppleEmail);
		
		await this.Authenticator.VerifyTokenAsync(request, CreateAppleProvider(), TestContext.Current.CancellationToken);
		
		Assert.Equal("Client", request.User?.FirstName);
		Assert.Equal("Name", request.User?.LastName);
	}
	
	[Fact]
	public async Task VerifyTokenAsync_WhenIdTokenHasNoEmail_DoesNotUseTheClientEmail()
	{
		this.RespondWithAppleIdToken(new Dictionary<string, object> { ["sub"] = AppleSub });
		var request = CreateClientRequest(AppleSub, "victim@example.com");
		
		Assert.True(await this.Authenticator.VerifyTokenAsync(request, CreateAppleProvider(), TestContext.Current.CancellationToken));
		
		Assert.Equal(AppleSub, request.UserId);
		Assert.Null(request.EmailAddress);
	}
	
	[Theory]
	[InlineData(true, true)]
	[InlineData("true", true)]
	[InlineData(false, false)]
	[InlineData("false", false)]
	[InlineData(null, false)]
	public async Task VerifyTokenAsync_TakesEmailVerifiedFromApplesIdToken(object? emailVerified, bool expected)
	{
		this.RespondWithAppleIdToken(AppleClaims(emailVerified));
		var request = CreateClientRequest(AppleSub, AppleEmail);
		
		await this.Authenticator.VerifyTokenAsync(request, CreateAppleProvider(), TestContext.Current.CancellationToken);
		
		Assert.Equal(expected, request.IsEmailVerified);
	}
	
	[Fact]
	public async Task VerifyTokenAsync_WithIdTokenForAnotherAudience_ReturnsFalse()
	{
		this.RespondWithAppleIdToken(AppleClaims(), audience: "com.attacker.app");
		var request = CreateClientRequest(AppleSub, AppleEmail);
		
		Assert.False(await this.Authenticator.VerifyTokenAsync(request, CreateAppleProvider(), TestContext.Current.CancellationToken));
	}
	
	[Fact]
	public async Task VerifyTokenAsync_WithIdTokenFromAnotherIssuer_ReturnsFalse()
	{
		this.RespondWithAppleIdToken(AppleClaims(), issuer: "https://attacker.example.com");
		var request = CreateClientRequest(AppleSub, AppleEmail);
		
		Assert.False(await this.Authenticator.VerifyTokenAsync(request, CreateAppleProvider(), TestContext.Current.CancellationToken));
	}
	
	[Fact]
	public async Task VerifyTokenAsync_WithExpiredIdToken_ReturnsFalse()
	{
		this.RespondWithAppleIdToken(AppleClaims(), expires: DateTime.UtcNow.AddHours(-1));
		var request = CreateClientRequest(AppleSub, AppleEmail);
		
		Assert.False(await this.Authenticator.VerifyTokenAsync(request, CreateAppleProvider(), TestContext.Current.CancellationToken));
	}
	
	#endregion
}