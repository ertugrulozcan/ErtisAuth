using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace ErtisAuth.Core.Models.Identity;

public class BearerToken : TokenBase, IRefreshableToken
{
	#region Properties
	
	[JsonProperty("token_type")]
	[JsonPropertyName("token_type")]
	[Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
	[System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
	public override SupportedTokenTypes TokenType => SupportedTokenTypes.Bearer;
	
	[JsonProperty("refresh_token")]
	[JsonPropertyName("refresh_token")]
	public string? RefreshToken { get; private set; }
	
	[Newtonsoft.Json.JsonIgnore]
	[System.Text.Json.Serialization.JsonIgnore]
	public TimeSpan RefreshExpiresIn { get; private set; }
	
	[JsonProperty("refresh_token_expires_in")]
	[JsonPropertyName("refresh_token_expires_in")]
	public int RefreshTokenExpiresInTimeStamp => (int) this.RefreshExpiresIn.TotalSeconds;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Private Constructor
	/// </summary>
	private BearerToken()
	{
		
	}
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="token"></param>
	/// <param name="expiresIn"></param>
	/// <param name="refreshToken"></param>
	/// <param name="refreshExpiresIn"></param>
	public BearerToken(string token, TimeSpan expiresIn, string? refreshToken, TimeSpan refreshExpiresIn)
	{
		this.AccessToken = token;
		this.ExpiresIn = expiresIn;
		this.RefreshToken = refreshToken;
		this.RefreshExpiresIn = refreshExpiresIn;
		this.CreatedAt = DateTime.Now;
	}
	
	#endregion
	
	#region Methods
	
	public static BearerToken CreateTemp(string token)
	{
		return new BearerToken
		{
			AccessToken = token,
			CreatedAt = DateTime.Now
		};
	}
	
	public static BearerToken ParseFromJson(string json)
	{
		var bearerToken = new BearerToken();
		
		var obj = JsonConvert.DeserializeObject(json);
		if (obj is JObject jObject)
		{
			if (jObject.TryGetValue("access_token", out var accessTokenNode))
			{
				var access_token = accessTokenNode.ToString();
				bearerToken.AccessToken = access_token;
			}
			
			if (jObject.TryGetValue("refresh_token", out var refreshTokenNode))
			{
				var refresh_token = refreshTokenNode.ToString();
				bearerToken.RefreshToken = refresh_token;
			}
			
			if (jObject.TryGetValue("expires_in", out var expiresInNode))
			{
				int.TryParse(expiresInNode.ToString(), out var expires_in);
				bearerToken.ExpiresIn = TimeSpan.FromSeconds(expires_in);
			}
			
			if (jObject.TryGetValue("refresh_token_expires_in", out var refreshTokenExpiresInNode))
			{
				int.TryParse(refreshTokenExpiresInNode.ToString(), out var refresh_token_expires_in);
				bearerToken.RefreshExpiresIn = TimeSpan.FromSeconds(refresh_token_expires_in);
			}
			
			if (jObject.TryGetValue("created_at", out var createdAtNode))
			{
				DateTime.TryParse(createdAtNode.ToString(), out var created_at);
				bearerToken.CreatedAt = created_at;
			}
		}
		
		return bearerToken;
	}
	
	#endregion
}