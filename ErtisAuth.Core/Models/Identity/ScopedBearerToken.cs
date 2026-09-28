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
	/// <param name="token"></param>
	/// <param name="createdAt"></param>
	private ScopedBearerToken(string token, DateTime? createdAt = null) : base(token, createdAt)
	{
		
	}
	
	/// <summary>
	/// Constructor 1
	/// </summary>
	/// <param name="token"></param>
	/// <param name="expiresIn"></param>
	public ScopedBearerToken(string token, TimeSpan expiresIn) : base(token)
	{
		this.ExpiresIn = expiresIn;
	}
	
	/// <summary>
	/// Constructor 2
	/// </summary>
	/// <param name="bearerToken"></param>
	/// <param name="scopes"></param>
	public ScopedBearerToken(BearerToken bearerToken, string[] scopes) : base(bearerToken.AccessToken, bearerToken.CreatedAt)
	{
		this.ExpiresIn = TimeSpan.FromSeconds(bearerToken.ExpiresInTimeStamp);
		this.Scopes = scopes;
	}
	
	#endregion
	
	#region Methods
	
	public static ScopedBearerToken CreateTemp(string token)
	{
		return new ScopedBearerToken(token);
	}
	
	public static ScopedBearerToken? ParseFromJson(string json)
	{
		var bearerToken = BearerToken.ParseFromJson(json);
		if (bearerToken == null)
		{
			return null;
		}
		
		return new ScopedBearerToken(bearerToken.AccessToken, bearerToken.CreatedAt)
		{
			ExpiresIn = bearerToken.ExpiresIn
		};
	}
	
	#endregion
}