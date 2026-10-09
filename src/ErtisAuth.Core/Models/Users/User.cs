using System.Text.Json.Serialization;
using Ertis.Core.Models;
using Ertis.Schema.Dynamics;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Integrations.OAuth.Core;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace ErtisAuth.Core.Models.Users;

public class User : MembershipBoundedResource, IUtilizer, IHasSysInfo
{
	#region Properties
	
	[JsonPropertyName("firstname")]
	[BsonElement("firstname")]
	public string? FirstName { get; set; }
	
	[JsonPropertyName("lastname")]
	[BsonElement("lastname")]
	public string? LastName { get; set; }
	
	[JsonPropertyName("username")]
	[BsonElement("username")]
	public required string Username { get; set; }
	
	[JsonPropertyName("email_address")]
	[BsonElement("email_address")]
	public string? EmailAddress { get; set; }
	
	[JsonPropertyName("role")]
	[BsonElement("role")]
	public required string Role { get; set; }
	
	[JsonPropertyName("permissions")]
	[BsonElement("permissions")]
	public IEnumerable<string>? Permissions { get; set; }
	
	[JsonPropertyName("forbidden")]
	[BsonElement("forbidden")]
	public IEnumerable<string>? Forbidden { get; set; }
	
	[JsonPropertyName("user_type")]
	[BsonElement("user_type")]
	public string? UserType { get; set; }
	
	[JsonPropertyName("source_provider")]
	[BsonElement("source_provider")]
	public string? SourceProvider { get; set; }
	
	[JsonPropertyName("connected_accounts")]
	[BsonElement("connected_accounts")]
	public ProviderAccountInfo[]? ConnectedAccounts { get; set; }
	
	[JsonPropertyName("is_active")]
	[BsonElement("is_active")]
	public bool IsActive { get; set; }
	
	[JsonPropertyName("sys")]
	[BsonElement("sys")]
	public SysModel? Sys { get; set; }
	
	[JsonIgnore]
	[BsonIgnore]
	public Utilizer.UtilizerType UtilizerType => Utilizer.UtilizerType.User;
	
	#endregion
	
	#region Implicit & Explicit Operators
	
	public static implicit operator DynamicObject(User user) => new(user);
	
	public static explicit operator User?(DynamicObject? dynamicObject) => dynamicObject?.Deserialize<User>();
	
	#endregion
}