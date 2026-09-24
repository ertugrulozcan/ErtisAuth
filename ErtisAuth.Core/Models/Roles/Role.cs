using System.Text.Json.Serialization;
using Ertis.Core.Helpers;
using Ertis.Core.Models.Resources;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Roles;

public class Role : MembershipBoundedResource, IHasSysInfo
{
	#region Properties
	
	[JsonProperty("name")]
	[JsonPropertyName("name")]
	[BsonElement("name")]
	public required string Name { get; set; }
	
	[JsonProperty("slug")]
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
	
	[JsonProperty("description")]
	[JsonPropertyName("description")]
	[BsonElement("description")]
	public string? Description { get; set; }
	
	[JsonProperty("permissions")]
	[JsonPropertyName("permissions")]
	[BsonElement("permissions")]
	public IEnumerable<string>? Permissions { get; set; }
	
	[JsonProperty("forbidden")]
	[JsonPropertyName("forbidden")]
	[BsonElement("forbidden")]
	public IEnumerable<string>? Forbidden { get; set; }
	
	[JsonProperty("sys")]
	[JsonPropertyName("sys")]
	[BsonElement("sys")]
	public SysModel? Sys { get; set; }
	
	#endregion
}