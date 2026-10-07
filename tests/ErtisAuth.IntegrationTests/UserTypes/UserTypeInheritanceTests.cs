using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ErtisAuth.IntegrationTests.Infrastructure;
using ErtisAuth.IntegrationTests.Resources;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ErtisAuth.IntegrationTests.UserTypes;

/// <summary>
/// The inheritance of the user types can not have a cycle, neither on an update nor on a create: the ancestors are walked
/// until the origin user type (a cycle would never end the walk, e.g. on the relations endpoint or on the reference fields of users).
/// A cyclic chain stored before the check is not walked forever either.
/// </summary>
public class UserTypeInheritanceTests : IClassFixture<ErtisAuthInstance>
{
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Properties
	
	private string UserTypesUrl => $"/memberships/{this._instance.MembershipId}/user-types";
	
	private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="instance"></param>
	public UserTypeInheritanceTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private async Task<JsonObject> CreateUserTypeAsync(string? baseType = null)
	{
		var userTypes = new ResourceClient(await this._instance.CreateAdminClientAsync(), this.UserTypesUrl);
		var body = new JsonObject { ["name"] = $"Type {Guid.NewGuid():N}", ["properties"] = new JsonObject() };
		if (baseType != null)
		{
			body["baseType"] = baseType;
		}
		
		return await userTypes.CreateAsync(body);
	}
	
	private async Task<HttpResponseMessage> UpdateBaseTypeAsync(JsonObject userType, string baseType)
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		var body = new JsonObject
		{
			["name"] = userType["name"]!.GetValue<string>(),
			["properties"] = new JsonObject(),
			["baseType"] = baseType
		};
		
		return await adminClient.PutAsJsonAsync($"{this.UserTypesUrl}/{userType["_id"]!.GetValue<string>()}", body, CancellationToken);
	}
	
	/// <summary>
	/// Two user types which are the base types of each other, written directly to the database (as if stored before the cycle check)
	/// </summary>
	private async Task<(JsonObject First, JsonObject Second)> StoreCyclicChainAsync()
	{
		var first = await this.CreateUserTypeAsync();
		var second = await this.CreateUserTypeAsync();
		var collection = this._instance.Database.GetCollection<BsonDocument>("user-types");
		foreach (var (userType, baseType) in new[] { (first, second), (second, first) })
		{
			await collection.UpdateOneAsync(
				Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(userType["_id"]!.GetValue<string>())),
				Builders<BsonDocument>.Update.Set("baseType", baseType["slug"]!.GetValue<string>()),
				cancellationToken: CancellationToken);
		}
		
		return (first, second);
	}
	
	/// <summary>
	/// The relations endpoint walks the ancestors: it must answer (a cycle would never end it)
	/// </summary>
	private async Task AssertRelationsAnswerAsync(JsonObject userType)
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		using var timeout = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		timeout.CancelAfter(TimeSpan.FromSeconds(10));
		using var response = await adminClient.GetAsync($"{this.UserTypesUrl}/relations/{userType["_id"]!.GetValue<string>()}", timeout.Token);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}
	
	#endregion
	
	#region Tests
	
	[Fact]
	public async Task UserType_WithItselfAsTheBaseType_IsRejected()
	{
		var userType = await this.CreateUserTypeAsync();
		
		using var response = await this.UpdateBaseTypeAsync(userType, userType["slug"]!.GetValue<string>());
		
		await ResourceClient.AssertStatusAsync(response, HttpStatusCode.BadRequest);
		await this.AssertRelationsAnswerAsync(userType);
	}
	
	[Fact]
	public async Task UserType_WithItsDescendantAsTheBaseType_IsRejected()
	{
		var parent = await this.CreateUserTypeAsync();
		var child = await this.CreateUserTypeAsync(parent["slug"]!.GetValue<string>());
		var grandchild = await this.CreateUserTypeAsync(child["slug"]!.GetValue<string>());
		
		using var response = await this.UpdateBaseTypeAsync(parent, grandchild["slug"]!.GetValue<string>());
		
		await ResourceClient.AssertStatusAsync(response, HttpStatusCode.BadRequest);
		await this.AssertRelationsAnswerAsync(grandchild);
	}
	
	[Fact]
	public async Task UserType_WithAnotherBaseTypeWithoutACycle_IsUpdated()
	{
		var first = await this.CreateUserTypeAsync();
		var second = await this.CreateUserTypeAsync();
		
		using var response = await this.UpdateBaseTypeAsync(second, first["slug"]!.GetValue<string>());
		
		await ResourceClient.AssertStatusAsync(response, HttpStatusCode.OK);
		await this.AssertRelationsAnswerAsync(second);
	}
	
	[Fact]
	public async Task UserType_CreatedOnACyclicChainStoredBefore_IsRejected()
	{
		var (first, _) = await this.StoreCyclicChainAsync();
		var userTypes = new ResourceClient(await this._instance.CreateAdminClientAsync(), this.UserTypesUrl);
		var body = new JsonObject { ["name"] = $"Type {Guid.NewGuid():N}", ["properties"] = new JsonObject(), ["baseType"] = first["slug"]!.GetValue<string>() };
		
		using var timeout = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		timeout.CancelAfter(TimeSpan.FromSeconds(10));
		using var response = await (await this._instance.CreateAdminClientAsync()).PostAsJsonAsync(userTypes.Url, body, timeout.Token);
		
		var error = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.BadRequest);
		Assert.Equal("UserTypeInheritanceCycle", error!["errorCode"]!.GetValue<string>());
	}
	
	[Fact]
	public async Task Relations_OfACyclicChainStoredBefore_Answer()
	{
		var (first, second) = await this.StoreCyclicChainAsync();
		
		await this.AssertRelationsAnswerAsync(first);
		await this.AssertRelationsAnswerAsync(second);
	}
	
	#endregion
}
