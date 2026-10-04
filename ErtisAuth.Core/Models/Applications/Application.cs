using System.Text.Json.Serialization;
using Ertis.Core.Helpers;
using Ertis.Core.Models;
using ErtisAuth.Core.Models.Identity;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace ErtisAuth.Core.Models.Applications;

public class Application : MembershipBoundedResource, IHasSlug, IUtilizer, IHasSysInfo
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
	
	[JsonPropertyName("role")]
	[BsonElement("role")]
	public required string Role { get; set; }
	
	[JsonPropertyName("permissions")]
	[BsonElement("permissions")]
	public IEnumerable<string>? Permissions { get; set; }
	
	[JsonPropertyName("forbidden")]
	[BsonElement("forbidden")]
	public IEnumerable<string>? Forbidden { get; set; }
	
	/// <summary>
	/// SHA-256 hash of the application secret used in Basic tokens. Server-managed and never serialized to clients.
	/// </summary>
	[JsonIgnore]
	[BsonElement("secret_hash")]
	[BsonIgnoreIfNull]
	public string? SecretHash { get; set; }
	
	[JsonPropertyName("sys")]
	[BsonElement("sys")]
	public SysModel? Sys { get; set; }
	
	[JsonIgnore]
	[BsonIgnore]
	public Utilizer.UtilizerType UtilizerType => Utilizer.UtilizerType.Application;
	
	#endregion
}