using ErtisAuth.Core.Models.Users;
using ErtisAuth.Integrations.OAuth.Core;
using Newtonsoft.Json;

namespace ErtisAuth.Integrations.OAuth.Apple;

public abstract class AppleLoginRequestBase : IProviderLoginRequest<AppleToken?, AppleUser?> 
{
	#region Properties
	
	[JsonIgnore]
	public abstract KnownProviders Provider { get; }
	
    [JsonProperty("user")]
    public AppleUser? User { get; set; }
    
    [JsonProperty("token")]
    public AppleToken? Token { get; set; }

    [JsonIgnore]
    public string? UserId => this.User?.Id;
    
    [JsonIgnore]
    public string? EmailAddress => this.User?.EmailAddress;
    
    [JsonIgnore]
    public string? AccessToken => this.Token?.AccessToken;
    
    [JsonIgnore]
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

    public object ToUser(string membershipId, string role, string userType)
    {
    	return new User
    	{
    		MembershipId = membershipId,
    		FirstName = this.User?.FirstName,
    		LastName = this.User?.LastName,
    		Username = this.User?.EmailAddress,
    		EmailAddress = this.User?.EmailAddress,
    		Role = role,
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
	public override KnownProviders Provider => KnownProviders.Apple;
	
	#endregion
}

public class AppleNativeLoginRequest : AppleLoginRequestBase
{
	#region Properties
	
	[JsonIgnore]
	public override KnownProviders Provider => KnownProviders.AppleNative;
	
	#endregion
}