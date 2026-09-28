using System.Text.Json.Serialization;
using ErtisAuth.Core.Models.Users;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using JsonIgnore = System.Text.Json.Serialization.JsonIgnoreAttribute;
using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

// ReSharper disable MemberCanBePrivate.Global
namespace ErtisAuth.Core.Models.Identity;

public readonly struct BearerTokenValidationResult : ITokenValidationResult
{
	#region Constants
	
	private const string AccessTokenKind = "access_token";
	
	private const string RefreshTokenKind = "refresh_token";
	
	#endregion
	
	#region Properties
	
	[JsonProperty("verified")]
	[JsonPropertyName("verified")]
	[BsonElement("verified")]
	public bool IsValidated { get; init; }
	
	[JsonIgnore]
	[BsonIgnore]
	[NewtonsoftJsonIgnore]
	public bool IsRefreshToken => this.TokenKind == RefreshTokenKind;
	
	[JsonProperty("token")]
	[JsonPropertyName("token")]
	[BsonElement("token")]
	public string Token { get; init; }
	
	[JsonProperty("token_kind")]
	[JsonPropertyName("token_kind")]
	[BsonElement("token_kind")]
	public string TokenKind { get; init; }
	
	[JsonIgnore]
	[BsonIgnore]
	[NewtonsoftJsonIgnore]
	public string[]? Scopes { get; init; }
	
	[JsonIgnore]
	[BsonIgnore]
	[NewtonsoftJsonIgnore]
	public User? User { get; }
	
	[JsonIgnore]
	[BsonIgnore]
	[NewtonsoftJsonIgnore]
	public TimeSpan RemainingTime => TimeSpan.FromSeconds(this.RemainingTimeUnixEpoch);
	
	[JsonProperty("remaining_time")]
	[JsonPropertyName("remaining_time")]
	[BsonElement("remaining_time")]
	public int RemainingTimeUnixEpoch { get; init; }
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="isVerified"></param>
	/// <param name="token"></param>
	/// <param name="user"></param>
	/// <param name="remainingTime"></param>
	/// <param name="isRefreshToken"></param>
	public BearerTokenValidationResult(bool isVerified, string token, User? user, TimeSpan remainingTime, bool isRefreshToken = false)
	{
		this.IsValidated = isVerified;
		this.Token = token;
		this.User = user;
		this.RemainingTimeUnixEpoch = (int) remainingTime.TotalSeconds;
		// Stored as a settable kind (not derived from IsRefreshToken) so that clients can deserialize it
		this.TokenKind = isRefreshToken ? RefreshTokenKind : AccessTokenKind;
	}
	
	#endregion
}