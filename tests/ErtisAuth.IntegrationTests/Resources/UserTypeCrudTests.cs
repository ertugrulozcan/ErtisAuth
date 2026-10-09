using ErtisAuth.IntegrationTests.Infrastructure;

namespace ErtisAuth.IntegrationTests.Resources;

/// <summary>
/// User types have no search and no bulk delete endpoint.
/// </summary>
public class UserTypeCrudTests : IClassFixture<ErtisAuthInstance>
{
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="instance"></param>
	public UserTypeCrudTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private async Task<ResourceClient> CreateResourceClientAsync() =>
		new(await this._instance.CreateAdminClientAsync(), $"/memberships/{this._instance.MembershipId}/user-types");
	
	private static object UserTypeBody(string name, string description) => new
	{
		name,
		description,
		properties = new Dictionary<string, object>
		{
			["loyalty_number"] = new { type = "string", displayName = "Loyalty Number", isRequired = true },
			["tier"] = new { type = "integer", displayName = "Tier", minimum = 1, maximum = 3 }
		},
		baseType = "user"
	};
	
	#endregion
	
	#region Tests
	
	[Fact]
	public async Task UserType_CrudRoundTrip()
	{
		var userTypes = await this.CreateResourceClientAsync();
		var name = $"Customer {Guid.NewGuid():N}";
		
		var created = await userTypes.CreateAsync(UserTypeBody(name, "Customers"));
		var id = created["_id"]!.GetValue<string>();
		var slug = created["slug"]!.GetValue<string>();
		Assert.Equal(this._instance.MembershipId, created["membership_id"]!.GetValue<string>());
		
		var fetched = await userTypes.GetAsync(id);
		Assert.Equal(name, fetched["name"]!.GetValue<string>());
		Assert.Equal("user", fetched["baseType"]!.GetValue<string>());
		var loyaltyNumber = fetched["properties"]!["loyalty_number"]!;
		Assert.Equal("string", loyaltyNumber["type"]!.GetValue<string>());
		Assert.True(loyaltyNumber["isRequired"]!.GetValue<bool>());
		Assert.Equal(3, fetched["properties"]!["tier"]!["maximum"]!.GetValue<int>());
		
		Assert.Contains(id, ResourceClient.IdsOf(await userTypes.ListAsync()));
		Assert.Equal([id], ResourceClient.IdsOf(await userTypes.QueryAsync(new { where = new { slug } })));
		
		var updated = await userTypes.UpdateAsync(id, UserTypeBody(name, "Paying customers"));
		Assert.Equal(id, updated["_id"]!.GetValue<string>());
		Assert.Equal(slug, updated["slug"]!.GetValue<string>());
		Assert.Equal("Paying customers", (await userTypes.GetAsync(id))["description"]!.GetValue<string>());
		
		await userTypes.DeleteAsync(id);
		await userTypes.AssertNotFoundAsync(id, "UserTypeNotFound");
	}
	
	#endregion
}