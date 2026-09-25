using System.Text.Json.Serialization;
using ErtisAuth.Core.Helpers;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Users;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using NewtonsoftJsonConverter = Newtonsoft.Json.JsonConverterAttribute;
using NewtonsoftStringEnumConverter = Newtonsoft.Json.Converters.StringEnumConverter;
using SystemJsonIgnore = System.Text.Json.Serialization.JsonIgnoreAttribute;
using SystemJsonConverter = System.Text.Json.Serialization.JsonConverterAttribute;

namespace ErtisAuth.Core.Models.Identity;

public struct Utilizer
{
	#region Properties
	
	[JsonProperty("id")]
	[JsonPropertyName("id")]
	[BsonElement("id")]
	public required string Id { get; set; }
	
	[JsonProperty("type")]
	[JsonPropertyName("type")]
	[BsonElement("type")]
	[NewtonsoftJsonConverter(typeof(NewtonsoftStringEnumConverter))]
	[SystemJsonConverter(typeof(JsonStringEnumConverter))]
	[BsonRepresentation(BsonType.String)]
	public UtilizerType Type { get; set; }
	
	[JsonProperty("username")]
	[JsonPropertyName("username")]
	[BsonElement("username")]
	public required string Username { get; set; }
	
	[JsonProperty("membership_id")]
	[JsonPropertyName("membership_id")]
	[BsonElement("membership_id")]
	public required string MembershipId { get; set; }
	
	[JsonProperty("role")]
	[JsonPropertyName("role")]
	[BsonElement("role")]
	public string? Role { get; set; }
	
	[JsonProperty("permissions")]
	[JsonPropertyName("permissions")]
	[BsonElement("permissions")]
	public IEnumerable<string>? Permissions { get; set; }
	
	[JsonProperty("forbidden")]
	[JsonPropertyName("forbidden")]
	[BsonElement("forbidden")]
	public IEnumerable<string>? Forbidden { get; set; }
	
	[JsonProperty("token")]
	[JsonPropertyName("token")]
	[BsonElement("token")]
	public string? Token { get; set; }
	
	[JsonProperty("scopes", NullValueHandling = NullValueHandling.Ignore)]
	[JsonPropertyName("scopes")]
	[BsonElement("scopes")]
	[SystemJsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string[]? Scopes { get; set; }
	
	[JsonProperty("tokenType")]
	[JsonPropertyName("tokenType")]
	[BsonElement("tokenType")]
	[NewtonsoftJsonConverter(typeof(NewtonsoftStringEnumConverter))]
	[SystemJsonConverter(typeof(JsonStringEnumConverter))]
	[BsonRepresentation(BsonType.String)]
	public SupportedTokenTypes TokenType { get; set; }
	
	#endregion
	
	#region Implicit Operators
	
	public static implicit operator Utilizer(User user) => new()
	{
		Id = user.Id,
		Type = UtilizerType.User,
		Username = user.Username,
		MembershipId = user.MembershipId,
		Role = user.Role,
		Permissions = user.Permissions,
		Forbidden = user.Forbidden
	};
	
	public static implicit operator Utilizer(Application application) => new()
	{
		Id = application.Id,
		Type = UtilizerType.Application,
		Username = application.Slug,
		MembershipId = application.MembershipId,
		Role = application.Role,
		Permissions = application.Permissions,
		Forbidden = application.Forbidden
	};
	
	#endregion
	
	#region Methods
	
	public static UtilizerType ParseType(string type)
	{
		if (string.IsNullOrEmpty(type) || string.IsNullOrWhiteSpace(type))
		{
			return UtilizerType.None;
		}
		
		type = type.ToLower();
		type = char.ToUpper(type[0]) + type.Substring(1);
		
		if (Enum.GetNames(typeof(UtilizerType)).Any(x => x == type))
		{
			return (UtilizerType) Enum.Parse(typeof(UtilizerType), type);
		}
		
		return UtilizerType.None;
	}
	
	public static Utilizer GetSystemUtilizer(string membershipId)
	{
		return new Utilizer
		{
			Id = "system",
			Username = "system",
			Role = ReservedRoles.Administrator,
			Type = UtilizerType.System,
			MembershipId = membershipId
		};
	}
	
	#endregion
	
	#region Enums
	
	public enum UtilizerType
	{
		None,
		System,
		User,
		Application
	}
	
	#endregion
}