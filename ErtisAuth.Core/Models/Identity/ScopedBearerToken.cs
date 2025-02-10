using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

// ReSharper disable UnusedMember.Global
namespace ErtisAuth.Core.Models.Identity;

public class ScopedBearerToken : TokenBase
{
    #region Properties
	
	[JsonProperty("token_type")]
	[JsonPropertyName("token_type")]
	[Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
	[System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
	public override SupportedTokenTypes TokenType => SupportedTokenTypes.Bearer;
	
	[JsonProperty("scopes")]
	[JsonPropertyName("scopes")]
	public string[] Scopes { get; set; }
	
	#endregion

	#region Constructors

	/// <summary>
	/// Private Constructor
	/// </summary>
	private ScopedBearerToken()
	{
		
	}
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="token"></param>
	/// <param name="expiresIn"></param>
	public ScopedBearerToken(string token, TimeSpan expiresIn)
	{
		this.AccessToken = token;
		this.ExpiresIn = expiresIn;
		this.CreatedAt = DateTime.Now;
	}

	public ScopedBearerToken(BearerToken bearerToken, string[] scopes)
	{
		this.AccessToken = bearerToken.AccessToken;
		this.ExpiresIn = TimeSpan.FromSeconds(bearerToken.ExpiresInTimeStamp);
		this.CreatedAt = bearerToken.CreatedAt;
		this.Scopes = scopes;
	}

	#endregion

	#region Methods

	public static ScopedBearerToken CreateTemp(string token)
	{
		return new ScopedBearerToken
		{
			AccessToken = token,
			CreatedAt = DateTime.Now
		};
	}
	
	public static ScopedBearerToken ParseFromJson(string json)
	{
		var bearerToken = new ScopedBearerToken();
		
		var obj = JsonConvert.DeserializeObject(json);
		if (obj is JObject jObject)
		{
			if (jObject.TryGetValue("access_token", out var accessToken))
			{
				var access_token = accessToken.ToString();
				bearerToken.AccessToken = access_token;
			}
			
			if (jObject.TryGetValue("expires_in", out var expiresIn) && int.TryParse(expiresIn.ToString(), out var expires_in))
			{
				bearerToken.ExpiresIn = TimeSpan.FromSeconds(expires_in);
			}
			
			if (jObject.TryGetValue("created_at", out var createdAt) && DateTime.TryParse(createdAt.ToString(), out var created_at))
			{
				bearerToken.CreatedAt = created_at;
			}
		}

		return bearerToken;
	}

	#endregion
}