using Ertis.Schema.Dynamics;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Webhooks;
using MongoDB.Bson;

namespace ErtisAuth.Infrastructure.Tests.Serialization;

/// <summary>
/// The payloads of the events are stored as BSON documents (any object: models, anonymous objects, DynamicObjects),
/// and the stored documents are read back as plain values.
/// </summary>
public class ErtisAuthEventSerializationTests
{
	#region Helpers
	
	private static ErtisAuthEvent CreateEvent(object? document = null)
	{
		return new ErtisAuthEvent
		{
			EventType = ErtisAuthEventType.UserCreated,
			UtilizerId = "utilizer",
			MembershipId = "membership",
			Document = document
		};
	}
	
	#endregion
	
	#region Methods
	
	[Fact]
	public void Document_WithDynamicObjectInsideAnAnonymousObject_StoresItsFields()
	{
		// Regression guard: without the converters a nested DynamicObject was stored as {}
		var ertisAuthEvent = CreateEvent(new { user = DynamicObject.Parse("""{ "username": "john.doe" }""") });
		
		Assert.Equal("john.doe", ertisAuthEvent.BsonDocument!["user"]["username"].AsString);
	}
	
	[Fact]
	public void Document_WithWebhookExecutionError_IsStored()
	{
		// Regression guard: an Exception in the payload could not be serialized (the event was not stored)
		var result = new WebhookExecutionResult
		{
			WebhookId = "webhook",
			StatusCode = 500,
			TryIndex = 1,
			Exception = WebhookExecutionError.FromException(new HttpRequestException("Connection refused"))
		};
		
		var ertisAuthEvent = CreateEvent(result);
		
		Assert.Equal(nameof(HttpRequestException), ertisAuthEvent.BsonDocument!["exception"]["type"].AsString);
		Assert.Equal("Connection refused", ertisAuthEvent.BsonDocument["exception"]["message"].AsString);
	}
	
	[Fact]
	public void BsonDocument_ReadFromTheDatabase_IsKeptAndMappedToPlainValues()
	{
		var id = ObjectId.GenerateNewId();
		var createdAt = new DateTime(2026, 10, 2, 10, 0, 0, DateTimeKind.Utc);
		var stored = new BsonDocument { { "_id", id }, { "created_at", createdAt }, { "tags", new BsonArray { "a" } } };
		var ertisAuthEvent = CreateEvent();
		
		ertisAuthEvent.BsonDocument = stored;
		
		// The stored document is not rebuilt from the mapped values (its dates would become strings)
		Assert.Same(stored, ertisAuthEvent.BsonDocument);
		
		var document = Assert.IsType<DynamicObject>(ertisAuthEvent.Document);
		Assert.Equal(id.ToString(), document.GetValue("_id"));
		Assert.Equal(createdAt, document.GetValue("created_at"));
		Assert.Equal(new object[] { "a" }, document.GetValue("tags"));
	}
	
	#endregion
}
