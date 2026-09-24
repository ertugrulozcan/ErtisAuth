using System.Text.Json.Serialization;
using ErtisAuth.Core.Exceptions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using NewtonsoftJsonProperty = Newtonsoft.Json.JsonPropertyAttribute;
using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;
using NewtonsoftJsonConverter = Newtonsoft.Json.JsonConverterAttribute;
using NewtonsoftStringEnumConverter = Newtonsoft.Json.Converters.StringEnumConverter;

// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace ErtisAuth.Core.Models.Identity;

public abstract class TokenBase
{
	#region Properties
	
	[JsonPropertyName("access_token")]
	[NewtonsoftJsonProperty("access_token")]
	[BsonElement("access_token")]
	public string AccessToken { get; set; } = null!;
	
	[JsonPropertyName("token_type")]
	[NewtonsoftJsonProperty("token_type")]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	[NewtonsoftJsonConverter(typeof(NewtonsoftStringEnumConverter))]
	[BsonElement("token_type")]
	[BsonRepresentation(BsonType.String)]
	public abstract SupportedTokenTypes TokenType { get; }
	
	[JsonIgnore]
	[BsonIgnore]
	[NewtonsoftJsonIgnore]
	internal TimeSpan ExpiresIn { get; set; }
	
	[JsonPropertyName("expires_in")]
	[NewtonsoftJsonProperty("expires_in")]
	[BsonElement("expires_in")]
	public int ExpiresInTimeStamp => (int) this.ExpiresIn.TotalSeconds;
	
	[JsonPropertyName("created_at")]
	[NewtonsoftJsonProperty("created_at")]
	[BsonElement("created_at")]
	public DateTime CreatedAt { get; protected set; }
	
	[JsonIgnore]
	[BsonIgnore]
	[NewtonsoftJsonIgnore]
	public bool IsExpired => DateTime.Now > this.CreatedAt.Add(this.ExpiresIn);
	
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
			var supportedTokenTypes = Enum.GetValues(typeof(SupportedTokenTypes)).Cast<SupportedTokenTypes>().Select(x => x.ToString());
			if (!supportedTokenTypes.Contains(parts[0]))
			{
				throw ErtisAuthException.UnsupportedTokenType();
			}
			
			tokenType = parts[0];
			return parts[1];
		}
		
		tokenType = null;
		return parts[0];
	}
	
	#endregion
}