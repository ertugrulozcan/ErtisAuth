using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using NewtonsoftJsonProperty = Newtonsoft.Json.JsonPropertyAttribute;
using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;
using NewtonsoftJsonConverter = Newtonsoft.Json.JsonConverterAttribute;
using NewtonsoftStringEnumConverter = Newtonsoft.Json.Converters.StringEnumConverter;

// ReSharper disable PropertyCanBeMadeInitOnly.Local
namespace ErtisAuth.Core.Models.Identity;

public class BearerToken : TokenBase, IRefreshableToken
{
	#region Properties
	
	[JsonPropertyName("token_type")]
	[NewtonsoftJsonProperty("token_type")]
	[BsonElement("token_type")]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	[NewtonsoftJsonConverter(typeof(NewtonsoftStringEnumConverter))]
	public override SupportedTokenTypes TokenType => SupportedTokenTypes.Bearer;
	
	[JsonPropertyName("refresh_token")]
	[NewtonsoftJsonProperty("refresh_token")]
	[BsonElement("refresh_token")]
	public string? RefreshToken { get; private set; }
	
	[JsonIgnore]
	[BsonIgnore]
	[NewtonsoftJsonIgnore]
	public TimeSpan RefreshExpiresIn { get; private set; }
	
	[JsonPropertyName("refresh_token_expires_in")]
	[NewtonsoftJsonProperty("refresh_token_expires_in")]
	[BsonElement("refresh_token_expires_in")]
	public int RefreshTokenExpiresInTimeStamp => (int) this.RefreshExpiresIn.TotalSeconds;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Private Constructor
	/// </summary>
	private BearerToken()
	{
		
	}
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="token"></param>
	/// <param name="expiresIn"></param>
	/// <param name="refreshToken"></param>
	/// <param name="refreshExpiresIn"></param>
	public BearerToken(string token, TimeSpan expiresIn, string? refreshToken, TimeSpan refreshExpiresIn)
	{
		this.AccessToken = token;
		this.ExpiresIn = expiresIn;
		this.RefreshToken = refreshToken;
		this.RefreshExpiresIn = refreshExpiresIn;
		this.CreatedAt = DateTime.UtcNow;
	}
	
	#endregion
	
	#region Methods
	
	public static BearerToken CreateTemp(string token)
	{
		return new BearerToken
		{
			AccessToken = token,
			CreatedAt = DateTime.UtcNow
		};
	}
	
	public static BearerToken? ParseFromJson(string json)
	{
		return System.Text.Json.JsonSerializer.Deserialize<BearerToken>(json);
	}
	
	#endregion
}