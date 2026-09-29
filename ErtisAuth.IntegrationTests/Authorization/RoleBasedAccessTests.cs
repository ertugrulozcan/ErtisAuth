using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ErtisAuth.IntegrationTests.Infrastructure;
using ErtisAuth.IntegrationTests.Resources;

namespace ErtisAuth.IntegrationTests.Authorization;

/// <summary>
/// RBAC, UBAC and token scopes through the HTTP pipeline, with users of restricted roles and their real tokens.
/// </summary>
public class RoleBasedAccessTests : IClassFixture<ErtisAuthInstance>
{
	#region Constants
	
	private const string Password = "Restricted-P@ssw0rd!";
	
	#endregion
	
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Properties
	
	private string MembershipUrl => $"/memberships/{this._instance.MembershipId}";
	
	private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;
	
	#endregion
	
	#region Constructors
	
	public RoleBasedAccessTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private async Task<ResourceClient> AdminResourceClientAsync(string resource) =>
		new(await this._instance.CreateAdminClientAsync(), $"{this.MembershipUrl}/{resource}");
	
	private async Task<(string Id, string Slug)> CreateRoleAsync(string[] permissions, string[]? forbidden = null)
	{
		var roles = await this.AdminResourceClientAsync("roles");
		var role = await roles.CreateAsync(new { name = $"Role {Guid.NewGuid():N}", permissions, forbidden = forbidden ?? [] });
		return (role["_id"]!.GetValue<string>(), role["slug"]!.GetValue<string>());
	}
	
	private async Task<JsonObject> CreateUserAsync(string role, string[]? permissions = null, string[]? forbidden = null)
	{
		var username = $"user{Guid.NewGuid():N}";
		var users = await this.AdminResourceClientAsync("users");
		return await users.CreateAsync(new
		{
			username,
			firstname = "Jane",
			lastname = "Doe",
			email_address = $"{username}@example.com",
			password = Password,
			role,
			user_type = "user",
			permissions,
			forbidden
		});
	}
	
	/// <summary>
	/// A user of the role, logged in; returns the user and a client with its bearer token.
	/// </summary>
	private async Task<(JsonObject User, HttpClient Client)> LoginAsUserOfRoleAsync(string[] permissions, string[]? forbidden = null, string[]? userPermissions = null, string[]? userForbidden = null)
	{
		var (_, slug) = await this.CreateRoleAsync(permissions, forbidden);
		var user = await this.CreateUserAsync(slug, userPermissions, userForbidden);
		var (accessToken, _) = await this._instance.GenerateTokenAsync(user["username"]!.GetValue<string>(), Password);
		return (user, this._instance.CreateClient($"Bearer {accessToken}"));
	}
	
	private static async Task AssertStatusAsync(Task<HttpResponseMessage> request, HttpStatusCode expected)
	{
		using var response = await request;
		await ResourceClient.AssertStatusAsync(response, expected);
	}
	
	private object NewUserBody()
	{
		var username = $"new{Guid.NewGuid():N}";
		return new
		{
			username,
			firstname = "New",
			lastname = "User",
			email_address = $"{username}@example.com",
			password = Password,
			role = "admin",
			user_type = "user"
		};
	}
	
	#endregion
	
	#region Role
	
	[Fact]
	public async Task Role_OnlyExplicitlyPermittedActionsAreAllowed()
	{
		var (_, client) = await this.LoginAsUserOfRoleAsync(["users.read"]);
		var otherUserId = (await this.CreateUserAsync("admin"))["_id"]!.GetValue<string>();
		
		await AssertStatusAsync(client.GetAsync($"{this.MembershipUrl}/users", CancellationToken), HttpStatusCode.OK);
		await AssertStatusAsync(client.GetAsync($"{this.MembershipUrl}/users/{otherUserId}", CancellationToken), HttpStatusCode.OK);
		await AssertStatusAsync(client.PostAsJsonAsync($"{this.MembershipUrl}/users", this.NewUserBody(), CancellationToken), HttpStatusCode.Forbidden);
		await AssertStatusAsync(client.DeleteAsync($"{this.MembershipUrl}/users/{otherUserId}", CancellationToken), HttpStatusCode.Forbidden);
		await AssertStatusAsync(client.GetAsync($"{this.MembershipUrl}/roles", CancellationToken), HttpStatusCode.Forbidden);
		await AssertStatusAsync(client.GetAsync($"{this.MembershipUrl}/applications", CancellationToken), HttpStatusCode.Forbidden);
	}
	
	[Fact]
	public async Task Role_ForbiddenNarrowsAWildcardPermission()
	{
		var (_, client) = await this.LoginAsUserOfRoleAsync(["users.*"], forbidden: ["users.delete"]);
		var otherUserId = (await this.CreateUserAsync("admin"))["_id"]!.GetValue<string>();
		
		await AssertStatusAsync(client.GetAsync($"{this.MembershipUrl}/users/{otherUserId}", CancellationToken), HttpStatusCode.OK);
		await AssertStatusAsync(client.DeleteAsync($"{this.MembershipUrl}/users/{otherUserId}", CancellationToken), HttpStatusCode.Forbidden);
	}
	
	[Fact]
	public async Task Role_ObjectPermission_CoversOnlyThatObject()
	{
		var permittedUserId = (await this.CreateUserAsync("admin"))["_id"]!.GetValue<string>();
		var otherUserId = (await this.CreateUserAsync("admin"))["_id"]!.GetValue<string>();
		var (_, client) = await this.LoginAsUserOfRoleAsync([$"users.read.{permittedUserId}"]);
		
		await AssertStatusAsync(client.GetAsync($"{this.MembershipUrl}/users/{permittedUserId}", CancellationToken), HttpStatusCode.OK);
		await AssertStatusAsync(client.GetAsync($"{this.MembershipUrl}/users/{otherUserId}", CancellationToken), HttpStatusCode.Forbidden);
	}
	
	[Fact]
	public async Task Role_ChangesApplyToExistingTokens()
	{
		var (roleId, slug) = await this.CreateRoleAsync(["users.read"]);
		var user = await this.CreateUserAsync(slug);
		var (accessToken, _) = await this._instance.GenerateTokenAsync(user["username"]!.GetValue<string>(), Password);
		var client = this._instance.CreateClient($"Bearer {accessToken}");
		await AssertStatusAsync(client.GetAsync($"{this.MembershipUrl}/roles", CancellationToken), HttpStatusCode.Forbidden);
		
		var roles = await this.AdminResourceClientAsync("roles");
		var role = await roles.GetAsync(roleId);
		await roles.UpdateAsync(roleId, new { name = role["name"]!.GetValue<string>(), slug, permissions = new[] { "users.read", "roles.read" } });
		await AssertStatusAsync(client.GetAsync($"{this.MembershipUrl}/roles", CancellationToken), HttpStatusCode.OK);
		
		await roles.UpdateAsync(roleId, new { name = role["name"]!.GetValue<string>(), slug, permissions = new[] { "users.read" }, forbidden = new[] { "roles.read" } });
		await AssertStatusAsync(client.GetAsync($"{this.MembershipUrl}/roles", CancellationToken), HttpStatusCode.Forbidden);
	}
	
	#endregion
	
	#region UBAC
	
	[Fact]
	public async Task Ubac_PermissionOverridesTheRole()
	{
		var (_, client) = await this.LoginAsUserOfRoleAsync(["users.read"], forbidden: ["roles.read"], userPermissions: ["roles.read"]);
		
		await AssertStatusAsync(client.GetAsync($"{this.MembershipUrl}/roles", CancellationToken), HttpStatusCode.OK);
	}
	
	[Fact]
	public async Task Ubac_ForbiddenOverridesTheRole()
	{
		var (_, client) = await this.LoginAsUserOfRoleAsync(["users.read", "roles.read"], userForbidden: ["roles.read"]);
		
		await AssertStatusAsync(client.GetAsync($"{this.MembershipUrl}/users", CancellationToken), HttpStatusCode.OK);
		await AssertStatusAsync(client.GetAsync($"{this.MembershipUrl}/roles", CancellationToken), HttpStatusCode.Forbidden);
	}
	
	#endregion
	
	#region Own Update
	
	[Fact]
	public async Task OwnUpdate_AllowsProfileChangesButNotPrivilegedFields()
	{
		var (user, client) = await this.LoginAsUserOfRoleAsync(["users.read"]);
		var id = user["_id"]!.GetValue<string>();
		var url = $"{this.MembershipUrl}/users/{id}";
		
		var profile = user.DeepClone().AsObject();
		profile.Remove("_id");
		profile["lastname"] = "Smith";
		await AssertStatusAsync(client.PutAsJsonAsync(url, profile, CancellationToken), HttpStatusCode.OK);
		
		var escalation = profile.DeepClone().AsObject();
		escalation["permissions"] = new JsonArray("*");
		await AssertStatusAsync(client.PutAsJsonAsync(url, escalation, CancellationToken), HttpStatusCode.Forbidden);
		
		var roleChange = profile.DeepClone().AsObject();
		roleChange["role"] = "admin";
		await AssertStatusAsync(client.PutAsJsonAsync(url, roleChange, CancellationToken), HttpStatusCode.Forbidden);
		
		var stored = await (await this.AdminResourceClientAsync("users")).GetAsync(id);
		Assert.Equal("Smith", stored["lastname"]!.GetValue<string>());
		Assert.Null(stored["permissions"]);
		Assert.NotEqual("admin", stored["role"]!.GetValue<string>());
	}
	
	#endregion
	
	#region Scopes
	
	[Fact]
	public async Task ScopedToken_IsLimitedToItsScopes()
	{
		var (accessToken, _) = await this._instance.GenerateTokenAsync();
		using var request = new HttpRequestMessage(HttpMethod.Post, "/generate-token")
		{
			Content = JsonContent.Create(new { scopes = new[] { "users.read" } })
		};
		
		request.Headers.Add("Membership", this._instance.MembershipId);
		request.Headers.Add("Authorization", $"Bearer {accessToken}");
		using var response = await this._instance.CreateClient().SendAsync(request, CancellationToken);
		var scoped = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.Created);
		
		var client = this._instance.CreateClient($"Bearer {scoped!["access_token"]!.GetValue<string>()}");
		await AssertStatusAsync(client.GetAsync($"{this.MembershipUrl}/users", CancellationToken), HttpStatusCode.OK);
		await AssertStatusAsync(client.GetAsync($"{this.MembershipUrl}/roles", CancellationToken), HttpStatusCode.Forbidden);
		await AssertStatusAsync(client.PostAsJsonAsync($"{this.MembershipUrl}/users", this.NewUserBody(), CancellationToken), HttpStatusCode.Forbidden);
	}
	
	#endregion
	
	#region Check Permission
	
	[Fact]
	public async Task CheckPermission_ByToken()
	{
		var (_, client) = await this.LoginAsUserOfRoleAsync(["users.read", "roles.read"]);
		var url = $"{this.MembershipUrl}/roles/check-permission";
		
		await AssertStatusAsync(client.GetAsync($"{url}?permission=users.read", CancellationToken), HttpStatusCode.OK);
		await AssertStatusAsync(client.GetAsync($"{url}?permission=users.delete", CancellationToken), HttpStatusCode.Unauthorized);
		await AssertStatusAsync(client.GetAsync(url, CancellationToken), HttpStatusCode.BadRequest);
	}
	
	[Fact]
	public async Task CheckPermission_ByRole()
	{
		var (roleId, _) = await this.CreateRoleAsync(["users.read"], forbidden: ["users.delete"]);
		var adminClient = await this._instance.CreateAdminClientAsync();
		var url = $"{this.MembershipUrl}/roles/{roleId}/check-permission";
		
		await AssertStatusAsync(adminClient.GetAsync($"{url}?permission=users.read", CancellationToken), HttpStatusCode.OK);
		await AssertStatusAsync(adminClient.GetAsync($"{url}?permission=users.delete", CancellationToken), HttpStatusCode.Unauthorized);
		await AssertStatusAsync(adminClient.GetAsync($"{url}?permission=roles.read", CancellationToken), HttpStatusCode.Unauthorized);
	}
	
	#endregion
}