using System.Text.Json.Serialization;
using Ertis.Core.Models.Resources;
using Ertis.Schema.Dynamics;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Integrations.OAuth.Core;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using JsonIgnore = System.Text.Json.Serialization.JsonIgnoreAttribute;
using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

namespace ErtisAuth.Core.Models.Users;

public class User : MembershipBoundedResource, IUtilizer, IHasSysInfo
{
	#region Properties
	
	[JsonProperty("firstname")]
	[JsonPropertyName("firstname")]
	[BsonElement("firstname")]
	public string? FirstName { get; set; }
	
	[JsonProperty("lastname")]
	[JsonPropertyName("lastname")]
	[BsonElement("lastname")]
	public string? LastName { get; set; }
	
	[JsonProperty("username")]
	[JsonPropertyName("username")]
	[BsonElement("username")]
	public required string Username { get; set; }
	
	[JsonProperty("email_address")]
	[JsonPropertyName("email_address")]
	[BsonElement("email_address")]
	public string? EmailAddress { get; set; }
	
	[JsonProperty("role")]
	[JsonPropertyName("role")]
	[BsonElement("role")]
	public required string Role { get; set; }
	
	[JsonProperty("permissions")]
	[JsonPropertyName("permissions")]
	[BsonElement("permissions")]
	public IEnumerable<string>? Permissions { get; set; }
	
	[JsonProperty("forbidden")]
	[JsonPropertyName("forbidden")]
	[BsonElement("forbidden")]
	public IEnumerable<string>? Forbidden { get; set; }
	
	[JsonProperty("user_type")]
	[JsonPropertyName("user_type")]
	[BsonElement("user_type")]
	public string? UserType { get; set; }
	
	[JsonProperty("source_provider")]
	[JsonPropertyName("source_provider")]
	[BsonElement("source_provider")]
	public string? SourceProvider { get; set; }
	
	[JsonProperty("connected_accounts")]
	[JsonPropertyName("connected_accounts")]
	[BsonElement("connected_accounts")]
	public ProviderAccountInfo[]? ConnectedAccounts { get; set; }
	
	[JsonProperty("is_active")]
	[JsonPropertyName("is_active")]
	[BsonElement("is_active")]
	public bool IsActive { get; set; }
	
	[JsonProperty("sys")]
	[JsonPropertyName("sys")]
	[BsonElement("sys")]
	public SysModel? Sys { get; set; }
	
	[JsonIgnore]
	[BsonIgnore]
	[NewtonsoftJsonIgnore]
	public Utilizer.UtilizerType UtilizerType => Utilizer.UtilizerType.User;
	
	#endregion
	
	#region Implicit & Explicit Operators
	
	public static implicit operator DynamicObject(User user) => new(user);
	
	public static explicit operator User?(DynamicObject? dynamicObject) => dynamicObject?.Deserialize<User>();
	
	#endregion
}