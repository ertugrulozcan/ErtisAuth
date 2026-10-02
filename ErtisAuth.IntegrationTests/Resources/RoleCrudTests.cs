using System.Net;
using ErtisAuth.IntegrationTests.Infrastructure;

namespace ErtisAuth.IntegrationTests.Resources;

public class RoleCrudTests : IClassFixture<ErtisAuthInstance>
{
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="instance"></param>
	public RoleCrudTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private async Task<ResourceClient> CreateResourceClientAsync() =>
		new(await this._instance.CreateAdminClientAsync(), $"/memberships/{this._instance.MembershipId}/roles");
	
	private static object RoleBody(string name, string description) => new
	{
		name,
		description,
		permissions = new[] { "users.read", "roles.read" },
		forbidden = new[] { "users.delete" }
	};
	
	#endregion
	
	#region Tests
	
	[Fact]
	public async Task Role_CrudRoundTrip()
	{
		var roles = await this.CreateResourceClientAsync();
		var name = $"Editor {Guid.NewGuid():N}";
		
		var created = await roles.CreateAsync(RoleBody(name, "Edits content"));
		var id = created["_id"]!.GetValue<string>();
		var slug = created["slug"]!.GetValue<string>();
		Assert.Equal(this._instance.MembershipId, created["membership_id"]!.GetValue<string>());
		Assert.Equal(["users.read", "roles.read"], created["permissions"]!.AsArray().Select(x => x!.GetValue<string>()));
		Assert.Equal(["users.delete"], created["forbidden"]!.AsArray().Select(x => x!.GetValue<string>()));
		
		var fetched = await roles.GetAsync(id);
		Assert.Equal(name, fetched["name"]!.GetValue<string>());
		Assert.Equal("Edits content", fetched["description"]!.GetValue<string>());
		
		Assert.Contains(id, ResourceClient.IdsOf(await roles.ListAsync()));
		Assert.Equal([id], ResourceClient.IdsOf(await roles.QueryAsync(new { where = new { slug } })));
		
		var updated = await roles.UpdateAsync(id, RoleBody(name, "Edits and publishes content"));
		Assert.Equal(id, updated["_id"]!.GetValue<string>());
		Assert.Equal("Edits and publishes content", (await roles.GetAsync(id))["description"]!.GetValue<string>());
		
		await roles.DeleteAsync(id);
		await roles.AssertNotFoundAsync(id, "RoleNotFound");
	}
	
	[Fact]
	public async Task Role_BulkDelete_DeletesAll()
	{
		var roles = await this.CreateResourceClientAsync();
		var first = (await roles.CreateAsync(RoleBody($"First {Guid.NewGuid():N}", "First")))["_id"]!.GetValue<string>();
		var second = (await roles.CreateAsync(RoleBody($"Second {Guid.NewGuid():N}", "Second")))["_id"]!.GetValue<string>();
		
		using var response = await roles.BulkDeleteAsync(first, second);
		await ResourceClient.AssertStatusAsync(response, HttpStatusCode.NoContent);
		
		await roles.AssertNotFoundAsync(first, "RoleNotFound");
		await roles.AssertNotFoundAsync(second, "RoleNotFound");
	}
	
	[Fact]
	public async Task Role_Search_FindsByKeyword()
	{
		var roles = await this.CreateResourceClientAsync();
		var name = $"Searchable {Guid.NewGuid():N}";
		var id = (await roles.CreateAsync(RoleBody(name, "Searchable")))["_id"]!.GetValue<string>();
		
		var found = await roles.SearchAsync(name);
		Assert.Contains(id, ResourceClient.IdsOf(found));
	}
	
	#endregion
}