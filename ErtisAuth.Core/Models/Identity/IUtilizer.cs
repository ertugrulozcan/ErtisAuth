using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using JsonIgnore = System.Text.Json.Serialization.JsonIgnoreAttribute;
using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

namespace ErtisAuth.Core.Models.Identity;

public interface IUtilizer
{
	#region Properties
	
	[JsonProperty("_id")]
	[JsonPropertyName("_id")]
	[BsonElement("_id")]
	string Id { get; set; }
	
	[JsonProperty("role")]
	[JsonPropertyName("role")]
	[BsonElement("role")]
	string Role { get; set; }
	
	[JsonProperty("permissions")]
	[JsonPropertyName("permissions")]
	[BsonElement("permissions")]
	IEnumerable<string>? Permissions { get; set; }
	
	[JsonProperty("forbidden")]
	[JsonPropertyName("forbidden")]
	[BsonElement("forbidden")]
	IEnumerable<string>? Forbidden { get; set; }
	
	[JsonIgnore]
	[BsonIgnore]
	[NewtonsoftJsonIgnore]
	Utilizer.UtilizerType UtilizerType { get; }
	
	[JsonProperty("membership_id")]
	[JsonPropertyName("membership_id")]
	[BsonElement("membership_id")]
	string MembershipId { get; set; }
	
	#endregion
}