using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Core.Models;

public abstract class MembershipBoundedResource : ResourceBase, IHasMembership
{
	#region Properties
	
	[JsonPropertyName("membership_id")]
	[BsonElement("membership_id")]
	public required string MembershipId { get; set; }
	
	#endregion
}