using System.Net;
using System.Text.Json.Nodes;
using ErtisAuth.IntegrationTests.Infrastructure;

namespace ErtisAuth.IntegrationTests.Resources;

public class UserCrudTests : IClassFixture<ErtisAuthInstance>
{
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Constructors
	
	public UserCrudTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private async Task<ResourceClient> CreateResourceClientAsync() =>
		new(await this._instance.CreateAdminClientAsync(), $"/memberships/{this._instance.MembershipId}/users");
	
	private static JsonObject UserBody(string username, string lastname) => new()
	{
		["username"] = username,
		["firstname"] = "Jane",
		["lastname"] = lastname,
		["email_address"] = $"{username}@example.com",
		["password"] = "J@ne-P@ssw0rd!",
		["role"] = "admin",
		["user_type"] = "user"
	};
	
	private static void AssertNoPasswordFields(JsonObject user)
	{
		Assert.False(user.ContainsKey("password"), user.ToJsonString());
		Assert.False(user.ContainsKey("password_hash"), user.ToJsonString());
	}
	
	#endregion
	
	#region Tests
	
	[Fact]
	public async Task User_CrudRoundTrip()
	{
		var users = await this.CreateResourceClientAsync();
		var username = $"jane{Guid.NewGuid():N}";
		
		var created = await users.CreateAsync(UserBody(username, "Doe"));
		var id = created["_id"]!.GetValue<string>();
		Assert.Equal(this._instance.MembershipId, created["membership_id"]!.GetValue<string>());
		AssertNoPasswordFields(created);
		
		var fetched = await users.GetAsync(id);
		Assert.Equal(username, fetched["username"]!.GetValue<string>());
		Assert.Equal("Doe", fetched["lastname"]!.GetValue<string>());
		Assert.Equal("user", fetched["user_type"]!.GetValue<string>());
		AssertNoPasswordFields(fetched);
		
		var listed = await users.ListAsync();
		Assert.Contains(id, ResourceClient.IdsOf(listed));
		Assert.All(listed.Select(x => x!.AsObject()), AssertNoPasswordFields);
		
		var queried = await users.QueryAsync(new { where = new { username } });
		Assert.Equal([id], ResourceClient.IdsOf(queried));
		AssertNoPasswordFields(queried[0]!.AsObject());
		
		var update = UserBody(username, "Smith");
		update.Remove("password");
		var updated = await users.UpdateAsync(id, update);
		Assert.Equal(id, updated["_id"]!.GetValue<string>());
		AssertNoPasswordFields(updated);
		Assert.Equal("Smith", (await users.GetAsync(id))["lastname"]!.GetValue<string>());
		
		// The password was not touched by the update
		var (accessToken, _) = await this._instance.GenerateTokenAsync(username, "J@ne-P@ssw0rd!");
		Assert.False(string.IsNullOrEmpty(accessToken));
		
		await users.DeleteAsync(id);
		await users.AssertNotFoundAsync(id, "UserNotFound");
	}
	
	[Fact]
	public async Task User_BulkDelete_DeletesAll()
	{
		var users = await this.CreateResourceClientAsync();
		var first = (await users.CreateAsync(UserBody($"first{Guid.NewGuid():N}", "First")))["_id"]!.GetValue<string>();
		var second = (await users.CreateAsync(UserBody($"second{Guid.NewGuid():N}", "Second")))["_id"]!.GetValue<string>();
		
		using var response = await users.BulkDeleteAsync(first, second);
		await ResourceClient.AssertStatusAsync(response, HttpStatusCode.NoContent);
		
		await users.AssertNotFoundAsync(first, "UserNotFound");
		await users.AssertNotFoundAsync(second, "UserNotFound");
	}
	
	[Fact]
	public async Task User_Search_FindsByKeyword()
	{
		var users = await this.CreateResourceClientAsync();
		var name = $"searchable{Guid.NewGuid():N}";
		var id = (await users.CreateAsync(UserBody(name, "Searchable")))["_id"]!.GetValue<string>();
		
		var found = await users.SearchAsync(name);
		Assert.Contains(id, ResourceClient.IdsOf(found));
		Assert.All(found.Select(x => x!.AsObject()), AssertNoPasswordFields);
	}
	
	#endregion
}