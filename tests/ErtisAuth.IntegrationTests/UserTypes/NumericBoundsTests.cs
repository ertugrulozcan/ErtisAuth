using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ErtisAuth.IntegrationTests.Infrastructure;
using ErtisAuth.IntegrationTests.Resources;

namespace ErtisAuth.IntegrationTests.UserTypes;

/// <summary>
/// The numeric bounds of integer and float fields (minimum, maximum, exclusiveMinimum, exclusiveMaximum) are enforced
/// on user data, at the top level and inside objects and arrays.
/// </summary>
public class NumericBoundsTests : IClassFixture<ErtisAuthInstance>
{
	#region Constants
	
	private const string UserTypeSlug = "bounded";
	
	#endregion
	
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
	public NumericBoundsTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	/// <summary>
	/// integer_range: [0, 100], integer_exclusive: (0, 10), float_range: [0, 1], float_exclusive: (0, 1),
	/// and integer_range again inside an object and inside the objects of an array.
	/// </summary>
	private static JsonObject BoundedProperties()
	{
		JsonObject IntegerRange() => new() { ["type"] = "integer", ["minimum"] = 0, ["maximum"] = 100 };
		
		return new JsonObject
		{
			["integer_range"] = IntegerRange(),
			["integer_exclusive"] = new JsonObject { ["type"] = "integer", ["exclusiveMinimum"] = 0, ["exclusiveMaximum"] = 10 },
			["float_range"] = new JsonObject { ["type"] = "float", ["minimum"] = 0, ["maximum"] = 1 },
			["float_exclusive"] = new JsonObject { ["type"] = "float", ["exclusiveMinimum"] = 0, ["exclusiveMaximum"] = 1 },
			["nested"] = new JsonObject
			{
				["type"] = "object",
				["properties"] = new JsonObject { ["integer_range"] = IntegerRange() }
			},
			["items"] = new JsonObject
			{
				["type"] = "array",
				["itemSchema"] = new JsonObject
				{
					["type"] = "object",
					["properties"] = new JsonObject { ["integer_range"] = IntegerRange() }
				}
			}
		};
	}
	
	private async Task EnsureUserTypeAsync()
	{
		var userTypes = new ResourceClient(await this._instance.CreateAdminClientAsync(), $"{this.MembershipUrl}/user-types");
		if ((await userTypes.QueryAsync(new { where = new { slug = UserTypeSlug } })).Count == 0)
		{
			await userTypes.CreateAsync(new JsonObject { ["name"] = "Bounded", ["baseType"] = "user", ["properties"] = BoundedProperties() });
		}
	}
	
	/// <summary>
	/// A user with the given value at the given place: a top-level field, 'nested' (object) or 'items' (array of objects).
	/// </summary>
	private async Task<HttpResponseMessage> CreateUserAsync(string field, double value)
	{
		await this.EnsureUserTypeAsync();
		
		JsonNode number = field.StartsWith("integer") ? JsonValue.Create((long) value) : JsonValue.Create(value);
		var username = $"bounded{Guid.NewGuid():N}";
		var body = new JsonObject
		{
			["username"] = username,
			["firstname"] = "Bounded",
			["email_address"] = $"{username}@example.com",
			["password"] = "Bounded-P@ssw0rd!",
			["role"] = "admin",
			["user_type"] = UserTypeSlug
		};
		
		switch (field)
		{
			case "nested.integer_range":
				body["nested"] = new JsonObject { ["integer_range"] = number };
				break;
			case "items.integer_range":
				body["items"] = new JsonArray(new JsonObject { ["integer_range"] = number });
				break;
			default:
				body[field] = number;
				break;
		}
		
		var adminClient = await this._instance.CreateAdminClientAsync();
		return await adminClient.PostAsJsonAsync($"{this.MembershipUrl}/users", body, CancellationToken);
	}
	
	#endregion
	
	#region Tests
	
	[Theory]
	[InlineData("integer_range", 0)]
	[InlineData("integer_range", 100)]
	[InlineData("integer_exclusive", 1)]
	[InlineData("integer_exclusive", 9)]
	[InlineData("float_range", 0)]
	[InlineData("float_range", 1)]
	[InlineData("float_range", 0.5)]
	[InlineData("float_exclusive", 0.001)]
	[InlineData("float_exclusive", 0.999)]
	[InlineData("nested.integer_range", 100)]
	[InlineData("items.integer_range", 0)]
	public async Task ValueWithinTheBounds_IsAccepted(string field, double value)
	{
		using var response = await this.CreateUserAsync(field, value);
		
		await ResourceClient.AssertStatusAsync(response, HttpStatusCode.Created);
	}
	
	[Theory]
	[InlineData("integer_range", -1)]
	[InlineData("integer_range", 101)]
	[InlineData("integer_exclusive", 0)]
	[InlineData("integer_exclusive", 10)]
	[InlineData("float_range", -0.001)]
	[InlineData("float_range", 1.001)]
	[InlineData("float_exclusive", 0)]
	[InlineData("float_exclusive", 1)]
	[InlineData("nested.integer_range", 101)]
	[InlineData("nested.integer_range", -1)]
	[InlineData("items.integer_range", 101)]
	[InlineData("items.integer_range", -1)]
	public async Task ValueOutOfTheBounds_IsRejected(string field, double value)
	{
		using var response = await this.CreateUserAsync(field, value);
		
		var error = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.BadRequest);
		Assert.Contains(field.Split('.').Last(), error!.ToJsonString());
	}
	
	#endregion
}