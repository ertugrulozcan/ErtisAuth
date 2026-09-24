using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using NewtonsoftJsonProperty = Newtonsoft.Json.JsonPropertyAttribute;
using NewtonsoftJsonConverter = Newtonsoft.Json.JsonConverterAttribute;
using NewtonsoftStringEnumConverter = Newtonsoft.Json.Converters.StringEnumConverter;

// ReSharper disable UnusedMember.Global
namespace ErtisAuth.Core.Models.Identity;

public class ScopedBearerToken : TokenBase
{
    #region Properties
	
	[JsonPropertyName("token_type")]
	[NewtonsoftJsonProperty("token_type")]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	[NewtonsoftJsonConverter(typeof(NewtonsoftStringEnumConverter))]
	[BsonElement("token_type")]
	[BsonRepresentation(BsonType.String)]
	public override SupportedTokenTypes TokenType => SupportedTokenTypes.Bearer;
	
	[JsonPropertyName("scopes")]
	[NewtonsoftJsonProperty("scopes")]
	[BsonElement("scopes")]
	public string[]? Scopes { get; set; }
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Private Constructor
	/// </summary>
	private ScopedBearerToken()
	{
		
	}
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="token"></param>
	/// <param name="expiresIn"></param>
	public ScopedBearerToken(string token, TimeSpan expiresIn)
	{
		this.AccessToken = token;
		this.ExpiresIn = expiresIn;
		this.CreatedAt = DateTime.Now;
	}
	
	public ScopedBearerToken(BearerToken bearerToken, string[] scopes)
	{
		this.AccessToken = bearerToken.AccessToken;
		this.ExpiresIn = TimeSpan.FromSeconds(bearerToken.ExpiresInTimeStamp);
		this.CreatedAt = bearerToken.CreatedAt;
		this.Scopes = scopes;
	}
	
	#endregion
	
	#region Methods
	
	public static ScopedBearerToken CreateTemp(string token)
	{
		return new ScopedBearerToken
		{
			AccessToken = token,
			CreatedAt = DateTime.Now
		};
	}
	
	public static ScopedBearerToken? ParseFromJson(string json)
	{
		var bearerToken = System.Text.Json.JsonSerializer.Deserialize<BearerToken>(json);
		if (bearerToken == null)
		{
			return null;
		}
		
		return new ScopedBearerToken
		{
			AccessToken = bearerToken.AccessToken,
			ExpiresIn = bearerToken.ExpiresIn,
			CreatedAt = bearerToken.CreatedAt
		};
	}
	
	#endregion
}