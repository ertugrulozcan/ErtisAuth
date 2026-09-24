using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity;

public interface ITokenValidationResult
{
	[JsonProperty("verified")]
	[JsonPropertyName("verified")]
	[BsonElement("verified")]
	bool IsValidated { get; }
	
	[JsonProperty("token")]
	[JsonPropertyName("token")]
	[BsonElement("token")]
	string Token { get; }
}