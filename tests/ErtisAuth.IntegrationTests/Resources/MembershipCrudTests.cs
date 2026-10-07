using System.Net;
using ErtisAuth.IntegrationTests.Infrastructure;

namespace ErtisAuth.IntegrationTests.Resources;

/// <summary>
/// Memberships have no bulk delete endpoint.
/// </summary>
public class MembershipCrudTests : IClassFixture<ErtisAuthInstance>
{
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="instance"></param>
	public MembershipCrudTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private async Task<ResourceClient> CreateResourceClientAsync() =>
		new(await this._instance.CreateAdminClientAsync(), "/memberships");
	
	private static object MembershipBody(string name, string slug, int expiresIn) => new
	{
		name,
		slug,
		secret_key = "tenant-membership-secret-key-tenant-membership-secret",
		expires_in = expiresIn,
		refresh_token_expires_in = 86400,
		hash_algorithm = "ARGON2ID",
		encoding = "UTF-8",
		default_language = "en",
		user_activation = "passive",
		otp_settings = new { host = "https://example.com", policy = new { length = 6, contains_digits = true, expires_in = 300, max_attempts = 3 } }
	};
	
	#endregion
	
	#region Tests
	
	[Fact]
	public async Task Membership_CrudRoundTrip()
	{
		var memberships = await this.CreateResourceClientAsync();
		var name = $"Tenant {Guid.NewGuid():N}";
		var slug = $"tenant-{Guid.NewGuid():N}";
		
		var created = await memberships.CreateAsync(MembershipBody(name, slug, 3600));
		var id = created["_id"]!.GetValue<string>();
		Assert.Equal(slug, created["slug"]!.GetValue<string>());
		
		var fetched = await memberships.GetAsync(id);
		Assert.Equal(name, fetched["name"]!.GetValue<string>());
		Assert.Equal(3600, fetched["expires_in"]!.GetValue<int>());
		Assert.Equal("ARGON2ID", fetched["hash_algorithm"]!.GetValue<string>());
		Assert.Equal(300, fetched["otp_settings"]!["policy"]!["expires_in"]!.GetValue<int>());
		
		Assert.Contains(id, ResourceClient.IdsOf(await memberships.ListAsync()));
		Assert.Equal([id], ResourceClient.IdsOf(await memberships.QueryAsync(new { where = new { slug } })));
		
		var updated = await memberships.UpdateAsync(id, MembershipBody(name, slug, 7200));
		Assert.Equal(id, updated["_id"]!.GetValue<string>());
		Assert.Equal(7200, (await memberships.GetAsync(id))["expires_in"]!.GetValue<int>());
		
		await memberships.DeleteAsync(id);
		await memberships.AssertNotFoundAsync(id, "MembershipNotFound");
	}
	
	[Fact]
	public async Task Membership_Settings()
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		
		foreach (var path in new[] { "settings", "settings/encodings", "settings/encodings/default", "settings/hash-algorithms", "settings/hash-algorithms/default", "settings/db-locales", "settings/db-locales/default" })
		{
			using var response = await adminClient.GetAsync($"/memberships/{path}", TestContext.Current.CancellationToken);
			var body = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.OK);
			Assert.NotNull(body);
		}
	}
	
	[Fact]
	public async Task Membership_Search_FindsByKeyword()
	{
		var memberships = await this.CreateResourceClientAsync();
		var name = $"Searchable {Guid.NewGuid():N}";
		var id = (await memberships.CreateAsync(MembershipBody(name, $"searchable-{Guid.NewGuid():N}", 3600)))["_id"]!.GetValue<string>();
		
		var found = await memberships.SearchAsync(name);
		Assert.Contains(id, ResourceClient.IdsOf(found));
	}
	
	#endregion
}