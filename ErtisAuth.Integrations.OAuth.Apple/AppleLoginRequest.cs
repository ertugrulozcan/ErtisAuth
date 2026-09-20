using System.Text.Json.Serialization;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Integrations.OAuth.Core;
using JsonProperty = Newtonsoft.Json.JsonPropertyAttribute;
using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

namespace ErtisAuth.Integrations.OAuth.Apple;

public abstract class AppleLoginRequestBase : IProviderLoginRequest<AppleToken, AppleUser> 
{
	#region Properties
	
	[JsonIgnore]
	[NewtonsoftJsonIgnore]
	public abstract KnownProviders Provider { get; }
	
    [JsonProperty("user")]
	[JsonPropertyName("user")]
    public required AppleUser User { get; set; }
    
    [JsonProperty("token")]
	[JsonPropertyName("token")]
    public AppleToken? Token { get; set; }
	
    [JsonIgnore]
	[NewtonsoftJsonIgnore]
    public string? UserId => this.User.Id;
    
    [JsonIgnore]
	[NewtonsoftJsonIgnore]
    public string? EmailAddress => this.User.EmailAddress;
    
    [JsonIgnore]
	[NewtonsoftJsonIgnore]
    public string? AccessToken => this.Token?.AccessToken;
    
    [JsonIgnore]
	[NewtonsoftJsonIgnore]
    public string? AvatarUrl => null;
	
    #endregion
	
    #region Methods
	
    public bool IsValid()
    {
    	if (this.User is { } user)
    	{
    		if (string.IsNullOrEmpty(user.Id))
    		{
    			return false;
    		}
			
    		if (string.IsNullOrEmpty(user.FirstName))
    		{
    			return false;
    		}
			
    		if (string.IsNullOrEmpty(user.EmailAddress))
    		{
    			return false;
    		}
    	}
    	else
    	{
    		return false;
    	}
		
    	// ReSharper disable once ConvertIfStatementToReturnStatement
    	if (this.Token == null || string.IsNullOrEmpty(this.Token.AccessToken))
    	{
    		return false;
    	}
		
    	return true;
    }
	
    public object ToUser(string membershipId, string? role, string? userType)
    {
    	return new User
    	{
    		MembershipId = membershipId,
    		FirstName = this.User.FirstName,
    		LastName = this.User.LastName,
    		Username = this.User.EmailAddress ?? string.Empty,
    		EmailAddress = this.User.EmailAddress,
    		Role = role ?? string.Empty,
    		UserType = userType,
    		SourceProvider = KnownProviders.Apple.ToString(),
    		ConnectedAccounts = new ProviderAccountInfo[]
    		{
    			new()
    			{
    				Provider = KnownProviders.Apple.ToString(),
    				UserId = this.UserId,
    				Token = this.AccessToken
    			}
    		}
    	};
    }
	
    #endregion
}

public class AppleLoginRequest : AppleLoginRequestBase
{
	#region Properties
	
	[JsonIgnore]
	[NewtonsoftJsonIgnore]
	public override KnownProviders Provider => KnownProviders.Apple;
	
	#endregion
}

public class AppleNativeLoginRequest : AppleLoginRequestBase
{
	#region Properties
	
	[JsonIgnore]
	[NewtonsoftJsonIgnore]
	public override KnownProviders Provider => KnownProviders.AppleNative;
	
	#endregion
}