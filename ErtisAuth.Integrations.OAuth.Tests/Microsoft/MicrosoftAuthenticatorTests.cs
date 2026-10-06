using System.Net;
using System.Text.Json;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Integrations.OAuth.Core;
using ErtisAuth.Integrations.OAuth.Microsoft;
using ErtisAuth.Integrations.OAuth.Tests.Helpers;

namespace ErtisAuth.Integrations.OAuth.Tests.Microsoft;

/// <summary>
/// Microsoft login: the identity comes from Microsoft Graph (/me) called with the client's access token.
/// </summary>
public class MicrosoftAuthenticatorTests
{
	#region Constants
	
	private const string MeUrl = "https://graph.microsoft.com/v1.0/me";
	private const string ClientId = "our-azure-app";
	
	#endregion
	
	#region Fields
	
	private readonly OAuthTestServices _services = new();
	
	#endregion
	
	#region Helpers
	
	private IMicrosoftAuthenticator Authenticator => this._services.Get<IMicrosoftAuthenticator>();
	
	private static MicrosoftProvider CreateProvider()
	{
		return new MicrosoftProvider
		{
			MembershipId = "5f8a1b2c3d4e5f6a7b8c9d00",
			IsActive = true,
			AppClientId = ClientId
		};
	}
	
	private static MicrosoftLoginRequest CreateClientRequest(string clientId = ClientId)
	{
		return new MicrosoftLoginRequest
		{
			ClientId = clientId,
			Token = new MicrosoftToken { AccessToken = "microsoft-access-token" }
		};
	}
	
	#endregion
	
	#region Tests
	
	[Fact]
	public async Task VerifyTokenAsync_TakesTheIdentityFromGraph()
	{
		this._services.Handler.Respond(MeUrl, JsonSerializer.Serialize(new { id = "graph-user-id", mail = "graph.user@contoso.com", givenName = "Graph", surname = "User" }));
		var request = CreateClientRequest();
		
		Assert.True(await this.Authenticator.VerifyTokenAsync((IProviderLoginRequest) request, CreateProvider(), TestContext.Current.CancellationToken));
		
		Assert.Equal("graph-user-id", request.UserId);
		Assert.Equal("graph.user@contoso.com", request.EmailAddress);
		Assert.Equal("Bearer microsoft-access-token", this._services.Handler.SingleRequestTo(MeUrl).Authorization);
	}
	
	[Fact]
	public async Task VerifyTokenAsync_NeverReportsTheEmailAsVerified()
	{
		// Graph's "mail" is set by the tenant and is not verified (nOAuth); linking by email depends on trust_email
		this._services.Handler.Respond(MeUrl, JsonSerializer.Serialize(new { id = "graph-user-id", mail = "graph.user@contoso.com" }));
		var request = CreateClientRequest();
		
		await this.Authenticator.VerifyTokenAsync((IProviderLoginRequest) request, CreateProvider(), TestContext.Current.CancellationToken);
		
		Assert.False(request.IsEmailVerified);
	}
	
	[Fact]
	public async Task VerifyTokenAsync_WhenGraphRejectsTheToken_ReturnsFalse()
	{
		this._services.Handler.Respond(MeUrl, """{"error":{"code":"InvalidAuthenticationToken"}}""", HttpStatusCode.Unauthorized);
		var request = CreateClientRequest();
		
		Assert.False(await this.Authenticator.VerifyTokenAsync((IProviderLoginRequest) request, CreateProvider(), TestContext.Current.CancellationToken));
		Assert.Null(request.UserId);
	}
	
	[Fact]
	public async Task VerifyTokenAsync_WithAnotherClientId_ThrowsUntrustedProvider()
	{
		var request = CreateClientRequest(clientId: "another-app");
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.Authenticator.VerifyTokenAsync((IProviderLoginRequest) request, CreateProvider(), TestContext.Current.CancellationToken));
		
		Assert.Equal("UntrustedProvider", exception.ErrorCode);
	}
	
	#endregion
}