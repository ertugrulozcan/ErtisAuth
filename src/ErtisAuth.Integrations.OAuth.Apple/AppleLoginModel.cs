using System.Text.Json.Serialization;
using Microsoft.IdentityModel.JsonWebTokens;

// ReSharper disable UnusedMember.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Integrations.OAuth.Apple;

public class AppleLoginModel
{
	#region Properties
	
	[JsonPropertyName("user")]
	public AppleUserModel? User { get; set; }
	
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
	
	[JsonPropertyName("name")]
	public AppleUserNameModel? Name { get; set; }
	
	[JsonPropertyName("email")]
	public string? EmailAddress { get; set; }
	
	#endregion
}

public class AppleUserNameModel
{
	#region Properties
	
	[JsonPropertyName("firstName")]
	public string? FirstName { get; set; }
	
	[JsonPropertyName("lastName")]
	public string? LastName { get; set; }
	
	#endregion
}

public class AppleUserAuthorizationModel
{
	#region Properties
	
	[JsonPropertyName("code")]
	public string? Code { get; set; }
	
	[JsonPropertyName("id_token")]
	public string? IdToken { get; set; }
	
	#endregion
}