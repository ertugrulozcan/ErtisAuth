using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using JsonIgnore = System.Text.Json.Serialization.JsonIgnoreAttribute;
using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

namespace ErtisAuth.Core.Models.Identity;

public class ResetPasswordToken
{
	#region Fields
	
	private TimeSpan expiresIn;
	private int expiresInTimeStamp;
	
	#endregion
	
	#region Properties
	
	[JsonProperty("reset_token")]
	[JsonPropertyName("reset_token")]
	[BsonElement("reset_token")]
	public string Token { get; protected set; }
	
	[JsonIgnore]
	[BsonIgnore]
	[NewtonsoftJsonIgnore]
	public TimeSpan ExpiresIn
	{
		get => this.expiresIn;
		private init
		{
			this.expiresIn = value;
			this.expiresInTimeStamp = (int) value.TotalSeconds;
		}
	}
	
	[JsonProperty("expires_in")]
	[JsonPropertyName("expires_in")]
	[BsonElement("expires_in")]
	public int ExpiresInTimeStamp
	{
		get => this.expiresInTimeStamp;
		set
		{
			this.expiresInTimeStamp = value;
			this.expiresIn = TimeSpan.FromSeconds(value);
		}
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
	/// <param name="createdAt"></param>
	public ResetPasswordToken(string token, TimeSpan expiresIn, DateTime? createdAt = null)
	{
		this.Token = token;
		this.ExpiresIn = expiresIn;
		this.CreatedAt = createdAt ?? DateTime.UtcNow;
	}
	
	#endregion
	
	#region Enums
	
	public enum ResetPasswordTokenPurpose
	{
		ResetPassword, 
		OneTimePassword
	}
	
	#endregion
}