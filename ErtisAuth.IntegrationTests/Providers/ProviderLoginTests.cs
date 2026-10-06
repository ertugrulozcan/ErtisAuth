using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ErtisAuth.IntegrationTests.Infrastructure;
using ErtisAuth.IntegrationTests.Resources;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ErtisAuth.IntegrationTests.Providers;

/// <summary>
/// Sign in with Microsoft, Google, Facebook and Apple end to end: the controller, ProviderService and the real
/// authenticators, with the providers' HTTP APIs answered by <see cref="FakeOAuthProviders"/>.
/// The identity (and the email a login may be linked by) comes from the provider, never from the client's payload.
/// </summary>
public class ProviderLoginTests : IClassFixture<OAuthErtisAuthInstance>
{
	#region Constants
	
	private const string MicrosoftClientId = "microsoft-client-id";
	
	private const string GoogleClientId = "google-client-id";
	
	private const string FacebookAppId = "facebook-app-id";
	
	private const string AppleClientId = "com.example.app";
	
	#endregion
	
	#region Fields
	
	private readonly OAuthErtisAuthInstance _instance;
	
	#endregion
	
	#region Properties
	
	private FakeOAuthProviders Providers => this._instance.Providers;
	
	private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="instance"></param>
	public ProviderLoginTests(OAuthErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private async Task ConfigureProvidersAsync()
	{
		await this._instance.ConfigureProviderAsync("Microsoft", x => x["appClientId"] = MicrosoftClientId);
		await this._instance.ConfigureProviderAsync("Google", x => x["appClientId"] = GoogleClientId);
		await this._instance.ConfigureProviderAsync("Facebook", x => x["appClientId"] = FacebookAppId);
		await this._instance.ConfigureProviderAsync("Apple", x =>
		{
			x["appClientId"] = AppleClientId;
			x["teamId"] = "TEAM123456";
			x["privateKeyId"] = "KEY1234567";
			x["privateKey"] = FakeOAuthProviders.CreateApplePrivateKeyPem();
			x["redirectUri"] = "https://app.example.com/apple/callback";
		});
	}
	
	private async Task<HttpResponseMessage> LoginAsync(string provider, object body, string? query = null)
	{
		await this.ConfigureProvidersAsync();
		return await this.SendLoginAsync(provider, body, query);
	}
	
	/// <summary>
	/// The login request as it is, without configuring the providers first.
	/// </summary>
	private async Task<HttpResponseMessage> SendLoginAsync(string provider, object body, string? query = null)
	{
		using var request = new HttpRequestMessage(HttpMethod.Post, $"/oauth/{provider}/login{query}");
		request.Content = JsonContent.Create(body);
		
		request.Headers.Add("Membership", this._instance.MembershipId);
		return await this._instance.CreateClient().SendAsync(request, CancellationToken);
	}
	
	/// <summary>
	/// Logs in, then returns the user of the issued token (as the admin sees it) and the token.
	/// </summary>
	private async Task<(JsonObject User, string AccessToken)> AssertLoginAsync(HttpResponseMessage response)
	{
		var token = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.Created);
		var accessToken = token!["access_token"]!.GetValue<string>();
		
		var me = await this._instance.CreateClient($"Bearer {accessToken}").GetFromJsonAsync<JsonObject>("/me", CancellationToken);
		var users = new ResourceClient(await this._instance.CreateAdminClientAsync(), $"/memberships/{this._instance.MembershipId}/users");
		return (await users.GetAsync(me!["_id"]!.GetValue<string>()), accessToken);
	}
	
	private static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode statusCode, string errorCode)
	{
		var error = await ResourceClient.AssertStatusAsync(response, statusCode);
		Assert.Equal(errorCode, error!["errorCode"]!.GetValue<string>());
	}
	
	private static string[] ConnectedAccountsOf(JsonObject user, string provider)
	{
		return user["connected_accounts"]?.AsArray()
			.Where(x => x!["Provider"]!.GetValue<string>() == provider)
			.Select(x => x!["UserId"]!.GetValue<string>())
			.ToArray() ?? [];
	}
	
	private async Task<JsonObject> CreateLocalUserAsync(string emailAddress)
	{
		var users = new ResourceClient(await this._instance.CreateAdminClientAsync(), $"/memberships/{this._instance.MembershipId}/users");
		return await users.CreateAsync(new
		{
			username = emailAddress,
			firstname = "Local",
			lastname = "User",
			email_address = emailAddress,
			password = "Local-P@ssw0rd!",
			role = "admin",
			user_type = "user"
		});
	}
	
	private static object MicrosoftLogin(string clientId = MicrosoftClientId) => new
	{
		clientId,
		token = new { accessToken = "microsoft-access-token" }
	};
	
	private static object GoogleLogin(string idToken, string clientId = GoogleClientId) => new
	{
		clientId,
		token = new { idToken, clientId }
	};
	
	#endregion
	
	#region Microsoft
	
	[Fact]
	public async Task Microsoft_FirstLoginCreatesTheUserAndTheNextLoginsFindIt()
	{
		var graphId = Guid.NewGuid().ToString("N");
		var email = $"ms-{graphId}@contoso.com";
		this.Providers.Respond(FakeOAuthProviders.MicrosoftMeUrl, new { id = graphId, mail = email, givenName = "Graph", surname = "User" });
		
		var (user, accessToken) = await this.AssertLoginAsync(await this.LoginAsync("microsoft", MicrosoftLogin()));
		Assert.Equal(email, user["email_address"]!.GetValue<string>());
		Assert.Equal("Graph", user["firstname"]!.GetValue<string>());
		Assert.Equal([graphId], ConnectedAccountsOf(user, "Microsoft"));
		Assert.Equal("Bearer microsoft-access-token", this.Providers.RequestsTo(FakeOAuthProviders.MicrosoftMeUrl).Last().Authorization);
		
		using var meResponse = await this._instance.CreateClient($"Bearer {accessToken}").GetAsync("/me", CancellationToken);
		await ResourceClient.AssertStatusAsync(meResponse, HttpStatusCode.OK);
		
		var (again, _) = await this.AssertLoginAsync(await this.LoginAsync("microsoft", MicrosoftLogin()));
		Assert.Equal(user["_id"]!.GetValue<string>(), again["_id"]!.GetValue<string>());
	}
	
	/// <summary>
	/// nOAuth: Graph's "mail" is set by the tenant, not verified; with trust_email off it can't link to a local account.
	/// </summary>
	[Fact]
	public async Task Microsoft_UnverifiedEmailOfAnExistingAccount_IsNotLinked()
	{
		var victimEmail = $"victim-{Guid.NewGuid():N}@example.com";
		var victim = await this.CreateLocalUserAsync(victimEmail);
		this.Providers.Respond(FakeOAuthProviders.MicrosoftMeUrl, new { id = Guid.NewGuid().ToString("N"), mail = victimEmail, givenName = "Attacker" });
		
		using var response = await this.LoginAsync("microsoft", MicrosoftLogin());
		
		await AssertErrorAsync(response, HttpStatusCode.Conflict, "ProviderEmailNotTrusted");
		var users = new ResourceClient(await this._instance.CreateAdminClientAsync(), $"/memberships/{this._instance.MembershipId}/users");
		Assert.Empty(ConnectedAccountsOf(await users.GetAsync(victim["_id"]!.GetValue<string>()), "Microsoft"));
	}
	
	[Fact]
	public async Task Microsoft_TokenOfAnotherApp_IsRejected()
	{
		using var response = await this.LoginAsync("microsoft", MicrosoftLogin(clientId: "another-app"));
		
		await AssertErrorAsync(response, HttpStatusCode.Forbidden, "UntrustedProvider");
	}
	
	[Fact]
	public async Task Microsoft_TokenRejectedByGraph_IsUnauthorized()
	{
		this.Providers.Respond(FakeOAuthProviders.MicrosoftMeUrl, new { error = new { code = "InvalidAuthenticationToken" } }, HttpStatusCode.Unauthorized);
		
		using var response = await this.LoginAsync("microsoft", MicrosoftLogin());
		
		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}
	
	#endregion
	
	#region Google
	
	/// <summary>
	/// An email verified by Google links the login to the existing account (trust_email off).
	/// </summary>
	[Fact]
	public async Task Google_VerifiedEmailOfAnExistingAccount_IsLinked()
	{
		var email = $"google-{Guid.NewGuid():N}@example.com";
		var local = await this.CreateLocalUserAsync(email);
		var sub = Guid.NewGuid().ToString("N");
		var idToken = this.Providers.Google.Issue(GoogleClientId, sub, email, emailVerified: true);
		
		var (user, _) = await this.AssertLoginAsync(await this.LoginAsync("google", GoogleLogin(idToken)));
		
		Assert.Equal(local["_id"]!.GetValue<string>(), user["_id"]!.GetValue<string>());
		Assert.Equal([sub], ConnectedAccountsOf(user, "Google"));
	}
	
	[Fact]
	public async Task Google_UnverifiedEmailOfAnExistingAccount_IsNotLinked()
	{
		var email = $"google-{Guid.NewGuid():N}@example.com";
		await this.CreateLocalUserAsync(email);
		var idToken = this.Providers.Google.Issue(GoogleClientId, Guid.NewGuid().ToString("N"), email, emailVerified: false);
		
		using var response = await this.LoginAsync("google", GoogleLogin(idToken));
		
		await AssertErrorAsync(response, HttpStatusCode.Conflict, "ProviderEmailNotTrusted");
	}
	
	[Fact]
	public async Task Google_TokenOfAnotherAudience_IsUnauthorized()
	{
		var idToken = this.Providers.Google.Issue("another-client-id", Guid.NewGuid().ToString("N"), $"google-{Guid.NewGuid():N}@example.com", emailVerified: true);
		
		using var response = await this.LoginAsync("google", GoogleLogin(idToken));
		
		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}
	
	#endregion
	
	#region Facebook
	
	/// <summary>
	/// The client's payload names another email: the identity and the email come from Facebook (debug_token, /me).
	/// </summary>
	[Fact]
	public async Task Facebook_TakesTheIdentityFromFacebook()
	{
		var facebookId = Guid.NewGuid().ToString("N");
		var facebookEmail = $"fb-{facebookId}@example.com";
		this.Providers.Respond(FakeOAuthProviders.FacebookDebugTokenUrl, new { data = new { app_id = FacebookAppId, user_id = facebookId, is_valid = true, type = "USER" } });
		this.Providers.Respond(FakeOAuthProviders.FacebookMeUrl, new { id = facebookId, first_name = "Graph", last_name = "Profile", email = facebookEmail });
		
		var (user, _) = await this.AssertLoginAsync(await this.LoginAsync("facebook", new
		{
			appId = FacebookAppId,
			user = new { id = facebookId, first_name = "Client", last_name = "Name", email = $"victim-{Guid.NewGuid():N}@example.com", accessToken = "facebook-user-access-token" }
		}));
		
		Assert.Equal(facebookEmail, user["email_address"]!.GetValue<string>());
		Assert.Equal([facebookId], ConnectedAccountsOf(user, "Facebook"));
	}
	
	/// <summary>
	/// The provider and the provider's user id must match in the same connected account: a Facebook account whose id equals
	/// the Google id of a user must not log in as that user (the two conditions used to match different array elements).
	/// </summary>
	[Fact]
	public async Task Facebook_IdOfAnotherProvidersAccount_DoesNotLogInAsItsUser()
	{
		var googleId = Guid.NewGuid().ToString("N");
		var victim = await this.CreateLocalUserAsync($"victim-{Guid.NewGuid():N}@example.com");
		var victimId = victim["_id"]!.GetValue<string>();
		await this._instance.Database.GetCollection<BsonDocument>("users").UpdateOneAsync(
			Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(victimId)),
			Builders<BsonDocument>.Update.Set("connected_accounts", new BsonArray
			{
				new BsonDocument { { "Provider", "Google" }, { "UserId", googleId } },
				new BsonDocument { { "Provider", "Facebook" }, { "UserId", Guid.NewGuid().ToString("N") } }
			}),
			cancellationToken: CancellationToken);
		
		var attackerEmail = $"fb-{Guid.NewGuid():N}@example.com";
		this.Providers.Respond(FakeOAuthProviders.FacebookDebugTokenUrl, new { data = new { app_id = FacebookAppId, user_id = googleId, is_valid = true, type = "USER" } });
		this.Providers.Respond(FakeOAuthProviders.FacebookMeUrl, new { id = googleId, first_name = "Attacker", last_name = "Account", email = attackerEmail });
		
		var (user, _) = await this.AssertLoginAsync(await this.LoginAsync("facebook", new
		{
			appId = FacebookAppId,
			user = new { id = googleId, first_name = "Attacker", last_name = "Account", email = attackerEmail, accessToken = "facebook-user-access-token" }
		}));
		
		Assert.NotEqual(victimId, user["_id"]!.GetValue<string>());
		Assert.Equal(attackerEmail, user["email_address"]!.GetValue<string>());
	}
	
	[Fact]
	public async Task Facebook_TokenOfAnotherApp_IsRejected()
	{
		var facebookId = Guid.NewGuid().ToString("N");
		this.Providers.Respond(FakeOAuthProviders.FacebookDebugTokenUrl, new { data = new { app_id = "another-app", user_id = facebookId, is_valid = true, type = "USER" } });
		
		using var response = await this.LoginAsync("facebook", new
		{
			appId = FacebookAppId,
			user = new { id = facebookId, first_name = "Client", last_name = "Name", email = $"fb-{facebookId}@example.com", accessToken = "facebook-user-access-token" }
		});
		
		Assert.False(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync(CancellationToken));
	}
	
	#endregion
	
	#region Apple
	
	private object AppleLogin(string clientSub, string clientEmail) => new
	{
		user = new { name = new { firstName = "Apple", lastName = "User" }, email = clientEmail },
		authorization = new
		{
			code = "authorization-code",
			id_token = FakeOAuthProviders.CreateUnsignedToken(new Dictionary<string, object>
			{
				["iss"] = FakeOAuthProviders.AppleIssuer,
				["aud"] = AppleClientId,
				["sub"] = clientSub,
				["exp"] = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds()
			})
		}
	};
	
	/// <summary>
	/// The client's id_token and email are ignored: the identity comes from Apple's token endpoint (code exchange).
	/// </summary>
	[Fact]
	public async Task Apple_TakesTheIdentityFromTheCodeExchange()
	{
		var appleSub = $"000123.{Guid.NewGuid():N}.0456";
		var appleEmail = $"{Guid.NewGuid():N}@privaterelay.appleid.com";
		this.Providers.RespondToAppleCodeExchange(AppleClientId, appleSub, appleEmail);
		
		var (user, _) = await this.AssertLoginAsync(await this.LoginAsync("apple", this.AppleLogin("forged-sub", $"victim-{Guid.NewGuid():N}@example.com"), "?platform=web"));
		
		Assert.Equal(appleEmail, user["email_address"]!.GetValue<string>());
		Assert.Equal([appleSub], ConnectedAccountsOf(user, "Apple"));
		Assert.Contains("code=authorization-code", this.Providers.RequestsTo(FakeOAuthProviders.AppleTokenUrl).Last().Body);
	}
	
	/// <summary>
	/// Logging out (revoke-token) revokes the Apple token of the session.
	/// </summary>
	[Fact]
	public async Task Apple_LogoutRevokesTheAppleToken()
	{
		this.Providers.RespondToAppleCodeExchange(AppleClientId, $"000123.{Guid.NewGuid():N}.0456", $"{Guid.NewGuid():N}@privaterelay.appleid.com");
		this.Providers.Respond(FakeOAuthProviders.AppleRevokeUrl, "{}");
		var (_, accessToken) = await this.AssertLoginAsync(await this.LoginAsync("apple", this.AppleLogin("client-sub", "client@example.com"), "?platform=web"));
		var revokeCountBefore = this.Providers.RequestsTo(FakeOAuthProviders.AppleRevokeUrl).Count();
		
		using var response = await this._instance.CreateClient($"Bearer {accessToken}").GetAsync("/revoke-token", CancellationToken);
		await ResourceClient.AssertStatusAsync(response, HttpStatusCode.NoContent);
		
		Assert.Equal(revokeCountBefore + 1, this.Providers.RequestsTo(FakeOAuthProviders.AppleRevokeUrl).Count());
	}
	
	/// <summary>
	/// Regression: the native flow (AppleNativeProvider) was given the authenticator of the web flow only (500).
	/// </summary>
	[Fact]
	public async Task AppleNative_TakesTheIdentityFromTheCodeExchange()
	{
		await this.ConfigureProvidersAsync();
		await this._instance.ConfigureProviderAsync("AppleNative", x =>
		{
			x["appClientId"] = AppleClientId;
			x["teamId"] = "TEAM123456";
			x["privateKeyId"] = "KEY1234567";
			x["privateKey"] = FakeOAuthProviders.CreateApplePrivateKeyPem();
			x["redirectUri"] = "https://app.example.com/apple/callback";
		});
		
		var appleSub = $"000123.{Guid.NewGuid():N}.0456";
		var appleEmail = $"{Guid.NewGuid():N}@privaterelay.appleid.com";
		this.Providers.RespondToAppleCodeExchange(AppleClientId, appleSub, appleEmail);
		
		var (user, _) = await this.AssertLoginAsync(await this.SendLoginAsync("apple", this.AppleLogin("forged-sub", $"victim-{Guid.NewGuid():N}@example.com"), "?platform=ios"));
		
		Assert.Equal(appleEmail, user["email_address"]!.GetValue<string>());
		Assert.Equal([appleSub], ConnectedAccountsOf(user, "AppleNative"));
	}
	
	#endregion
	
	#region Provider State
	
	[Fact]
	public async Task InactiveProvider_IsRejected()
	{
		await this.ConfigureProvidersAsync();
		await this._instance.ConfigureProviderAsync("AppleNative", x => x["isActive"] = false);
		
		using var response = await this.LoginAsync("apple", this.AppleLogin("client-sub", "client@example.com"), "?platform=ios");
		
		await AssertErrorAsync(response, HttpStatusCode.Forbidden, "ProviderIsDisable");
	}
	
	[Fact]
	public async Task DeletedProvider_IsNotConfigured()
	{
		await this.ConfigureProvidersAsync();
		var provider = (await this._instance.FindProviderAsync("Microsoft"))!;
		var adminClient = await this._instance.CreateAdminClientAsync();
		using (var deleteResponse = await adminClient.DeleteAsync($"/memberships/{this._instance.MembershipId}/providers/{provider["_id"]!.GetValue<string>()}", CancellationToken))
		{
			await ResourceClient.AssertStatusAsync(deleteResponse, HttpStatusCode.NoContent);
		}
		
		using var response = await this.SendLoginAsync("microsoft", MicrosoftLogin());
		
		await AssertErrorAsync(response, HttpStatusCode.Forbidden, "ProviderNotConfigured");
	}
	
	#endregion
}