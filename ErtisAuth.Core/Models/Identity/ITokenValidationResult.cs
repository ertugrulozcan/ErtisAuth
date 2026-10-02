using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable UnusedMemberInSuper.Global
namespace ErtisAuth.Core.Models.Identity;

public interface ITokenValidationResult
{
	[JsonPropertyName("verified")]
	[BsonElement("verified")]
	bool IsValidated { get; }
	
	[JsonPropertyName("token")]
	[BsonElement("token")]
	string Token { get; }
}