using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity;

public class ActivationToken
{
	#region Properties

	[JsonProperty("reset_token")]
	[JsonPropertyName("reset_token")]
	public string Token { get; protected set; }
		
	[Newtonsoft.Json.JsonIgnore]
	[System.Text.Json.Serialization.JsonIgnore]
	public TimeSpan ExpiresIn { get; protected set; }

	[JsonProperty("expires_in")]
	[JsonPropertyName("expires_in")]
	public int ExpiresInTimeStamp => (int) this.ExpiresIn.TotalSeconds;

	[JsonProperty("created_at")]
	[JsonPropertyName("created_at")]
	public DateTime CreatedAt { get; protected set; }

	[Newtonsoft.Json.JsonIgnore]
	[System.Text.Json.Serialization.JsonIgnore]
	public bool IsExpired => DateTime.Now > this.CreatedAt.Add(this.ExpiresIn);

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
		this.CreatedAt = DateTime.Now;
	}

	#endregion
}