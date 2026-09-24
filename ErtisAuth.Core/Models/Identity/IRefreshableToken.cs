using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using JsonIgnore = System.Text.Json.Serialization.JsonIgnoreAttribute;
using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

// ReSharper disable UnusedMemberInSuper.Global
namespace ErtisAuth.Core.Models.Identity;

public interface IRefreshableToken
{
	[JsonProperty("refresh_token")]
	[JsonPropertyName("refresh_token")]
	[BsonElement("refresh_token")]
	string? RefreshToken { get; }
	
	[JsonIgnore]
	[BsonIgnore]
	[NewtonsoftJsonIgnore]
	TimeSpan RefreshExpiresIn { get; }
}