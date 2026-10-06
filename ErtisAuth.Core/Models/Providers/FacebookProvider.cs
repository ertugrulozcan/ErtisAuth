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

public class FacebookProvider : Provider
{
	#region Properties
	
	[JsonPropertyName("type")]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	[BsonIgnore]
	public override ProviderType Type => ProviderType.Facebook;
	
	[JsonPropertyName("appClientId")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("appClientId")]
	[BsonIgnoreIfNull]
	public string? AppClientId { get; set; }
	
	#endregion
}