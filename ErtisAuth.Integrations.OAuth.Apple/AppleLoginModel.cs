using System.Text.Json.Serialization;
using Microsoft.IdentityModel.JsonWebTokens;
using Newtonsoft.Json;

namespace ErtisAuth.Integrations.OAuth.Apple;

public class AppleLoginModel
{
	#region Properties
	
	[JsonProperty("user")]
	[JsonPropertyName("user")]
	public AppleUserModel? User { get; set; }
	
	[JsonProperty("authorization")]
	[JsonPropertyName("authorization")]
	public AppleUserAuthorizationModel? Authorization { get; set; }
	
	#endregion
	
	#region Methods
	
	public AppleLoginRequestBase ToLoginRequest(bool isAppleNative)
	{
		var tokenHandler = new JsonWebTokenHandler();
		var jwtToken = tokenHandler.ReadJsonWebToken(this.Authorization?.IdToken);
		
		var userId = jwtToken.Subject;
		var expirationClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "exp");
		var expiresIn = expirationClaim != null && !string.IsNullOrEmpty(expirationClaim.Value) && long.TryParse(expirationClaim.Value, out var expiresIn_) ? expiresIn_ : 0;
		
		var user = new AppleUser
		{
			Id = userId,
			FirstName = this.User?.Name?.FirstName,
			LastName = this.User?.Name?.LastName,
			EmailAddress = this.User?.EmailAddress
		};
		
		var token = new AppleToken
		{
			IdToken = this.Authorization?.IdToken,
			Code = this.Authorization?.Code,
			ExpiresIn = expiresIn
		};
		
		if (isAppleNative)
		{
			return new AppleNativeLoginRequest
			{
				User = user,
				Token = token
			};
		}
		else
		{
			return new AppleLoginRequest
			{
				User = user,
				Token = token
			};
		}
	}
	
	#endregion
}

public class AppleUserModel
{
	#region Properties
	
	[JsonProperty("name")]
	[JsonPropertyName("name")]
	public AppleUserNameModel? Name { get; set; }
	
	[JsonProperty("email")]
	[JsonPropertyName("email")]
	public string? EmailAddress { get; set; }
	
	#endregion
}

public class AppleUserNameModel
{
	#region Properties
	
	[JsonProperty("firstName")]
	[JsonPropertyName("firstName")]
	public string? FirstName { get; set; }
	
	[JsonProperty("lastName")]
	[JsonPropertyName("lastName")]
	public string? LastName { get; set; }
	
	#endregion
}

public class AppleUserAuthorizationModel
{
	#region Properties
	
	[JsonProperty("code")]
	[JsonPropertyName("code")]
	public string? Code { get; set; }
	
	[JsonProperty("id_token")]
	[JsonPropertyName("id_token")]
	public string? IdToken { get; set; }
	
	#endregion
}