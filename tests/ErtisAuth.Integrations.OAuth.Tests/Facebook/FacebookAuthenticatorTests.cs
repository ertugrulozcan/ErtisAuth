using System.Net;
using System.Text.Json;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Integrations.OAuth.Facebook;
using ErtisAuth.Integrations.OAuth.Tests.Helpers;

namespace ErtisAuth.Integrations.OAuth.Tests.Facebook;

/// <summary>
/// Facebook login (classic access token and Limited Login JWT): the identity (id, email) must come from Facebook
/// (Graph API or the verified JWT), never from the client payload.
/// </summary>
public class FacebookAuthenticatorTests
{
	#region Constants
	
	private const string DebugTokenUrl = "https://graph.facebook.com/debug_token";
	private const string MeUrl = "https://graph.facebook.com/me";
	private const string JwksUrl = "https://www.facebook.com/.well-known/oauth/openid/jwks/";
	private const string FacebookIssuer = "https://www.facebook.com";
	private const string AppId = "our-facebook-app";
	private const string FacebookUserId = "10001234567890";
	private const string FacebookEmail = "token.owner@example.com";
	
	#endregion
	
	#region Fields
	
	private readonly OAuthTestServices _services = new();
	
	private readonly TestJwt _facebookKey1 = new("facebook-key-1");
	
	private readonly TestJwt _facebookKey2 = new("facebook-key-2");
	
	#endregion
	
	#region Helpers
	
	private IFacebookAuthenticator Authenticator => this._services.Get<IFacebookAuthenticator>();
	
	private static Provider CreateProvider()
	{
		return new FacebookProvider
		{
			MembershipId = "5f8a1b2c3d4e5f6a7b8c9d00",
			IsActive = true,
			AppClientId = AppId
		};
	}
	
	/// <summary>
	/// The login payload as the client posts it; every field of "user" is controlled by the client.
	/// </summary>
	private static FacebookLoginRequest CreateClientRequest(string userId, string email, string accessToken = "facebook-user-access-token", string appId = AppId, bool isLimited = false)
	{
		return new FacebookLoginRequest
		{
			AppId = appId,
			IsLimited = isLimited,
			User = new FacebookUserToken
			{
				Id = userId,
				FirstName = "Client",
				LastName = "Name",
				EmailAddress = email,
				AccessToken = accessToken
			}
		};
	}
	
	private void RespondWithDebugToken(bool isValid = true, string appId = AppId, string userId = FacebookUserId)
	{
		this._services.Handler.Respond(DebugTokenUrl, JsonSerializer.Serialize(new
		{
			data = new { app_id = appId, user_id = userId, is_valid = isValid, type = "USER" }
		}));
	}
	
	private void RespondWithProfile(string id = FacebookUserId, string? email = FacebookEmail)
	{
		this._services.Handler.Respond(MeUrl, JsonSerializer.Serialize(new
		{
			id,
			first_name = "Graph",
			last_name = "Profile",
			email,
			picture = new { data = new { url = "https://graph.example.com/picture.jpg" } }
		}));
	}
	
	private string CreateLimitedToken(TestJwt key, string audience = AppId, string issuer = FacebookIssuer)
	{
		return key.CreateToken(issuer, audience, new Dictionary<string, object>
		{
			["sub"] = FacebookUserId,
			["email"] = FacebookEmail,
			["given_name"] = "Jwt",
			["family_name"] = "Claims"
		});
	}
	
	#endregion
	
	#region Classic Login
	
	[Fact]
	public async Task VerifyTokenAsync_WithAnotherAppId_ThrowsUntrustedProvider()
	{
		var request = CreateClientRequest(FacebookUserId, FacebookEmail, appId: "another-app");
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.Authenticator.VerifyTokenAsync(request, CreateProvider(), TestContext.Current.CancellationToken));
		
		Assert.Equal("UntrustedProvider", exception.ErrorCode);
	}
	
	[Fact]
	public async Task VerifyTokenAsync_WithInvalidAccessToken_ReturnsFalse()
	{
		this.RespondWithDebugToken(isValid: false);
		this.RespondWithProfile();
		var request = CreateClientRequest(FacebookUserId, FacebookEmail);
		
		Assert.False(await this.Authenticator.VerifyTokenAsync(request, CreateProvider(), TestContext.Current.CancellationToken));
	}
	
	[Fact]
	public async Task VerifyTokenAsync_WithAccessTokenOfAnotherApp_ReturnsFalse()
	{
		this.RespondWithDebugToken(appId: "another-app");
		this.RespondWithProfile();
		var request = CreateClientRequest(FacebookUserId, FacebookEmail);
		
		Assert.False(await this.Authenticator.VerifyTokenAsync(request, CreateProvider(), TestContext.Current.CancellationToken));
	}
	
	[Fact]
	public async Task VerifyTokenAsync_WithUserIdNotOwningTheAccessToken_ReturnsFalse()
	{
		this.RespondWithDebugToken();
		this.RespondWithProfile();
		var request = CreateClientRequest("victim-facebook-id", FacebookEmail);
		
		Assert.False(await this.Authenticator.VerifyTokenAsync(request, CreateProvider(), TestContext.Current.CancellationToken));
	}
	
	[Fact]
	public async Task VerifyTokenAsync_TakesTheEmailFromFacebook_NotFromTheClient()
	{
		// Attack: the attacker's own valid access token + the victim's email in the payload
		this.RespondWithDebugToken();
		this.RespondWithProfile();
		var request = CreateClientRequest(FacebookUserId, "victim@example.com");
		
		Assert.True(await this.Authenticator.VerifyTokenAsync(request, CreateProvider(), TestContext.Current.CancellationToken));
		
		Assert.Equal(FacebookUserId, request.UserId);
		Assert.Equal(FacebookEmail, request.EmailAddress);
		Assert.Equal("Graph", request.User?.FirstName);
		Assert.Equal("Profile", request.User?.LastName);
		Assert.Equal("https://graph.example.com/picture.jpg", request.AvatarUrl);
		Assert.Equal("facebook-user-access-token", this._services.Handler.SingleRequestTo(MeUrl).Query("access_token"));
	}
	
	[Fact]
	public async Task VerifyTokenAsync_WhenFacebookSharesNoEmail_DoesNotUseTheClientEmail()
	{
		this.RespondWithDebugToken();
		this.RespondWithProfile(email: null);
		var request = CreateClientRequest(FacebookUserId, "victim@example.com");
		
		Assert.True(await this.Authenticator.VerifyTokenAsync(request, CreateProvider(), TestContext.Current.CancellationToken));
		
		Assert.Null(request.EmailAddress);
	}
	
	[Fact]
	public async Task VerifyTokenAsync_WhenProfileBelongsToAnotherUser_ReturnsFalse()
	{
		this.RespondWithDebugToken();
		this.RespondWithProfile(id: "another-facebook-id");
		var request = CreateClientRequest(FacebookUserId, FacebookEmail);
		
		Assert.False(await this.Authenticator.VerifyTokenAsync(request, CreateProvider(), TestContext.Current.CancellationToken));
	}
	
	[Fact]
	public async Task VerifyTokenAsync_NeverReportsTheEmailAsVerified()
	{
		// Facebook does not assert email verification; linking by email depends on the provider's trust_email setting
		this.RespondWithDebugToken();
		this.RespondWithProfile();
		var request = CreateClientRequest(FacebookUserId, FacebookEmail);
		
		await this.Authenticator.VerifyTokenAsync(request, CreateProvider(), TestContext.Current.CancellationToken);
		
		Assert.False(request.IsEmailVerified);
	}
	
	#endregion
	
	#region Limited Login
	
	[Fact]
	public async Task VerifyTokenAsync_Limited_WithValidToken_TakesTheIdentityFromTheToken()
	{
		// Attack: the attacker's own valid JWT + the victim's id and email in the payload
		this._services.Handler.Respond(JwksUrl, TestJwt.JwksJson(this._facebookKey1));
		var request = CreateClientRequest("victim-facebook-id", "victim@example.com", this.CreateLimitedToken(this._facebookKey1), isLimited: true);
		
		Assert.True(await this.Authenticator.VerifyTokenAsync(request, CreateProvider(), TestContext.Current.CancellationToken));
		
		Assert.Equal(FacebookUserId, request.UserId);
		Assert.Equal(FacebookEmail, request.EmailAddress);
		Assert.Equal("Jwt", request.User?.FirstName);
		Assert.Equal("Claims", request.User?.LastName);
	}
	
	[Fact]
	public async Task VerifyTokenAsync_Limited_WithTokenOfAnotherApp_IsRejected()
	{
		// Attack: a JWT issued to another app, with that app's id as appId in the payload
		this._services.Handler.Respond(JwksUrl, TestJwt.JwksJson(this._facebookKey1));
		var request = CreateClientRequest(FacebookUserId, FacebookEmail, this.CreateLimitedToken(this._facebookKey1, audience: "attacker-app"), appId: "attacker-app", isLimited: true);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.Authenticator.VerifyTokenAsync(request, CreateProvider(), TestContext.Current.CancellationToken));
		
		Assert.Equal("UntrustedProvider", exception.ErrorCode);
	}
	
	[Fact]
	public async Task VerifyTokenAsync_Limited_WithTokenSignedByAnyPublishedKey_Succeeds()
	{
		// Facebook publishes several keys (rotation); the token may be signed by any of them
		this._services.Handler.Respond(JwksUrl, TestJwt.JwksJson(this._facebookKey1, this._facebookKey2));
		var request = CreateClientRequest(FacebookUserId, FacebookEmail, this.CreateLimitedToken(this._facebookKey2), isLimited: true);
		
		Assert.True(await this.Authenticator.VerifyTokenAsync(request, CreateProvider(), TestContext.Current.CancellationToken));
	}
	
	[Fact]
	public async Task VerifyTokenAsync_Limited_WithTokenSignedByUnknownKey_ThrowsUnauthorized()
	{
		this._services.Handler.Respond(JwksUrl, TestJwt.JwksJson(this._facebookKey1));
		var request = CreateClientRequest(FacebookUserId, FacebookEmail, this.CreateLimitedToken(new TestJwt("attacker-key")), isLimited: true);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.Authenticator.VerifyTokenAsync(request, CreateProvider(), TestContext.Current.CancellationToken));
		
		Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
	}
	
	[Fact]
	public async Task VerifyTokenAsync_Limited_WithTokenFromAnotherIssuer_ThrowsUnauthorized()
	{
		this._services.Handler.Respond(JwksUrl, TestJwt.JwksJson(this._facebookKey1));
		var request = CreateClientRequest(FacebookUserId, FacebookEmail, this.CreateLimitedToken(this._facebookKey1, issuer: "https://attacker.example.com"), isLimited: true);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.Authenticator.VerifyTokenAsync(request, CreateProvider(), TestContext.Current.CancellationToken));
		
		Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
	}
	
	#endregion
}