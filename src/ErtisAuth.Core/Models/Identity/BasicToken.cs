using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Core.Models.Identity;

public class BasicToken : TokenBase
{
	#region Properties
	
	[JsonPropertyName("token_type")]
	[BsonElement("token_type")]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public override SupportedTokenTypes TokenType => SupportedTokenTypes.Basic;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="token"></param>
	public BasicToken(string token) : base(token)
	{
		this.ExpiresIn = TimeSpan.MaxValue;
	}
	
	#endregion
}