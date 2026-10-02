using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.IntegrationTests.Infrastructure;
using ErtisAuth.IntegrationTests.Resources;

namespace ErtisAuth.IntegrationTests.UserTypes;

/// <summary>
/// The type system of user types (Ertis.Schema, similar to classes): inheritance, abstract and sealed types, and
/// how the users of a type behave when its schema changes.
/// </summary>
public class UserTypeRulesTests : IClassFixture<ErtisAuthInstance>
{
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Properties
	
	private string MembershipUrl => $"/memberships/{this._instance.MembershipId}";
	
	private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="instance"></param>
	public UserTypeRulesTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private async Task<ResourceClient> AdminResourceClientAsync(string resource) =>
		new(await this._instance.CreateAdminClientAsync(), $"{this.MembershipUrl}/{resource}");
	
	private static JsonObject UserTypeBody(string name, string baseType = "user", JsonObject? properties = null, bool isAbstract = false, bool isSealed = false) => new()
	{
		["name"] = name,
		["baseType"] = baseType,
		["isAbstract"] = isAbstract,
		["isSealed"] = isSealed,
		["properties"] = properties ?? new JsonObject()
	};
	
	private async Task<JsonObject> CreateUserTypeAsync(string name, string baseType = "user", JsonObject? properties = null, bool isAbstract = false, bool isSealed = false)
	{
		var userTypes = await this.AdminResourceClientAsync("user-types");
		return await userTypes.CreateAsync(UserTypeBody(name, baseType, properties, isAbstract, isSealed));
	}
	
	private async Task<HttpResponseMessage> CreateUserAsync(string userType, Action<JsonObject>? configure = null)
	{
		var username = $"user{Guid.NewGuid():N}";
		var body = new JsonObject
		{
			["username"] = username,
			["firstname"] = "Rule",
			["lastname"] = "User",
			["email_address"] = $"{username}@example.com",
			["password"] = "Rule-P@ssw0rd!",
			["role"] = "admin",
			["user_type"] = userType
		};
		
		configure?.Invoke(body);
		var adminClient = await this._instance.CreateAdminClientAsync();
		return await adminClient.PostAsJsonAsync($"{this.MembershipUrl}/users", body, CancellationToken);
	}
	
	private static async Task AssertErrorAsync(HttpResponseMessage response, string errorCode)
	{
		var error = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.BadRequest);
		Assert.Equal(errorCode, error!["errorCode"]!.GetValue<string>());
	}
	
	private static string UniqueName(string prefix) => $"{prefix} {Guid.NewGuid():N}";
	
	#endregion
	
	#region Inheritance
	
	/// <summary>
	/// A derived type has the fields of its base types, and its users are validated with them.
	/// </summary>
	[Fact]
	public async Task DerivedType_InheritsTheFieldsOfItsBaseType()
	{
		var parent = await this.CreateUserTypeAsync(UniqueName("Customer"), properties: new JsonObject
		{
			["loyalty_number"] = new JsonObject { ["type"] = "string", ["isRequired"] = true }
		});
		var child = await this.CreateUserTypeAsync(UniqueName("Premium Customer"), baseType: parent["slug"]!.GetValue<string>(), properties: new JsonObject
		{
			["tier"] = new JsonObject { ["type"] = "enum", ["items"] = new JsonArray(new JsonObject { ["displayName"] = "Gold", ["value"] = "gold" }) }
		});
		var childSlug = child["slug"]!.GetValue<string>();
		
		Assert.NotNull(child["properties"]!["loyalty_number"]);
		Assert.NotNull(child["properties"]!["firstname"]);
		
		using var withoutInheritedField = await this.CreateUserAsync(childSlug, x => x["tier"] = "gold");
		var error = await ResourceClient.AssertStatusAsync(withoutInheritedField, HttpStatusCode.BadRequest);
		Assert.Contains("loyalty_number", error!.ToJsonString());
		
		using var valid = await this.CreateUserAsync(childSlug, x =>
		{
			x["loyalty_number"] = "L-1";
			x["tier"] = "gold";
		});
		await ResourceClient.AssertStatusAsync(valid, HttpStatusCode.Created);
		
		// The declaring type of each field
		var adminClient = await this._instance.CreateAdminClientAsync();
		var relations = await adminClient.GetFromJsonAsync<JsonObject>($"{this.MembershipUrl}/user-types/relations/{child["_id"]!.GetValue<string>()}", CancellationToken);
		Assert.Equal(new[] { "loyalty_number" }, relations![parent["slug"]!.GetValue<string>()]!.AsArray().Select(x => x!.GetValue<string>()));
		Assert.Equal(new[] { "tier" }, relations[childSlug]!.AsArray().Select(x => x!.GetValue<string>()));
		
		// The fields of the origin user type are declared by the origin, not by the topmost custom type of the chain
		Assert.Contains("firstname", relations[UserType.ORIGIN_USER_TYPE_SLUG]!.AsArray().Select(x => x!.GetValue<string>()));
	}
	
	/// <summary>
	/// Derived types refer to their base type by its slug: a base type can not be deleted, and keeps its slug when renamed.
	/// </summary>
	[Fact]
	public async Task BaseTypeOfAnotherType_CanNotBeDeletedAndKeepsItsSlug()
	{
		var parent = await this.CreateUserTypeAsync(UniqueName("Member"));
		var parentId = parent["_id"]!.GetValue<string>();
		var parentSlug = parent["slug"]!.GetValue<string>();
		var child = await this.CreateUserTypeAsync(UniqueName("Honorary Member"), baseType: parentSlug);
		
		var userTypes = await this.AdminResourceClientAsync("user-types");
		var adminClient = await this._instance.CreateAdminClientAsync();
		using var deleteResponse = await adminClient.DeleteAsync($"{userTypes.Url}/{parentId}", CancellationToken);
		await AssertErrorAsync(deleteResponse, "UserTypeCanNotBeDelete");
		
		var renamed = await userTypes.UpdateAsync(parentId, UserTypeBody(UniqueName("Renamed Member")));
		Assert.Equal(parentSlug, renamed["slug"]!.GetValue<string>());
		Assert.Equal(parentSlug, (await userTypes.GetAsync(child["_id"]!.GetValue<string>()))["baseType"]!.GetValue<string>());
	}
	
	[Fact]
	public async Task AbstractType_HasNoUsersButCanBeInherited()
	{
		var abstractType = await this.CreateUserTypeAsync(UniqueName("Person"), isAbstract: true);
		var abstractSlug = abstractType["slug"]!.GetValue<string>();
		
		using var abstractUser = await this.CreateUserAsync(abstractSlug);
		await AssertErrorAsync(abstractUser, "InheritedTypeIsAbstract");
		
		var concrete = await this.CreateUserTypeAsync(UniqueName("Employee"), baseType: abstractSlug);
		using var concreteUser = await this.CreateUserAsync(concrete["slug"]!.GetValue<string>());
		await ResourceClient.AssertStatusAsync(concreteUser, HttpStatusCode.Created);
	}
	
	[Fact]
	public async Task SealedType_CanNotBeInherited()
	{
		var sealedType = await this.CreateUserTypeAsync(UniqueName("Guest"), isSealed: true);
		
		var userTypes = await this.AdminResourceClientAsync("user-types");
		var adminClient = await this._instance.CreateAdminClientAsync();
		using var derived = await adminClient.PostAsJsonAsync(userTypes.Url, UserTypeBody(UniqueName("Vip Guest"), baseType: sealedType["slug"]!.GetValue<string>()), CancellationToken);
		await AssertErrorAsync(derived, "InheritedTypeIsSealed");
		
		using var sealedUser = await this.CreateUserAsync(sealedType["slug"]!.GetValue<string>());
		await ResourceClient.AssertStatusAsync(sealedUser, HttpStatusCode.Created);
	}
	
	[Fact]
	public async Task AbstractAndSealedType_IsRejected()
	{
		var userTypes = await this.AdminResourceClientAsync("user-types");
		var adminClient = await this._instance.CreateAdminClientAsync();
		
		using var response = await adminClient.PostAsJsonAsync(userTypes.Url, UserTypeBody(UniqueName("Impossible"), isAbstract: true, isSealed: true), CancellationToken);
		
		await AssertErrorAsync(response, "UserTypeCannotBeBothAbstractAndSealed");
	}
	
	#endregion
	
	#region Schema Changes
	
	/// <summary>
	/// Accepted behavior: the users of a type are not migrated when a required field is added; they can be read,
	/// and are rejected by the next update until the field is given.
	/// </summary>
	[Fact]
	public async Task RequiredFieldAddedToAType_IsRequiredOnTheNextUpdateOfItsUsers()
	{
		var userType = await this.CreateUserTypeAsync(UniqueName("Trial"), properties: new JsonObject
		{
			["plan"] = new JsonObject { ["type"] = "string" }
		});
		using var createResponse = await this.CreateUserAsync(userType["slug"]!.GetValue<string>());
		var user = (await ResourceClient.AssertStatusAsync(createResponse, HttpStatusCode.Created))!.AsObject();
		var userId = user["_id"]!.GetValue<string>();
		
		var userTypes = await this.AdminResourceClientAsync("user-types");
		var update = UserTypeBody(userType["name"]!.GetValue<string>(), properties: new JsonObject
		{
			["plan"] = new JsonObject { ["type"] = "string" },
			["contract_number"] = new JsonObject { ["type"] = "string", ["isRequired"] = true }
		});
		await userTypes.UpdateAsync(userType["_id"]!.GetValue<string>(), update);
		
		var users = await this.AdminResourceClientAsync("users");
		var stored = await users.GetAsync(userId);
		var userUpdate = stored.DeepClone().AsObject();
		userUpdate.Remove("_id");
		userUpdate["lastname"] = "Changed";
		
		var adminClient = await this._instance.CreateAdminClientAsync();
		using var rejected = await adminClient.PutAsJsonAsync($"{users.Url}/{userId}", userUpdate, CancellationToken);
		var error = await ResourceClient.AssertStatusAsync(rejected, HttpStatusCode.BadRequest);
		Assert.Contains("contract_number", error!.ToJsonString());
		
		userUpdate["contract_number"] = "C-1";
		var updated = await users.UpdateAsync(userId, userUpdate);
		Assert.Equal("Changed", updated["lastname"]!.GetValue<string>());
	}
	
	#endregion
}