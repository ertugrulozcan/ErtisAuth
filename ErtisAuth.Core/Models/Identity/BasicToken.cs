using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using JsonConverter = System.Text.Json.Serialization.JsonConverterAttribute;
using NewtonsoftJsonConverter = Newtonsoft.Json.JsonConverterAttribute;
using NewtonsoftStringEnumConverter = Newtonsoft.Json.Converters.StringEnumConverter;

namespace ErtisAuth.Core.Models.Identity;

public class BasicToken : TokenBase
{
	#region Properties
	
	[JsonProperty("token_type")]
	[JsonPropertyName("token_type")]
	[BsonElement("token_type")]
	[NewtonsoftJsonConverter(typeof(NewtonsoftStringEnumConverter))]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public override SupportedTokenTypes TokenType => SupportedTokenTypes.Basic;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="token"></param>
	public BasicToken(string token)
	{
		this.AccessToken = token;
		this.ExpiresIn = TimeSpan.MaxValue;
		this.CreatedAt = DateTime.Now;
	}
	
	#endregion
}