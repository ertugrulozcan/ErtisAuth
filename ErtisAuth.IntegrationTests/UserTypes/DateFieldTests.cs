using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ErtisAuth.IntegrationTests.Infrastructure;
using ErtisAuth.IntegrationTests.Resources;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ErtisAuth.IntegrationTests.UserTypes;

/// <summary>
/// The custom date and datetime fields of user types are stored as UTC dates whatever the server's time zone is
/// (a value with 'Z', with an offset or without an offset, which is UTC), and date-like values of string fields stay strings.
/// Run the suite under other time zones too (e.g. TZ=Pacific/Kiritimati and TZ=Etc/GMT+12): the stored values must not shift.
/// </summary>
public class DateFieldTests : IClassFixture<ErtisAuthInstance>
{
	#region Constants
	
	private const string UserTypeSlug = "dated";
	
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
	public DateFieldTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private async Task EnsureUserTypeAsync()
	{
		var userTypes = new ResourceClient(await this._instance.CreateAdminClientAsync(), $"{this.MembershipUrl}/user-types");
		if ((await userTypes.QueryAsync(new { where = new { slug = UserTypeSlug } })).Count == 0)
		{
			await userTypes.CreateAsync(new JsonObject
			{
				["name"] = "Dated",
				["baseType"] = "user",
				["properties"] = new JsonObject
				{
					["birth_date"] = new JsonObject { ["type"] = "date" },
					["last_seen"] = new JsonObject { ["type"] = "datetime" },
					["note"] = new JsonObject { ["type"] = "string" }
				}
			});
		}
	}
	
	private async Task<HttpResponseMessage> CreateUserAsync(string field, string value)
	{
		await this.EnsureUserTypeAsync();
		
		var username = $"dated{Guid.NewGuid():N}";
		var body = new JsonObject
		{
			["username"] = username,
			["firstname"] = "Dated",
			["email_address"] = $"{username}@example.com",
			["password"] = "Dated-P@ssw0rd!",
			["role"] = "admin",
			["user_type"] = UserTypeSlug,
			[field] = value
		};
		
		var adminClient = await this._instance.CreateAdminClientAsync();
		return await adminClient.PostAsJsonAsync($"{this.MembershipUrl}/users", body, CancellationToken);
	}
	
	private async Task<(BsonValue Stored, JsonNode Returned)> CreateAndReadAsync(string field, string value)
	{
		using var response = await this.CreateUserAsync(field, value);
		var created = (await ResourceClient.AssertStatusAsync(response, HttpStatusCode.Created))!.AsObject();
		var id = created["_id"]!.GetValue<string>();
		
		var stored = await this._instance.Database.GetCollection<BsonDocument>("users")
			.Find(Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(id)))
			.SingleAsync(CancellationToken);
		
		var users = new ResourceClient(await this._instance.CreateAdminClientAsync(), $"{this.MembershipUrl}/users");
		var returned = await users.GetAsync(id);
		return (stored[field], returned[field]!);
	}
	
	#endregion
	
	#region Tests
	
	[Theory]
	[InlineData("2026-01-31T10:00:00Z")]
	[InlineData("2026-01-31T13:00:00+03:00")]
	[InlineData("2026-01-31T05:00:00-05:00")]
	[InlineData("2026-01-31T10:00:00")]
	[InlineData("2026-01-31T10:00:00.000Z")]
	public async Task DateTimeField_IsStoredAsTheUtcInstant(string value)
	{
		var expected = new DateTime(2026, 1, 31, 10, 0, 0, DateTimeKind.Utc);
		
		var (stored, returned) = await this.CreateAndReadAsync("last_seen", value);
		
		Assert.Equal(BsonType.DateTime, stored.BsonType);
		Assert.Equal(expected, stored.ToUniversalTime());
		Assert.Equal(new DateTimeOffset(expected), DateTimeOffset.Parse(returned.GetValue<string>(), System.Globalization.CultureInfo.InvariantCulture));
	}
	
	[Fact]
	public async Task DateField_IsStoredAsTheUtcMidnight()
	{
		var (stored, _) = await this.CreateAndReadAsync("birth_date", "2026-01-31");
		
		Assert.Equal(BsonType.DateTime, stored.BsonType);
		Assert.Equal(new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc), stored.ToUniversalTime());
	}
	
	[Theory]
	[InlineData("2026-01-31T10:00:00Z")]
	[InlineData("2026-01-31")]
	public async Task StringField_WithDateLikeValue_StaysAString(string value)
	{
		var (stored, returned) = await this.CreateAndReadAsync("note", value);
		
		Assert.Equal(BsonType.String, stored.BsonType);
		Assert.Equal(value, stored.AsString);
		Assert.Equal(value, returned.GetValue<string>());
	}
	
	[Theory]
	[InlineData("last_seen", "31/01/2026 10:00")]
	[InlineData("birth_date", "2026-31-01")]
	public async Task DateField_WithInvalidValue_IsRejected(string field, string value)
	{
		using var response = await this.CreateUserAsync(field, value);
		
		await ResourceClient.AssertStatusAsync(response, HttpStatusCode.BadRequest);
	}
	
	#endregion
}
