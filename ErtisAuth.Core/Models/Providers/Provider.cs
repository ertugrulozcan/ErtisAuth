using System.Text.Json.Serialization;
using Ertis.Core.Helpers;
using Ertis.Core.Models;
using ErtisAuth.Integrations.OAuth.Core;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace ErtisAuth.Core.Models.Providers;

public abstract class Provider : MembershipBoundedResource, IHasSlug, IHasSysInfo
{
	#region Properties
	
	[JsonPropertyName("type")]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	[BsonIgnore]
	public abstract ProviderType Type { get; }
	
	[JsonPropertyName("name")]
	[BsonElement("name")]
	public string Name
	{
		get => string.IsNullOrEmpty(field) ? this.Type.ToString() : field;
		set;
	}
	
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
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("description")]
	[BsonIgnoreIfNull]
	public string? Description { get; set; }
	
	[JsonPropertyName("defaultRole")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("defaultRole")]
	[BsonIgnoreIfNull]
	public string? DefaultRole { get; set; }
	
	[JsonPropertyName("defaultUserType")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("defaultUserType")]
	[BsonIgnoreIfNull]
	public string? DefaultUserType { get; set; }
	
	[JsonPropertyName("isActive")]
	[BsonElement("isActive")]
	public bool IsActive { get; set; }
	
	/// <summary>
	/// Link a provider login to an existing user by email address even when the provider does not assert that the
	/// email is verified (Facebook, Microsoft). Google and Apple verified emails are linked regardless.
	/// </summary>
	[JsonPropertyName("trust_email")]
	[BsonElement("trust_email")]
	public bool TrustEmail { get; set; }
	
	[JsonPropertyName("sys")]
	[BsonElement("sys")]
	public SysModel? Sys { get; set; }
	
	#endregion
}