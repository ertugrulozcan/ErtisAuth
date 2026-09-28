using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Integrations.OAuth.Core;
using ErtisAuth.Integrations.OAuth.Google;
using Google.Apis.Auth;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ErtisAuth.Integrations.OAuth.Tests.Google;

/// <summary>
/// Google login: the client sends a Google ID token; the identity comes from the validated token payload.
/// The signature check itself belongs to Google's library (behind IGoogleIdTokenValidator).
/// </summary>
public class GoogleAuthenticatorTests
{
	#region Constants
	
	private const string ClientId = "our-client.apps.googleusercontent.com";
	private const string IdToken = "google-id-token";
	
	#endregion
	
	#region Fields
	
	private readonly IGoogleIdTokenValidator _validator = Substitute.For<IGoogleIdTokenValidator>();
	
	#endregion
	
	#region Helpers
	
	private GoogleAuthenticator CreateAuthenticator() => new(this._validator);
	
	private static Provider CreateProvider()
	{
		return new Provider(KnownProviders.Google)
		{
			MembershipId = "5f8a1b2c3d4e5f6a7b8c9d00",
			IsActive = true,
			AppClientId = ClientId
		};
	}
	
	private static GoogleLoginRequest CreateClientRequest(string clientId = ClientId, string? idToken = IdToken)
	{
		return new GoogleLoginRequest
		{
			ClientId = clientId,
			Token = new GoogleToken { AccessToken = idToken }
		};
	}
	
	private void ValidatorReturns(bool emailVerified = true, string? givenName = "John", string? name = "John Doe", string? email = "john.doe@gmail.com")
	{
		this._validator.ValidateAsync(IdToken, ClientId).Returns(new GoogleJsonWebSignature.Payload
		{
			Subject = "google-sub",
			Email = email,
			EmailVerified = emailVerified,
			GivenName = givenName,
			Name = name,
			FamilyName = "Doe",
			Picture = "https://lh3.googleusercontent.com/picture.jpg",
			ExpirationTimeSeconds = 3600
		});
	}
	
	#endregion
	
	#region Tests
	
	[Fact]
	public async Task VerifyTokenAsync_TakesTheIdentityFromTheValidatedToken()
	{
		this.ValidatorReturns();
		var request = CreateClientRequest();
		
		Assert.True(await this.CreateAuthenticator().VerifyTokenAsync((IProviderLoginRequest) request, CreateProvider(), TestContext.Current.CancellationToken));
		
		Assert.Equal("google-sub", request.UserId);
		Assert.Equal("john.doe@gmail.com", request.EmailAddress);
		Assert.Equal("John", request.User?.FirstName);
		Assert.Equal("Doe", request.User?.LastName);
		Assert.Equal("https://lh3.googleusercontent.com/picture.jpg", request.AvatarUrl);
		Assert.Equal(3600, request.Token?.ExpiresIn);
	}
	
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task VerifyTokenAsync_TakesEmailVerifiedFromTheToken(bool emailVerified)
	{
		this.ValidatorReturns(emailVerified);
		var request = CreateClientRequest();
		
		await this.CreateAuthenticator().VerifyTokenAsync((IProviderLoginRequest) request, CreateProvider(), TestContext.Current.CancellationToken);
		
		Assert.Equal(emailVerified, request.IsEmailVerified);
	}
	
	[Fact]
	public async Task VerifyTokenAsync_ValidatesAgainstTheProviderClientId()
	{
		this.ValidatorReturns();
		
		await this.CreateAuthenticator().VerifyTokenAsync((IProviderLoginRequest) CreateClientRequest(), CreateProvider(), TestContext.Current.CancellationToken);
		
		await this._validator.Received(1).ValidateAsync(IdToken, ClientId);
	}
	
	[Fact]
	public async Task VerifyTokenAsync_WithInvalidToken_ReturnsFalse()
	{
		this._validator.ValidateAsync(IdToken, ClientId).ThrowsAsync(new InvalidJwtException("JWT invalid, unable to verify signature."));
		var request = CreateClientRequest();
		
		Assert.False(await this.CreateAuthenticator().VerifyTokenAsync((IProviderLoginRequest) request, CreateProvider(), TestContext.Current.CancellationToken));
		Assert.Null(request.UserId);
	}
	
	[Fact]
	public async Task VerifyTokenAsync_WithoutToken_ReturnsFalseWithoutValidating()
	{
		var request = CreateClientRequest(idToken: null);
		
		Assert.False(await this.CreateAuthenticator().VerifyTokenAsync((IProviderLoginRequest) request, CreateProvider(), TestContext.Current.CancellationToken));
		await this._validator.DidNotReceiveWithAnyArgs().ValidateAsync(null!, null!);
	}
	
	[Fact]
	public async Task VerifyTokenAsync_WithAnotherClientId_ThrowsUntrustedProvider()
	{
		var request = CreateClientRequest(clientId: "another-client");
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.CreateAuthenticator().VerifyTokenAsync((IProviderLoginRequest) request, CreateProvider(), TestContext.Current.CancellationToken));
		
		Assert.Equal("UntrustedProvider", exception.ErrorCode);
	}
	
	[Fact]
	public async Task VerifyTokenAsync_WithoutGivenName_UsesTheName()
	{
		// Names written in a single field may come only as "name"
		this.ValidatorReturns(givenName: null, name: "Cher");
		var request = CreateClientRequest();
		
		Assert.True(await this.CreateAuthenticator().VerifyTokenAsync((IProviderLoginRequest) request, CreateProvider(), TestContext.Current.CancellationToken));
		
		Assert.Equal("Cher", request.User?.FirstName);
	}
	
	[Theory]
	[InlineData("john.doe@gmail.com", null, null)]
	[InlineData(null, "John", "John Doe")]
	public async Task VerifyTokenAsync_WithoutEmailOrName_ThrowsProviderProfileIncomplete(string? email, string? givenName, string? name)
	{
		// The token is valid, but the client did not request the "email" / "profile" scopes
		this.ValidatorReturns(givenName: givenName, name: name, email: email);
		var request = CreateClientRequest();
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.CreateAuthenticator().VerifyTokenAsync((IProviderLoginRequest) request, CreateProvider(), TestContext.Current.CancellationToken));
		
		Assert.Equal("ProviderProfileIncomplete", exception.ErrorCode);
		Assert.Equal(System.Net.HttpStatusCode.Unauthorized, exception.StatusCode);
	}
	
	#endregion
}