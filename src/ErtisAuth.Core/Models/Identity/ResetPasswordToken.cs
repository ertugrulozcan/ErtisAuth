using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace ErtisAuth.Core.Models.Identity;

public class ResetPasswordToken
{
	#region Fields
	
	private TimeSpan expiresIn;
	private int expiresInTimeStamp;
	
	#endregion
	
	#region Properties
	
	[JsonPropertyName("reset_token")]
	[BsonElement("reset_token")]
	public string Token { get; protected set; }
	
	[JsonIgnore]
	[BsonIgnore]
	public TimeSpan ExpiresIn
	{
		get => this.expiresIn;
		private init
		{
			this.expiresIn = value;
			this.expiresInTimeStamp = (int) value.TotalSeconds;
		}
	}
	
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
	
	[JsonPropertyName("created_at")]
	[BsonElement("created_at")]
	public DateTime CreatedAt { get; protected set; }
	
	[JsonIgnore]
	[BsonIgnore]
	public bool IsExpired => DateTime.UtcNow > this.ExpireTime;
	
	/// <summary>
	/// Stored for the TTL index of one time passwords (the lifetime differs per membership, so it must be an absolute time).
	/// Not serialized to clients.
	/// </summary>
	[JsonIgnore]
	[BsonElement("expire_time")]
	public DateTime ExpireTime => this.CreatedAt.Add(this.ExpiresIn);
	
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