using System.Text.Json.Serialization;
using Ertis.Core.Helpers;
using Ertis.Core.Models.Resources;
using ErtisAuth.Integrations.OAuth.Core;
using MongoDB.Bson.Serialization.Attributes;
using NewtonsoftJsonProperty = Newtonsoft.Json.JsonPropertyAttribute;

namespace ErtisAuth.Core.Models.Providers;

public class Provider : MembershipBoundedResource, IHasSysInfo
{
	#region Properties
	
	[JsonPropertyName("name")]
	[NewtonsoftJsonProperty("name")]
	[BsonElement("name")]
	public string Name { get; private set; }
	
	[JsonPropertyName("slug")]
	[NewtonsoftJsonProperty("slug")]
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
	[NewtonsoftJsonProperty("description", NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
	[BsonElement("description")]
	[BsonIgnoreIfNull]
	public string? Description { get; set; }
	
	[JsonPropertyName("defaultRole")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[NewtonsoftJsonProperty("defaultRole", NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
	[BsonElement("defaultRole")]
	[BsonIgnoreIfNull]
	public string? DefaultRole { get; set; }
	
	[JsonPropertyName("defaultUserType")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[NewtonsoftJsonProperty("defaultUserType", NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
	[BsonElement("defaultUserType")]
	[BsonIgnoreIfNull]
	public string? DefaultUserType { get; set; }
	
	[JsonPropertyName("appClientId")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[NewtonsoftJsonProperty("appClientId", NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
	[BsonElement("appClientId")]
	[BsonIgnoreIfNull]
	public string? AppClientId { get; set; }
	
	[JsonPropertyName("teamId")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[NewtonsoftJsonProperty("teamId", NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
	[BsonElement("teamId")]
	[BsonIgnoreIfNull]
	public string? TeamId { get; set; }
	
	[JsonPropertyName("tenantId")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[NewtonsoftJsonProperty("tenantId", NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
	[BsonElement("tenantId")]
	[BsonIgnoreIfNull]
	public string? TenantId { get; set; }
	
	[JsonPropertyName("privateKey")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[NewtonsoftJsonProperty("privateKey", NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
	[BsonElement("privateKey")]
	[BsonIgnoreIfNull]
	public string? PrivateKey { get; set; }
	
	[JsonPropertyName("privateKeyId")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[NewtonsoftJsonProperty("privateKeyId", NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
	[BsonElement("privateKeyId")]
	[BsonIgnoreIfNull]
	public string? PrivateKeyId { get; set; }
	
	[JsonPropertyName("redirectUri")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[NewtonsoftJsonProperty("redirectUri", NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
	[BsonElement("redirectUri")]
	[BsonIgnoreIfNull]
	public string? RedirectUri { get; set; }
	
	[JsonPropertyName("isActive")]
	[NewtonsoftJsonProperty("isActive")]
	[BsonElement("isActive")]
	public bool IsActive { get; set; }
	
	[JsonPropertyName("sys")]
	[NewtonsoftJsonProperty("sys")]
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