using System.Text.Json;
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
	public TimeSpan RefreshExpiresIn { get; }
	
	[JsonPropertyName("refresh_token_expires_in")]
	[NewtonsoftJsonProperty("refresh_token_expires_in")]
	[BsonElement("refresh_token_expires_in")]
	public int RefreshTokenExpiresInTimeStamp => (int) this.RefreshExpiresIn.TotalSeconds;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="token"></param>
	/// <param name="expiresIn"></param>
	/// <param name="refreshToken"></param>
	/// <param name="refreshExpiresIn"></param>
	/// <param name="createdAt"></param>
	public BearerToken(
		string token, 
		TimeSpan expiresIn, 
		string? refreshToken = null, 
		TimeSpan? refreshExpiresIn = null, 
		DateTime? createdAt = null) : base(token, createdAt)
	{
		this.ExpiresIn = expiresIn;
		this.RefreshToken = refreshToken;
		this.RefreshExpiresIn = refreshExpiresIn ?? TimeSpan.Zero;
	}
	
	#endregion
	
	#region Methods
	
	public static BearerToken CreateTemp(string token)
	{
		return new BearerToken(token, TimeSpan.Zero);
	}
	
	public static BearerToken? ParseFromJson(string json)
	{
		var serializableToken = JsonSerializer.Deserialize<SerializableToken>(json);
		if (serializableToken == null)
		{
			return null;
		}
		
		return new BearerToken(
			token: serializableToken.AccessToken,
			expiresIn: TimeSpan.FromSeconds(serializableToken.ExpiresInTimeStamp),
			refreshToken: serializableToken.RefreshToken,
			refreshExpiresIn: TimeSpan.FromSeconds(serializableToken.RefreshTokenExpiresInTimeStamp),
			createdAt: serializableToken.CreatedAt
		);
	}
	
	#endregion
}