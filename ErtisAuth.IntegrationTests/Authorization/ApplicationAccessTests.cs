using System.Net;
using System.Net.Http.Json;
using ErtisAuth.IntegrationTests.Infrastructure;
using ErtisAuth.IntegrationTests.Resources;

namespace ErtisAuth.IntegrationTests.Authorization;

/// <summary>
/// Applications (M2M) with their own Basic token secret ("applicationId:secret") and a restricted role.
/// </summary>
public class ApplicationAccessTests : IClassFixture<ErtisAuthInstance>
{
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Properties
	
	private string MembershipUrl => $"/memberships/{this._instance.MembershipId}";
	
	private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;
	
	#endregion
	
	#region Constructors
	
	public ApplicationAccessTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private async Task<ResourceClient> AdminResourceClientAsync(string resource) =>
		new(await this._instance.CreateAdminClientAsync(), $"{this.MembershipUrl}/{resource}");
	
	/// <summary>
	/// An application of a new role with the given permissions; the secret is only returned on create.
	/// </summary>
	private async Task<(string Id, string Secret)> CreateApplicationAsync(params string[] permissions)
	{
		var roles = await this.AdminResourceClientAsync("roles");
		var role = await roles.CreateAsync(new { name = $"Service {Guid.NewGuid():N}", permissions });
		
		var applications = await this.AdminResourceClientAsync("applications");
		var application = await applications.CreateAsync(new { name = $"Service {Guid.NewGuid():N}", role = role["slug"]!.GetValue<string>() });
		return (application["_id"]!.GetValue<string>(), application["secret"]!.GetValue<string>());
	}
	
	private HttpClient BasicClient(string applicationId, string secret) => this._instance.CreateClient($"Basic {applicationId}:{secret}");
	
	private static async Task AssertStatusAsync(Task<HttpResponseMessage> request, HttpStatusCode expected)
	{
		using var response = await request;
		await ResourceClient.AssertStatusAsync(response, expected);
	}
	
	#endregion
	
	#region Tests
	
	[Fact]
	public async Task Application_IsLimitedToItsRole()
	{
		var (id, secret) = await this.CreateApplicationAsync("users.read");
		var client = this.BasicClient(id, secret);
		
		await AssertStatusAsync(client.GetAsync($"{this.MembershipUrl}/users", CancellationToken), HttpStatusCode.OK);
		await AssertStatusAsync(client.GetAsync($"{this.MembershipUrl}/roles", CancellationToken), HttpStatusCode.Forbidden);
		await AssertStatusAsync(client.DeleteAsync($"{this.MembershipUrl}/applications/{id}", CancellationToken), HttpStatusCode.Forbidden);
	}
	
	[Fact]
	public async Task VerifyToken_WithTheApplicationSecret()
	{
		var (id, secret) = await this.CreateApplicationAsync("users.read");
		
		await AssertStatusAsync(this.BasicClient(id, secret).GetAsync("/verify-token", CancellationToken), HttpStatusCode.OK);
		await AssertStatusAsync(this.BasicClient(id, "wrong-secret").GetAsync("/verify-token", CancellationToken), HttpStatusCode.Unauthorized);
		await AssertStatusAsync(this._instance.CreateClient().PostAsJsonAsync("/verify-token", new { token = $"Basic {id}:{secret}" }, CancellationToken), HttpStatusCode.OK);
	}
	
	[Fact]
	public async Task Application_MembershipSecretIsNotAccepted()
	{
		var (id, _) = await this.CreateApplicationAsync("users.read");
		var membership = await (await this._instance.CreateAdminClientAsync()).GetFromJsonAsync<System.Text.Json.Nodes.JsonObject>(this.MembershipUrl, CancellationToken);
		var membershipSecret = membership!["secret_key"]?.GetValue<string>();
		Assert.False(string.IsNullOrEmpty(membershipSecret));
		
		await AssertStatusAsync(this.BasicClient(id, membershipSecret).GetAsync($"{this.MembershipUrl}/users", CancellationToken), HttpStatusCode.Unauthorized);
	}
	
	[Fact]
	public async Task RotateSecret_TheOldSecretStopsWorkingImmediately()
	{
		var (id, oldSecret) = await this.CreateApplicationAsync("users.read");
		await AssertStatusAsync(this.BasicClient(id, oldSecret).GetAsync($"{this.MembershipUrl}/users", CancellationToken), HttpStatusCode.OK);
		
		var adminClient = await this._instance.CreateAdminClientAsync();
		using var rotateResponse = await adminClient.PostAsync($"{this.MembershipUrl}/applications/{id}/secret", null, CancellationToken);
		var rotated = await ResourceClient.AssertStatusAsync(rotateResponse, HttpStatusCode.OK);
		var newSecret = rotated!["secret"]!.GetValue<string>();
		
		await AssertStatusAsync(this.BasicClient(id, oldSecret).GetAsync($"{this.MembershipUrl}/users", CancellationToken), HttpStatusCode.Unauthorized);
		await AssertStatusAsync(this.BasicClient(id, newSecret).GetAsync($"{this.MembershipUrl}/users", CancellationToken), HttpStatusCode.OK);
	}
	
	[Fact]
	public async Task DeletedApplication_ItsSecretStopsWorkingImmediately()
	{
		var (id, secret) = await this.CreateApplicationAsync("users.read");
		await AssertStatusAsync(this.BasicClient(id, secret).GetAsync($"{this.MembershipUrl}/users", CancellationToken), HttpStatusCode.OK);
		
		await (await this.AdminResourceClientAsync("applications")).DeleteAsync(id);
		
		await AssertStatusAsync(this.BasicClient(id, secret).GetAsync($"{this.MembershipUrl}/users", CancellationToken), HttpStatusCode.Unauthorized);
	}
	
	[Fact]
	public async Task Application_SecretIsNeverReturnedAfterCreate()
	{
		var (id, _) = await this.CreateApplicationAsync("users.read");
		var applications = await this.AdminResourceClientAsync("applications");
		
		var fetched = await applications.GetAsync(id);
		Assert.Null(fetched["secret"]);
		Assert.Null(fetched["secret_hash"]);
		
		var queried = await applications.QueryAsync(new { where = new Dictionary<string, object> { ["name"] = new Dictionary<string, object> { ["$exists"] = true } } });
		Assert.Contains(id, ResourceClient.IdsOf(queried));
		foreach (var item in queried)
		{
			Assert.Null(item!["secret"]);
			Assert.Null(item["secret_hash"]);
		}
		
		foreach (var item in await applications.ListAsync())
		{
			Assert.Null(item!["secret"]);
			Assert.Null(item["secret_hash"]);
		}
	}
	
	#endregion
}