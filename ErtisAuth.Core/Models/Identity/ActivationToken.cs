using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using JsonIgnore = System.Text.Json.Serialization.JsonIgnoreAttribute;
using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace ErtisAuth.Core.Models.Identity;

public class ActivationToken
{
	#region Properties
	
	[JsonProperty("reset_token")]
	[JsonPropertyName("reset_token")]
	[BsonElement("reset_token")]
	public string Token { get; protected set; }
	
	[JsonIgnore]
	[BsonIgnore]
	[NewtonsoftJsonIgnore]
	public TimeSpan ExpiresIn { get; protected set; }
	
	[JsonProperty("expires_in")]
	[JsonPropertyName("expires_in")]
	[BsonElement("expires_in")]
	public int ExpiresInTimeStamp
	{
		get => (int)this.ExpiresIn.TotalSeconds;
		set => this.ExpiresIn = TimeSpan.FromSeconds(value);
	}
	
	[JsonProperty("created_at")]
	[JsonPropertyName("created_at")]
	[BsonElement("created_at")]
	public DateTime CreatedAt { get; protected set; }
	
	[JsonIgnore]
	[BsonIgnore]
	[NewtonsoftJsonIgnore]
	public bool IsExpired => DateTime.UtcNow > this.CreatedAt.Add(this.ExpiresIn);
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="token"></param>
	/// <param name="expiresIn"></param>
	public ActivationToken(string token, TimeSpan expiresIn)
	{
		this.Token = token;
		this.ExpiresIn = expiresIn;
		this.CreatedAt = DateTime.UtcNow;
	}
	
	#endregion
}