using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ErtisAuth.IntegrationTests.Infrastructure;
using ErtisAuth.IntegrationTests.Resources;
using MongoDB.Bson;

namespace ErtisAuth.IntegrationTests.UserTypes;

/// <summary>
/// A user type with the structure of a production one (synthetic names and values; Data/sample-user-type.json as the API
/// returns it, Data/sample-user-type.bson.js as it is stored) and the validation of its users through the API:
/// primitives, enums, nested objects and arrays (objects in arrays in objects).
/// </summary>
public class MemberUserTypeTests : IClassFixture<ErtisAuthInstance>
{
	#region Constants
	
	private const string Password = "Member-P@ssw0rd!";
	
	#endregion
	
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Properties
	
	private string MembershipUrl => $"/memberships/{this._instance.MembershipId}";
	
	private static string DataDirectory => Path.Combine(AppContext.BaseDirectory, "UserTypes", "Data");
	
	private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="instance"></param>
	public MemberUserTypeTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private static JsonObject ReadMemberUserType() => JsonNode.Parse(File.ReadAllText(Path.Combine(DataDirectory, "sample-user-type.json")))!.AsObject();
	
	private async Task<ResourceClient> AdminResourceClientAsync(string resource) =>
		new(await this._instance.CreateAdminClientAsync(), $"{this.MembershipUrl}/{resource}");
	
	/// <summary>
	/// Creates the member user type once for the installation (the tests of the class share it).
	/// </summary>
	private async Task<JsonObject> EnsureMemberUserTypeAsync()
	{
		var userTypes = await this.AdminResourceClientAsync("user-types");
		var existing = await userTypes.QueryAsync(new { where = new { slug = "member" } });
		if (existing.Count > 0)
		{
			return existing[0]!.AsObject();
		}
		
		return await userTypes.CreateAsync(ReadMemberUserType());
	}
	
	/// <summary>
	/// A member with every nested structure of the type filled in.
	/// </summary>
	private static JsonObject MemberBody()
	{
		var username = $"member{Guid.NewGuid():N}";
		return new JsonObject
		{
			["username"] = username,
			["firstname"] = "Mia",
			["lastname"] = "Member",
			["email_address"] = $"{username}@example.com",
			["password"] = Password,
			["role"] = "admin",
			["user_type"] = "member",
			["phone_number"] = $"+1555{Random.Shared.NextInt64(1000000, 9999999)}",
			["avatar"] = new JsonObject { ["url"] = "https://cdn.example.com/avatars/mia.png" },
			["notification_preferences"] = new JsonObject { ["sms"] = true, ["email"] = false },
			["consents"] = new JsonObject
			{
				["terms"] = new JsonObject { ["document_id"] = "terms", ["version"] = 3, ["accepted"] = true },
				["privacy"] = new JsonObject { ["document_id"] = "privacy", ["version"] = 2, ["accepted"] = true },
				["cookies"] = new JsonObject { ["document_id"] = "cookies", ["version"] = 1, ["accepted"] = false }
			},
			["paid_user"] = true,
			["signup_platform"] = "ios",
			["region"] = "north",
			["billing_channel"] = "card",
			["subscriptions"] = new JsonArray
			{
				new JsonObject
				{
					["product_id"] = "premium-monthly",
					["tier"] = "premium",
					["activation_date"] = "2026-01-15T10:00:00Z",
					["status"] = "active"
				}
			},
			["coupons"] = new JsonArray
			{
				new JsonObject
				{
					["code"] = "WELCOME",
					["type"] = "coupon",
					["discount"] = new JsonObject { ["interval"] = "month", ["amount"] = 3, ["percentage"] = 50 },
					["free_trial"] = new JsonObject { ["interval"] = "week", ["amount"] = 1 },
					["redeem_date"] = "2026-01-15T10:00:00Z"
				}
			},
			["profile_settings"] = new JsonObject
			{
				["pin_code"] = "1234",
				["profiles"] = new JsonArray
				{
					new JsonObject
					{
						["id"] = "profile-1",
						["name"] = "Kids",
						["color"] = "#ff0000",
						["parental_control"] = new JsonObject { ["enabled"] = true, ["rate"] = "teen" },
						["locale"] = "en-US"
					}
				}
			},
			["parental_control"] = new JsonObject { ["is_active"] = false, ["rate"] = "all_ages" }
		};
	}
	
	private async Task<HttpResponseMessage> CreateMemberAsync(JsonObject body)
	{
		await this.EnsureMemberUserTypeAsync();
		var adminClient = await this._instance.CreateAdminClientAsync();
		return await adminClient.PostAsJsonAsync($"{this.MembershipUrl}/users", body, CancellationToken);
	}
	
	/// <summary>
	/// 400 with a validation error on the given field.
	/// </summary>
	private static async Task AssertValidationErrorAsync(HttpResponseMessage response, string fieldName)
	{
		var error = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.BadRequest);
		var json = error!.ToJsonString();
		Assert.Contains(fieldName, json);
	}
	
	/// <summary>
	/// Array item schemas come back with the name '$schema' (added by Ertis.Schema, not in the production API output).
	/// </summary>
	private static JsonNode? WithoutItemSchemaNames(JsonNode? node)
	{
		var copy = node?.DeepClone();
		RemoveItemSchemaNames(copy);
		return copy;
	}
	
	private static void RemoveItemSchemaNames(JsonNode? node)
	{
		switch (node)
		{
			case JsonObject obj:
			{
				if (obj["itemSchema"] is JsonObject itemSchema && itemSchema["name"]?.GetValue<string>() == "$schema")
				{
					itemSchema.Remove("name");
				}
				
				foreach (var child in obj.Select(x => x.Value).ToArray())
				{
					RemoveItemSchemaNames(child);
				}
				
				break;
			}
			case JsonArray array:
			{
				foreach (var item in array)
				{
					RemoveItemSchemaNames(item);
				}
				
				break;
			}
		}
	}
	
	#endregion
	
	#region Format Compatibility
	
	/// <summary>
	/// Every field definition (enum items, item schemas, minimum/maximum, formatPattern, appearance, defaultValue,
	/// uniqueBy, nested objects and arrays) survives the API round trip.
	/// </summary>
	[Fact]
	public async Task MemberUserType_RoundTripsThroughTheApi()
	{
		var created = await this.EnsureMemberUserTypeAsync();
		var userTypes = await this.AdminResourceClientAsync("user-types");
		
		var fetched = await userTypes.GetAsync(created["_id"]!.GetValue<string>());
		
		var expected = ReadMemberUserType()["properties"]!.AsObject();
		var actual = fetched["properties"]!.AsObject();
		Assert.Equal(expected.Select(x => x.Key).Order(), actual.Select(x => x.Key).Order());
		foreach (var (name, definition) in expected)
		{
			Assert.True(JsonNode.DeepEquals(definition, WithoutItemSchemaNames(actual[name])), $"'{name}' differs: {actual[name]?.ToJsonString()}");
		}
		
		// What the admin panel reads can be sent back as it is
		var update = fetched.DeepClone().AsObject();
		foreach (var key in new[] { "_id", "membership_id", "sys" })
		{
			update.Remove(key);
		}
		
		update["description"] = "Updated from the fetched model";
		var updated = await userTypes.UpdateAsync(created["_id"]!.GetValue<string>(), update);
		Assert.Equal("Updated from the fetched model", updated["description"]!.GetValue<string>());
	}
	
	/// <summary>
	/// A stored document (as mongosh prints it, integers as NumberInt) is read by the API and its users can be created.
	/// </summary>
	[Fact]
	public async Task StoredMemberUserType_IsReadable()
	{
		// ReSharper disable once MethodHasAsyncOverload
		var document = BsonDocument.Parse(File.ReadAllText(Path.Combine(DataDirectory, "sample-user-type.bson.js")));
		document["_id"] = ObjectId.GenerateNewId();
		document["name"] = "Stored Member";
		document["slug"] = "stored-member";
		document["membership_id"] = this._instance.MembershipId;
		await this._instance.Database.GetCollection<BsonDocument>("user-types").InsertOneAsync(document, cancellationToken: CancellationToken);
		
		var userTypes = await this.AdminResourceClientAsync("user-types");
		var fetched = await userTypes.GetAsync(document["_id"].ToString()!);
		var percentage = fetched["properties"]!["coupons"]!["itemSchema"]!["properties"]!["discount"]!["properties"]!["percentage"]!;
		Assert.Equal(0, percentage["minimum"]!.GetValue<int>());
		Assert.Equal(100, percentage["maximum"]!.GetValue<int>());
		
		var body = MemberBody();
		body["user_type"] = "stored-member";
		var adminClient = await this._instance.CreateAdminClientAsync();
		using var response = await adminClient.PostAsJsonAsync($"{this.MembershipUrl}/users", body, CancellationToken);
		await ResourceClient.AssertStatusAsync(response, HttpStatusCode.Created);
	}
	
	#endregion
	
	#region Member Validation
	
	[Fact]
	public async Task Member_WithEveryNestedStructure_IsStoredAsSent()
	{
		var body = MemberBody();
		
		using var response = await this.CreateMemberAsync(body);
		var created = (await ResourceClient.AssertStatusAsync(response, HttpStatusCode.Created))!.AsObject();
		
		var users = await this.AdminResourceClientAsync("users");
		var fetched = await users.GetAsync(created["_id"]!.GetValue<string>());
		foreach (var field in new[] { "avatar", "notification_preferences", "consents", "region", "paid_user", "profile_settings", "parental_control" })
		{
			Assert.True(JsonNode.DeepEquals(body[field], fetched[field]), $"'{field}' differs: {fetched[field]?.ToJsonString()}");
		}
		
		var subscription = fetched["subscriptions"]![0]!;
		Assert.Equal("premium", subscription["tier"]!.GetValue<string>());
		Assert.Equal(DateTime.Parse("2026-01-15T10:00:00Z").ToUniversalTime(), subscription["activation_date"]!.GetValue<DateTime>().ToUniversalTime());
		
		var coupon = fetched["coupons"]![0]!;
		Assert.Equal(50, coupon["discount"]!["percentage"]!.GetValue<int>());
		Assert.Equal(1, coupon["free_trial"]!["amount"]!.GetValue<int>());
	}
	
	[Theory]
	[InlineData("region", "center")]
	[InlineData("signup_platform", "windows")]
	[InlineData("paid_user", "yes")]
	[InlineData("paid_user", 1)]
	public async Task Member_WithInvalidTopLevelValue_IsRejected(string field, object value)
	{
		var body = MemberBody();
		body[field] = JsonValue.Create(value);
		
		using var response = await this.CreateMemberAsync(body);
		
		await AssertValidationErrorAsync(response, field);
	}
	
	/// <summary>
	/// Integer minimum/maximum are enforced deep inside arrays and objects, like string maxLength, enums and additional properties.
	/// </summary>
	[Fact]
	public async Task Member_WithPercentageOutOfRange_IsRejected()
	{
		var body = MemberBody();
		body["coupons"]![0]!["discount"]!["percentage"] = 150;
		
		using var response = await this.CreateMemberAsync(body);
		
		await AssertValidationErrorAsync(response, "percentage");
	}
	
	[Fact]
	public async Task Member_WithInvalidEnumDeepInsideAnArray_IsRejected()
	{
		var body = MemberBody();
		body["profile_settings"]!["profiles"]![0]!["parental_control"]!["rate"] = "everyone";
		
		using var response = await this.CreateMemberAsync(body);
		
		await AssertValidationErrorAsync(response, "rate");
	}
	
	[Fact]
	public async Task Member_WithInvalidUri_IsRejected()
	{
		var body = MemberBody();
		body["avatar"]!["url"] = "not a uri";
		
		using var response = await this.CreateMemberAsync(body);
		
		await AssertValidationErrorAsync(response, "url");
	}
	
	[Fact]
	public async Task Member_WithInvalidDateTime_IsRejected()
	{
		var body = MemberBody();
		body["subscriptions"]![0]!["activation_date"] = "yesterday";
		
		using var response = await this.CreateMemberAsync(body);
		
		await AssertValidationErrorAsync(response, "activation_date");
	}
	
	[Fact]
	public async Task Member_WithoutRequiredField_IsRejected()
	{
		var body = MemberBody();
		body.Remove("firstname");
		
		using var response = await this.CreateMemberAsync(body);
		
		await AssertValidationErrorAsync(response, "firstname");
	}
	
	/// <summary>
	/// allowAdditionalProperties is false: fields that are not in the schema are rejected, also in nested objects.
	/// </summary>
	[Fact]
	public async Task Member_WithAdditionalProperty_IsRejected()
	{
		var topLevel = MemberBody();
		topLevel["nickname"] = "mia";
		using var topLevelResponse = await this.CreateMemberAsync(topLevel);
		await AssertValidationErrorAsync(topLevelResponse, "nickname");
		
		var nested = MemberBody();
		nested["notification_preferences"]!["push"] = true;
		using var nestedResponse = await this.CreateMemberAsync(nested);
		await AssertValidationErrorAsync(nestedResponse, "push");
	}
	
	[Theory]
	[InlineData("phone_number")]
	[InlineData("email_address")]
	public async Task Member_WithValueOfAUniqueFieldInUse_IsRejected(string field)
	{
		var first = MemberBody();
		using var firstResponse = await this.CreateMemberAsync(first);
		await ResourceClient.AssertStatusAsync(firstResponse, HttpStatusCode.Created);
		
		var second = MemberBody();
		second[field] = first[field]!.DeepClone();
		using var secondResponse = await this.CreateMemberAsync(second);
		
		await AssertValidationErrorAsync(secondResponse, field);
	}
	
	/// <summary>
	/// Characterization: formatPattern ('{{firstname}}' for customer_id) does not fill the field when a user is created.
	/// </summary>
	[Fact]
	public async Task Member_FormatPatternField_IsNotFilledByTheApi()
	{
		using var response = await this.CreateMemberAsync(MemberBody());
		
		var created = (await ResourceClient.AssertStatusAsync(response, HttpStatusCode.Created))!.AsObject();
		Assert.Null(created["customer_id"]);
	}
	
	[Fact]
	public async Task MemberUpdate_IsValidatedToo()
	{
		using var response = await this.CreateMemberAsync(MemberBody());
		var created = (await ResourceClient.AssertStatusAsync(response, HttpStatusCode.Created))!.AsObject();
		var id = created["_id"]!.GetValue<string>();
		var users = await this.AdminResourceClientAsync("users");
		
		var update = (await users.GetAsync(id)).DeepClone().AsObject();
		update.Remove("_id");
		update["coupons"]![0]!["discount"]!["percentage"] = 75;
		var updated = await users.UpdateAsync(id, update);
		Assert.Equal(75, updated["coupons"]![0]!["discount"]!["percentage"]!.GetValue<int>());
		
		update["region"] = "center";
		var adminClient = await this._instance.CreateAdminClientAsync();
		using var invalidResponse = await adminClient.PutAsJsonAsync($"{this.MembershipUrl}/users/{id}", update, CancellationToken);
		await AssertValidationErrorAsync(invalidResponse, "region");
	}
	
	#endregion
}