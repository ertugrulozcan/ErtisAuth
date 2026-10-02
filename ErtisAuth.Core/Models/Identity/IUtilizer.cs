using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global
namespace ErtisAuth.Core.Models.Identity;

public interface IUtilizer
{
	#region Properties
	
	[JsonPropertyName("_id")]
	[BsonElement("_id")]
	string Id { get; set; }
	
	[JsonPropertyName("role")]
	[BsonElement("role")]
	string Role { get; set; }
	
	[JsonPropertyName("permissions")]
	[BsonElement("permissions")]
	IEnumerable<string>? Permissions { get; set; }
	
	[JsonPropertyName("forbidden")]
	[BsonElement("forbidden")]
	IEnumerable<string>? Forbidden { get; set; }
	
	[JsonIgnore]
	[BsonIgnore]
	Utilizer.UtilizerType UtilizerType { get; }
	
	[JsonPropertyName("membership_id")]
	[BsonElement("membership_id")]
	string MembershipId { get; set; }
	
	#endregion
}