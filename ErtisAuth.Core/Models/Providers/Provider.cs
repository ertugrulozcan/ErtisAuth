using System.Text.Json.Serialization;
using Ertis.Core.Helpers;
using Ertis.Core.Models;
using ErtisAuth.Integrations.OAuth.Core;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace ErtisAuth.Core.Models.Providers;

public class Provider : MembershipBoundedResource, IHasSysInfo
{
	#region Properties
	
	[JsonPropertyName("name")]
	[BsonElement("name")]
	public string Name { get; private set; }
	
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
	
	[JsonPropertyName("appClientId")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("appClientId")]
	[BsonIgnoreIfNull]
	public string? AppClientId { get; set; }
	
	[JsonPropertyName("teamId")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("teamId")]
	[BsonIgnoreIfNull]
	public string? TeamId { get; set; }
	
	[JsonPropertyName("tenantId")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("tenantId")]
	[BsonIgnoreIfNull]
	public string? TenantId { get; set; }
	
	[JsonPropertyName("privateKey")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("privateKey")]
	[BsonIgnoreIfNull]
	public string? PrivateKey { get; set; }
	
	[JsonPropertyName("privateKeyId")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("privateKeyId")]
	[BsonIgnoreIfNull]
	public string? PrivateKeyId { get; set; }
	
	[JsonPropertyName("redirectUri")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("redirectUri")]
	[BsonIgnoreIfNull]
	public string? RedirectUri { get; set; }
	
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
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="provider"></param>
	public Provider(KnownProviders provider)
	{
		this.Name = provider.ToString();
	}
	
	#endregion
}