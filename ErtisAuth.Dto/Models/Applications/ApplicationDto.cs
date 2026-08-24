using Ertis.MongoDB.Attributes;
using ErtisAuth.Dto.Models.Resources;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Dto.Models.Applications;

public class ApplicationDto : EntityBase, IHasMembership, IHasSysDto
{
	#region Properties
	
	[Searchable]
	[BsonElement("name")]
	public string? Name { get; set; }
	
	[BsonElement("slug")]
	public string? Slug { get; set; }
	
	[BsonElement("role")]
	public string? Role { get; set; }
	
	[BsonElement("permissions")]
	public IEnumerable<string>? Permissions { get; set; }
	
	[BsonElement("forbidden")]
	public IEnumerable<string>? Forbidden { get; set; }
	
	[BsonElement("membership_id")]
	public required string MembershipId { get; set; }
	
	[BsonElement("sys")]
	public SysModelDto? Sys { get; set; }
	
	#endregion
}