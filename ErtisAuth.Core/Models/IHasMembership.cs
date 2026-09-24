using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models;

public interface IHasMembership
{
	[JsonProperty("membership_id")]
	[JsonPropertyName("membership_id")]
	[BsonElement("membership_id")]
	string MembershipId { get; set; }
}