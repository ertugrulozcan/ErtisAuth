using System.Text.Json.Serialization;
using Ertis.Core.Helpers;
using Ertis.Core.Models;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Core.Models.Webhooks;

public class Webhook : MembershipBoundedResource, IHasSlug, IHasSysInfo
{
	#region Properties
	
	[JsonPropertyName("name")]
	[BsonElement("name")]
	public required string Name { get; set; }
	
	[JsonPropertyName("slug")]
	[BsonElement("slug")]
	public string Slug
	{
		get
		{
			if (string.IsNullOrEmpty(field))
			{
				field = Slugifier.Slugify(this.Name, Slugifier.Options.Ignore('_'));
			}
			
			return field;
		}
		set => field = Slugifier.Slugify(value, Slugifier.Options.Ignore('_'));
	}
	
	[JsonPropertyName("description")]
	[BsonElement("description")]
	public string? Description { get; set; }
	
	[JsonPropertyName("event")]
	[BsonElement("event")]
	public required string Event { get; set; }
	
	[JsonIgnore]
	[BsonIgnore]
	public ErtisAuthEventType? EventType
	{
		get
		{
			if (Enum.GetNames(typeof(ErtisAuthEventType)).Any(x => x == this.Event))
			{
				return (ErtisAuthEventType) Enum.Parse(typeof(ErtisAuthEventType), this.Event);
			}
			else
			{
				return null;
			}
		}
	}
	
	[JsonPropertyName("status")]
	[BsonElement("status")]
	[JsonConverter(typeof(EnumMemberJsonConverter<WebhookStatus>))]
	[BsonSerializer(typeof(NullableEnumMemberBsonSerializer<WebhookStatus>))]
	public WebhookStatus? Status { get; set; }
	
	[JsonIgnore]
	[BsonIgnore]
	public bool IsActive => this.Status == WebhookStatus.Active;
	
	[JsonPropertyName("request")]
	[BsonElement("request")]
	public WebhookRequest? Request { get; set; }
	
	[JsonPropertyName("try_count")]
	[BsonElement("try_count")]
	public int TryCount { get; set; }
	
	[JsonPropertyName("sys")]
	[BsonElement("sys")]
	public SysModel? Sys { get; set; }
	
	#endregion
}