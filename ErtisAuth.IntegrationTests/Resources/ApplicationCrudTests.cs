using System.Net;
using ErtisAuth.IntegrationTests.Infrastructure;

namespace ErtisAuth.IntegrationTests.Resources;

public class ApplicationCrudTests : IClassFixture<ErtisAuthInstance>
{
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="instance"></param>
	public ApplicationCrudTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private async Task<ResourceClient> CreateResourceClientAsync() =>
		new(await this._instance.CreateAdminClientAsync(), $"/memberships/{this._instance.MembershipId}/applications");
	
	#endregion
	
	#region Tests
	
	[Fact]
	public async Task Application_CrudRoundTrip()
	{
		var applications = await this.CreateResourceClientAsync();
		var name = $"Worker {Guid.NewGuid():N}";
		
		var created = await applications.CreateAsync(new { name, role = "admin" });
		var id = created["_id"]!.GetValue<string>();
		var slug = created["slug"]!.GetValue<string>();
		Assert.Equal(this._instance.MembershipId, created["membership_id"]!.GetValue<string>());
		Assert.Equal("admin", created["role"]!.GetValue<string>());
		
		var fetched = await applications.GetAsync(id);
		Assert.Equal(name, fetched["name"]!.GetValue<string>());
		
		Assert.Contains(id, ResourceClient.IdsOf(await applications.ListAsync()));
		Assert.Equal([id], ResourceClient.IdsOf(await applications.QueryAsync(new { where = new { slug } })));
		
		var renamed = $"{name} Renamed";
		var updated = await applications.UpdateAsync(id, new { name = renamed, slug, role = "admin" });
		Assert.Equal(id, updated["_id"]!.GetValue<string>());
		Assert.Equal(renamed, (await applications.GetAsync(id))["name"]!.GetValue<string>());
		
		await applications.DeleteAsync(id);
		await applications.AssertNotFoundAsync(id, "ApplicationNotFound");
	}
	
	[Fact]
	public async Task Application_RotateSecret_ReturnsANewSecret()
	{
		var applications = await this.CreateResourceClientAsync();
		var created = await applications.CreateAsync(new { name = $"Rotating {Guid.NewGuid():N}", role = "admin" });
		var id = created["_id"]!.GetValue<string>();
		
		var adminClient = await this._instance.CreateAdminClientAsync();
		using var firstResponse = await adminClient.PostAsync($"{applications.Url}/{id}/secret", null, TestContext.Current.CancellationToken);
		var first = await ResourceClient.AssertStatusAsync(firstResponse, HttpStatusCode.OK);
		using var secondResponse = await adminClient.PostAsync($"{applications.Url}/{id}/secret", null, TestContext.Current.CancellationToken);
		var second = await ResourceClient.AssertStatusAsync(secondResponse, HttpStatusCode.OK);
		
		var firstSecret = first!["secret"]!.GetValue<string>();
		var secondSecret = second!["secret"]!.GetValue<string>();
		Assert.False(string.IsNullOrEmpty(firstSecret));
		Assert.NotEqual(firstSecret, secondSecret);
	}
	
	[Fact]
	public async Task Application_BulkDelete_DeletesAll()
	{
		var applications = await this.CreateResourceClientAsync();
		var first = (await applications.CreateAsync(new { name = $"First {Guid.NewGuid():N}", role = "admin" }))["_id"]!.GetValue<string>();
		var second = (await applications.CreateAsync(new { name = $"Second {Guid.NewGuid():N}", role = "admin" }))["_id"]!.GetValue<string>();
		
		using var response = await applications.BulkDeleteAsync(first, second);
		await ResourceClient.AssertStatusAsync(response, HttpStatusCode.NoContent);
		
		await applications.AssertNotFoundAsync(first, "ApplicationNotFound");
		await applications.AssertNotFoundAsync(second, "ApplicationNotFound");
	}
	
	[Fact]
	public async Task Application_Search_FindsByKeyword()
	{
		var applications = await this.CreateResourceClientAsync();
		var name = $"Searchable {Guid.NewGuid():N}";
		var id = (await applications.CreateAsync(new { name, role = "admin" }))["_id"]!.GetValue<string>();
		
		var found = await applications.SearchAsync(name);
		Assert.Contains(id, ResourceClient.IdsOf(found));
	}
	
	#endregion
}