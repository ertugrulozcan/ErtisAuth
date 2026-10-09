using System.Text.Json.Serialization;
using ErtisAuth.Core.Exceptions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace ErtisAuth.Core.Models.Identity;

public abstract class TokenBase
{
	#region Properties
	
	[JsonPropertyName("token_type")]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	[BsonElement("token_type")]
	[BsonRepresentation(BsonType.String)]
	public abstract SupportedTokenTypes TokenType { get; }
	
	[JsonPropertyName("access_token")]
	[BsonElement("access_token")]
	public string AccessToken { get; set; }
	
	[JsonIgnore]
	[BsonIgnore]
	internal TimeSpan ExpiresIn { get; set; }
	
	[JsonPropertyName("expires_in")]
	[BsonElement("expires_in")]
	public int ExpiresInTimeStamp => (int) this.ExpiresIn.TotalSeconds;
	
	[JsonPropertyName("created_at")]
	[BsonElement("created_at")]
	public DateTime CreatedAt { get; private set; }
	
	[JsonIgnore]
	[BsonIgnore]
	public bool IsExpired => DateTime.UtcNow > this.CreatedAt.Add(this.ExpiresIn);
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="accessToken"></param>
	/// <param name="createdAt"></param>
	protected TokenBase(string accessToken, DateTime? createdAt = null)
	{
		this.AccessToken = accessToken;
		this.CreatedAt = createdAt ?? DateTime.UtcNow;
	}
	
	#endregion
	
	#region Methods
	
	public override string ToString()
	{
		return this.TokenType switch
		{
			SupportedTokenTypes.Basic => $"Basic {this.AccessToken}",
			SupportedTokenTypes.Bearer => $"Bearer {this.AccessToken}",
			_ => throw new ArgumentOutOfRangeException()
		};
	}
	
	public static string? ExtractToken(string? authorizationHeader, out string? tokenType)
	{
		if (string.IsNullOrEmpty(authorizationHeader))
		{
			tokenType = null;
			return null;
		}
		
		var parts = authorizationHeader.Split(' ');
		if (parts.Length > 2)
		{
			throw ErtisAuthException.InvalidToken();
		}
		
		if (parts.Length == 2)
		{
			// The scheme is case-insensitive (RFC 7235, 'bearer' is 'Bearer'); it is given with its canonical name
			if (!TokenTypeExtensions.TryParseTokenType(parts[0], out var supportedTokenType))
			{
				throw ErtisAuthException.UnsupportedTokenType();
			}
			
			tokenType = supportedTokenType.ToString();
			return parts[1];
		}
		
		tokenType = null;
		return parts[0];
	}
	
	#endregion
}