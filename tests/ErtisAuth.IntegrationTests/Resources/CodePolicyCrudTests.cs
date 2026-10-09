using System.Net;
using ErtisAuth.IntegrationTests.Infrastructure;

namespace ErtisAuth.IntegrationTests.Resources;

public class CodePolicyCrudTests : IClassFixture<ErtisAuthInstance>
{
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="instance"></param>
	public CodePolicyCrudTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private async Task<ResourceClient> CreateResourceClientAsync() =>
		new(await this._instance.CreateAdminClientAsync(), $"/memberships/{this._instance.MembershipId}/code-policies");
	
	private static object PolicyBody(string name, int length) => new
	{
		name,
		description = "Kiosk login",
		length,
		contains_letters = true,
		contains_digits = true,
		expires_in = 300
	};
	
	#endregion
	
	#region Tests
	
	[Fact]
	public async Task CodePolicy_CrudRoundTrip()
	{
		var policies = await this.CreateResourceClientAsync();
		var name = $"Kiosk {Guid.NewGuid():N}";
		
		var created = await policies.CreateAsync(PolicyBody(name, 6));
		var id = created["_id"]!.GetValue<string>();
		var slug = created["slug"]!.GetValue<string>();
		Assert.Equal(this._instance.MembershipId, created["membership_id"]!.GetValue<string>());
		
		var fetched = await policies.GetAsync(id);
		Assert.Equal(name, fetched["name"]!.GetValue<string>());
		Assert.Equal(6, fetched["length"]!.GetValue<int>());
		Assert.True(fetched["contains_letters"]!.GetValue<bool>());
		Assert.Equal(300, fetched["expires_in"]!.GetValue<int>());
		
		Assert.Contains(id, ResourceClient.IdsOf(await policies.ListAsync()));
		Assert.Equal([id], ResourceClient.IdsOf(await policies.QueryAsync(new { where = new { slug } })));
		
		var updated = await policies.UpdateAsync(id, PolicyBody(name, 8));
		Assert.Equal(id, updated["_id"]!.GetValue<string>());
		Assert.Equal(8, (await policies.GetAsync(id))["length"]!.GetValue<int>());
		
		await policies.DeleteAsync(id);
		await policies.AssertNotFoundAsync(id, "TokenCodePolicyNotFound");
	}
	
	[Fact]
	public async Task CodePolicy_BulkDelete_DeletesAll()
	{
		var policies = await this.CreateResourceClientAsync();
		var first = (await policies.CreateAsync(PolicyBody($"First {Guid.NewGuid():N}", 6)))["_id"]!.GetValue<string>();
		var second = (await policies.CreateAsync(PolicyBody($"Second {Guid.NewGuid():N}", 6)))["_id"]!.GetValue<string>();
		
		using var response = await policies.BulkDeleteAsync(first, second);
		await ResourceClient.AssertStatusAsync(response, HttpStatusCode.NoContent);
		
		await policies.AssertNotFoundAsync(first, "TokenCodePolicyNotFound");
		await policies.AssertNotFoundAsync(second, "TokenCodePolicyNotFound");
	}
	
	#endregion
}