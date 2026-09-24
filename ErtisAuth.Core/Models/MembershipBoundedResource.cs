using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models;

public abstract class MembershipBoundedResource : ResourceBase, IHasMembership
{
	#region Properties
	
	[JsonProperty("membership_id")]
	[JsonPropertyName("membership_id")]
	[BsonElement("membership_id")]
	public required string MembershipId { get; set; }
	
	#endregion
}