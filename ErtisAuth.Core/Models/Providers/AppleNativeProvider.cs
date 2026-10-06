using System.Text.Json.Serialization;
using ErtisAuth.Integrations.OAuth.Core;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace ErtisAuth.Core.Models.Providers;

public class AppleNativeProvider : Provider
{
	#region Properties
	
	[JsonPropertyName("type")]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	[BsonIgnore]
	public override ProviderType Type => ProviderType.AppleNative;
	
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
	
	#endregion
}