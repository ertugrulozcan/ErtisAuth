using System.Text.Json;
using System.Text.Json.Nodes;
using ErtisAuth.IntegrationTests.Infrastructure;

namespace ErtisAuth.IntegrationTests.Resources;

/// <summary>
/// The documents of the stored events are returned as plain json: the ids are strings and the dates are ISO 8601 strings
/// (regression: they were returned with the extended json wrappers { "$oid": .. } and { "$date": .. }).
/// </summary>
public class EventDocumentTests : IClassFixture<ErtisAuthInstance>
{
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="instance"></param>
	public EventDocumentTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Methods
	
	[Fact]
	public async Task UserCreatedEvent_ReturnsTheDocumentAsPlainJson()
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		var membershipUrl = $"/memberships/{this._instance.MembershipId}";
		var username = $"user{Guid.NewGuid():N}";
		var user = await new ResourceClient(adminClient, $"{membershipUrl}/users").CreateAsync(new
		{
			username,
			firstname = "Jane",
			lastname = "Doe",
			email_address = $"{username}@example.com",
			password = "Event-P@ssw0rd!",
			role = "admin",
			user_type = "user"
		});
		
		var events = await new ResourceClient(adminClient, $"{membershipUrl}/events").QueryAsync(new
		{
			where = new Dictionary<string, object>
			{
				["event_type"] = "UserCreated",
				["document.username"] = username
			}
		});
		
		var document = Assert.Single(events)!["document"]!.AsObject();
		Assert.Equal(JsonValueKind.String, document["_id"]!.GetValueKind());
		Assert.Equal(user["_id"]!.GetValue<string>(), document["_id"]!.GetValue<string>());
		
		var createdAt = document["sys"]!["created_at"]!;
		Assert.Equal(JsonValueKind.String, createdAt.GetValueKind());
		Assert.Equal(DateTimeKind.Utc, createdAt.GetValue<DateTime>().Kind);
		Assert.DoesNotContain("\"$", document.ToJsonString());
	}
	
	#endregion
}
