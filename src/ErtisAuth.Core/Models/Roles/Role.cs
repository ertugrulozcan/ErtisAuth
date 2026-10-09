using System.Text.Json.Serialization;
using Ertis.Core.Helpers;
using Ertis.Core.Models;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Core.Models.Roles;

public class Role : MembershipBoundedResource, IHasSlug, IHasSysInfo
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
	
	[JsonPropertyName("permissions")]
	[BsonElement("permissions")]
	public IEnumerable<string>? Permissions { get; set; }
	
	[JsonPropertyName("forbidden")]
	[BsonElement("forbidden")]
	public IEnumerable<string>? Forbidden { get; set; }
	
	[JsonPropertyName("sys")]
	[BsonElement("sys")]
	public SysModel? Sys { get; set; }
	
	#endregion
}