using System.Text.Json.Serialization;
using ErtisAuth.Core.Models.Applications;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Core.Models.Identity;

public readonly struct BasicTokenValidationResult : ITokenValidationResult
{
	#region Properties
	
	[JsonPropertyName("verified")]
	[BsonElement("verified")]
	public bool IsValidated { get; }
	
	[JsonPropertyName("token")]
	[BsonElement("token")]
	public string Token { get; }
	
	[JsonIgnore]
	[BsonIgnore]
	public Application Application { get; }
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="isVerified"></param>
	/// <param name="token"></param>
	/// <param name="application"></param>
	public BasicTokenValidationResult(bool isVerified, string token, Application application)
	{
		this.IsValidated = isVerified;
		this.Token = token;
		this.Application = application;
	}
	
	#endregion
}