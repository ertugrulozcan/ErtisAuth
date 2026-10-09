using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable UnusedMemberInSuper.Global
namespace ErtisAuth.Core.Models.Identity;

public interface IRefreshableToken
{
	[JsonPropertyName("refresh_token")]
	[BsonElement("refresh_token")]
	string? RefreshToken { get; }
	
	[JsonIgnore]
	[BsonIgnore]
	TimeSpan RefreshExpiresIn { get; }
}