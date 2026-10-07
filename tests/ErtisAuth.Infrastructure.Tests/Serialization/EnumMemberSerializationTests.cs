using System.Text.Json;
using ErtisAuth.Core.Models;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Webhooks;
using ErtisAuth.Infrastructure.Tests.Helpers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace ErtisAuth.Infrastructure.Tests.Serialization;

/// <summary>
/// Status values are stored and returned in lowercase ([EnumMember] value) as before .NET 10:
/// WebhookService queries active webhooks with status == "active".
/// </summary>
public class EnumMemberSerializationTests
{
	#region Helpers
	
	private static Webhook CreateWebhook(WebhookStatus? status)
	{
		return new Webhook
		{
			Name = "User created hook",
			Event = "UserCreated",
			MembershipId = "5f8a1b2c3d4e5f6a7b8c9d00",
			Status = status
		};
	}
	
	#endregion
	
	#region BSON
	
	[Theory]
	[InlineData(WebhookStatus.Active, "active")]
	[InlineData(WebhookStatus.Passive, "passive")]
	public void Webhook_ToBson_WritesLowercaseStatus(WebhookStatus status, string expected)
	{
		var document = CreateWebhook(status).ToBsonDocument();
		
		Assert.Equal(expected, document["status"].AsString);
	}
	
	[Fact]
	public void Webhook_ToBson_WithoutStatus_WritesNull()
	{
		var document = CreateWebhook(null).ToBsonDocument();
		
		Assert.True(document["status"].IsBsonNull);
		Assert.Null(BsonSerializer.Deserialize<Webhook>(document).Status);
	}
	
	[Theory]
	[InlineData("active")]
	[InlineData("Active")]
	[InlineData("ACTIVE")]
	public void Webhook_FromBson_ReadsStatusCaseInsensitively(string storedStatus)
	{
		var document = CreateWebhook(null).ToBsonDocument();
		document["status"] = storedStatus;
		
		Assert.Equal(WebhookStatus.Active, BsonSerializer.Deserialize<Webhook>(document).Status);
	}
	
	[Fact]
	public void Webhook_FromBson_WithUnknownStatus_Throws()
	{
		var document = CreateWebhook(null).ToBsonDocument();
		document["status"] = "enabled";
		
		Assert.ThrowsAny<FormatException>(() => BsonSerializer.Deserialize<Webhook>(document));
	}
	
	[Theory]
	[InlineData("active", Status.Active)]
	[InlineData("Passive", Status.Passive)]
	public void Membership_BsonRoundTrip_WritesLowercaseUserActivation(string storedUserActivation, Status expected)
	{
		var membership = TestServiceFactory.CreateMembership();
		membership.Id = "5f8a1b2c3d4e5f6a7b8c9d00";
		var document = membership.ToBsonDocument();
		document["user_activation"] = storedUserActivation;
		
		var storedMembership = BsonSerializer.Deserialize<Membership>(document);
		
		Assert.Equal(expected, storedMembership.UserActivation);
		Assert.Equal(storedUserActivation.ToLowerInvariant(), storedMembership.ToBsonDocument()["user_activation"].AsString);
	}
	
	#endregion
	
	#region JSON
	
	[Fact]
	public void Webhook_ToJson_WritesLowercaseStatus()
	{
		using var json = JsonDocument.Parse(JsonSerializer.Serialize(CreateWebhook(WebhookStatus.Active)));
		
		Assert.Equal("active", json.RootElement.GetProperty("status").GetString());
	}
	
	[Theory]
	[InlineData("\"active\"", WebhookStatus.Active)]
	[InlineData("\"Active\"", WebhookStatus.Active)]
	[InlineData("\"PASSIVE\"", WebhookStatus.Passive)]
	[InlineData("null", null)]
	public void Webhook_FromJson_ReadsStatusCaseInsensitively(string status, WebhookStatus? expected)
	{
		var json = $$"""{"name":"User created hook","event":"UserCreated","membership_id":"5f8a1b2c3d4e5f6a7b8c9d00","status":{{status}}}""";
		
		Assert.Equal(expected, JsonSerializer.Deserialize<Webhook>(json)!.Status);
	}
	
	[Fact]
	public void Webhook_FromJson_WithUnknownStatus_Throws()
	{
		const string json = """{"name":"User created hook","event":"UserCreated","membership_id":"5f8a1b2c3d4e5f6a7b8c9d00","status":"enabled"}""";
		
		Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Webhook>(json));
	}
	
	[Fact]
	public void Membership_ToJson_WritesLowercaseUserActivation()
	{
		var membership = TestServiceFactory.CreateMembership();
		membership.UserActivation = Status.Passive;
		
		using var json = JsonDocument.Parse(JsonSerializer.Serialize(membership));
		
		Assert.Equal("passive", json.RootElement.GetProperty("user_activation").GetString());
	}
	
	#endregion
}