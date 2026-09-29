using System.Net;
using System.Net.Http.Json;
using ErtisAuth.IntegrationTests.Infrastructure;
using ErtisAuth.IntegrationTests.Resources;

namespace ErtisAuth.IntegrationTests.Sdk;

/// <summary>
/// A client application protected by ErtisAuth.Sdk.AspNetCore against the real ErtisAuth: the SDK asks ErtisAuth
/// (whoami + roles/check-permission) whether the token may perform the endpoint's rbac.
/// </summary>
public class SdkClientAuthorizationTests : IClassFixture<ErtisAuthInstance>
{
	#region Constants
	
	private const string Password = "Client-P@ssw0rd!";
	
	#endregion
	
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Properties
	
	private string MembershipUrl => $"/memberships/{this._instance.MembershipId}";
	
	private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;
	
	#endregion
	
	#region Constructors
	
	public SdkClientAuthorizationTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private async Task<ResourceClient> AdminResourceClientAsync(string resource) =>
		new(await this._instance.CreateAdminClientAsync(), $"{this.MembershipUrl}/{resource}");
	
	private async Task<string> CreateRoleAsync(params string[] permissions)
	{
		var roles = await this.AdminResourceClientAsync("roles");
		var role = await roles.CreateAsync(new { name = $"Client {Guid.NewGuid():N}", permissions });
		return role["slug"]!.GetValue<string>();
	}
	
	private async Task<(string AccessToken, string RefreshToken)> LoginAsUserOfRoleAsync(params string[] permissions)
	{
		var role = await this.CreateRoleAsync(permissions);
		var username = $"client{Guid.NewGuid():N}";
		var users = await this.AdminResourceClientAsync("users");
		await users.CreateAsync(new
		{
			username,
			firstname = "Client",
			lastname = "User",
			email_address = $"{username}@example.com",
			password = Password,
			role,
			user_type = "user"
		});
		
		return await this._instance.GenerateTokenAsync(username, Password);
	}
	
	private async Task<string> GenerateScopedTokenAsync(string accessToken, params string[] scopes)
	{
		using var request = new HttpRequestMessage(HttpMethod.Post, "/generate-token")
		{
			Content = JsonContent.Create(new { scopes })
		};
		
		request.Headers.Add("Membership", this._instance.MembershipId);
		request.Headers.Add("Authorization", $"Bearer {accessToken}");
		using var response = await this._instance.CreateClient().SendAsync(request, CancellationToken);
		var scoped = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.Created);
		return scoped!["access_token"]!.GetValue<string>();
	}
	
	private static async Task AssertStatusAsync(Task<HttpResponseMessage> request, HttpStatusCode expected)
	{
		using var response = await request;
		await ResourceClient.AssertStatusAsync(response, expected);
	}
	
	#endregion
	
	#region Bearer
	
	[Fact]
	public async Task User_IsAllowedOnlyWhatItsRolePermits()
	{
		var (accessToken, _) = await this.LoginAsUserOfRoleAsync("orders.read", "roles.read");
		await using var application = await SdkClientApplication.StartAsync(this._instance);
		using var client = application.CreateClient($"Bearer {accessToken}");
		
		await AssertStatusAsync(client.GetAsync("/orders", CancellationToken), HttpStatusCode.OK);
		await AssertStatusAsync(client.PostAsync("/orders", null, CancellationToken), HttpStatusCode.Forbidden);
	}
	
	[Fact]
	public async Task User_WithoutPermission_IsForbidden()
	{
		var (accessToken, _) = await this.LoginAsUserOfRoleAsync("roles.read");
		await using var application = await SdkClientApplication.StartAsync(this._instance);
		using var client = application.CreateClient($"Bearer {accessToken}");
		
		await AssertStatusAsync(client.GetAsync("/orders", CancellationToken), HttpStatusCode.Forbidden);
	}
	
	/// <summary>
	/// The client application's permission is checked, not the permission of reading roles in ErtisAuth.
	/// </summary>
	[Fact]
	public async Task User_WhoseRoleDoesNotPermitReadingRoles_IsAllowed()
	{
		var (accessToken, _) = await this.LoginAsUserOfRoleAsync("orders.read");
		await using var application = await SdkClientApplication.StartAsync(this._instance);
		using var client = application.CreateClient($"Bearer {accessToken}");
		
		await AssertStatusAsync(client.GetAsync("/orders", CancellationToken), HttpStatusCode.OK);
	}
	
	[Fact]
	public async Task ScopedToken_IsLimitedToItsScopes()
	{
		var (accessToken, _) = await this.LoginAsUserOfRoleAsync("orders.*");
		var scopedToken = await this.GenerateScopedTokenAsync(accessToken, "orders.read");
		await using var application = await SdkClientApplication.StartAsync(this._instance);
		using var client = application.CreateClient($"Bearer {scopedToken}");
		
		await AssertStatusAsync(client.GetAsync("/orders", CancellationToken), HttpStatusCode.OK);
		await AssertStatusAsync(client.PostAsync("/orders", null, CancellationToken), HttpStatusCode.Forbidden);
	}
	
	[Fact]
	public async Task InvalidOrRevokedToken_IsUnauthorized()
	{
		var (accessToken, _) = await this.LoginAsUserOfRoleAsync("orders.read", "roles.read");
		await using var application = await SdkClientApplication.StartAsync(this._instance);
		
		using var invalidClient = application.CreateClient("Bearer not-a-token");
		await AssertStatusAsync(invalidClient.GetAsync("/orders", CancellationToken), HttpStatusCode.Unauthorized);
		
		await AssertStatusAsync(this._instance.CreateClient($"Bearer {accessToken}").GetAsync("/revoke-token", CancellationToken), HttpStatusCode.NoContent);
		using var revokedClient = application.CreateClient($"Bearer {accessToken}");
		await AssertStatusAsync(revokedClient.GetAsync("/orders", CancellationToken), HttpStatusCode.Unauthorized);
	}
	
	[Fact]
	public async Task SelfAuthorizedEndpoint_NeedsOnlyAValidToken()
	{
		var (accessToken, _) = await this.LoginAsUserOfRoleAsync("roles.read");
		await using var application = await SdkClientApplication.StartAsync(this._instance);
		
		using var client = application.CreateClient($"Bearer {accessToken}");
		await AssertStatusAsync(client.GetAsync("/profile", CancellationToken), HttpStatusCode.OK);
		
		using var invalidClient = application.CreateClient("Bearer not-a-token");
		await AssertStatusAsync(invalidClient.GetAsync("/profile", CancellationToken), HttpStatusCode.Unauthorized);
	}
	
	/// <summary>
	/// The CMS case: the action is [SelfAuthorized] in an [Authorized] controller and checks an rbac it builds from the request.
	/// </summary>
	[Fact]
	public async Task SelfAuthorizedAction_ChecksThePermissionItself()
	{
		var (accessToken, _) = await this.LoginAsUserOfRoleAsync("documents-invoice.read");
		await using var application = await SdkClientApplication.StartAsync(this._instance);
		
		using var client = application.CreateClient($"Bearer {accessToken}");
		await AssertStatusAsync(client.GetAsync("/documents/invoice", CancellationToken), HttpStatusCode.OK);
		await AssertStatusAsync(client.GetAsync("/documents/contract", CancellationToken), HttpStatusCode.Forbidden);
		
		using var invalidClient = application.CreateClient("Bearer not-a-token");
		await AssertStatusAsync(invalidClient.GetAsync("/documents/invoice", CancellationToken), HttpStatusCode.Unauthorized);
	}
	
	[Fact]
	public async Task SelfAuthorizedAction_WithScopedToken_ChecksThePermissionWithinTheScopes()
	{
		var (accessToken, _) = await this.LoginAsUserOfRoleAsync("documents-invoice.read", "documents-contract.read");
		var scopedToken = await this.GenerateScopedTokenAsync(accessToken, "documents-invoice.read");
		await using var application = await SdkClientApplication.StartAsync(this._instance);
		
		using var client = application.CreateClient($"Bearer {scopedToken}");
		await AssertStatusAsync(client.GetAsync("/documents/invoice", CancellationToken), HttpStatusCode.OK);
		await AssertStatusAsync(client.GetAsync("/documents/contract", CancellationToken), HttpStatusCode.Forbidden);
	}
	
	#endregion
	
	#region Basic
	
	/// <summary>
	/// The application's role needs no ErtisAuth permissions (roles.read, applications.read) for the SDK to work.
	/// </summary>
	[Fact]
	public async Task Application_IsAllowedOnlyWhatItsRolePermits()
	{
		var role = await this.CreateRoleAsync("orders.read");
		var applications = await this.AdminResourceClientAsync("applications");
		var created = await applications.CreateAsync(new { name = $"Client {Guid.NewGuid():N}", role });
		var basicToken = $"Basic {created["_id"]!.GetValue<string>()}:{created["secret"]!.GetValue<string>()}";
		
		await using var application = await SdkClientApplication.StartAsync(this._instance);
		using var client = application.CreateClient(basicToken);
		await AssertStatusAsync(client.GetAsync("/orders", CancellationToken), HttpStatusCode.OK);
		await AssertStatusAsync(client.PostAsync("/orders", null, CancellationToken), HttpStatusCode.Forbidden);
		
		using var selfAuthorizedClient = application.CreateClient(basicToken);
		await AssertStatusAsync(selfAuthorizedClient.GetAsync("/documents/invoice", CancellationToken), HttpStatusCode.Forbidden);
		
		using var wrongSecretClient = application.CreateClient($"Basic {created["_id"]!.GetValue<string>()}:wrong-secret");
		await AssertStatusAsync(wrongSecretClient.GetAsync("/orders", CancellationToken), HttpStatusCode.Unauthorized);
	}
	
	#endregion
	
	#region ErtisAuth Unavailable
	
	/// <summary>
	/// ErtisAuth unreachable: 503 AuthenticationServiceUnavailable, not 401, so that the client application doesn't sign
	/// its users out during an outage.
	/// </summary>
	[Fact]
	public async Task UnreachableErtisAuth_IsServiceUnavailable()
	{
		var (accessToken, _) = await this.LoginAsUserOfRoleAsync("orders.read");
		await using var application = await SdkClientApplication.StartAsync(this._instance, ertisAuthAddress: "http://127.0.0.1:1");
		
		foreach (var path in new[] { "/orders", "/profile" })
		{
			using var client = application.CreateClient($"Bearer {accessToken}");
			using var response = await client.GetAsync(path, CancellationToken);
			var error = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.ServiceUnavailable);
			Assert.Equal("AuthenticationServiceUnavailable", error!["ErrorCode"]?.GetValue<string>() ?? error["errorCode"]!.GetValue<string>());
		}
	}
	
	#endregion
}