using System.Text.Json.Serialization;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Integrations.OAuth.Core;
using JsonProperty = Newtonsoft.Json.JsonPropertyAttribute;
using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

namespace ErtisAuth.Integrations.OAuth.Facebook;

public class FacebookLoginRequest : IProviderLoginRequest<FacebookUserToken, FacebookUserToken>
{
	#region Properties
	
	[JsonIgnore]
	[NewtonsoftJsonIgnore]
	public KnownProviders Provider => KnownProviders.Facebook;
	
	[JsonProperty("user")]
	[JsonPropertyName("user")]
	public required FacebookUserToken User { get; set; }
	
	[JsonProperty("appId")]
	[JsonPropertyName("appId")]
	public string? AppId { get; set; }
	
	[JsonIgnore]
	[NewtonsoftJsonIgnore]
	public FacebookUserToken? Token { get; set; }
	
	[JsonIgnore]
	[NewtonsoftJsonIgnore]
	public string? AccessToken => this.User.AccessToken ?? this.Token?.AccessToken;
	
	[JsonIgnore]
	[NewtonsoftJsonIgnore]
	public string? UserId => this.User.Id;
	
	[JsonIgnore]
	[NewtonsoftJsonIgnore]
	public string? EmailAddress => this.User.EmailAddress;
	
	[JsonIgnore]
	[NewtonsoftJsonIgnore]
	public string? AvatarUrl => this.User.Picture?.Data?.Url;
	
	[JsonIgnore]
	[NewtonsoftJsonIgnore]
	public bool IsLimited { get; set; }
	
	#endregion
	
	#region Methods
	
	public bool IsValid()
	{
		if (string.IsNullOrEmpty(this.AppId))
		{
			return false;
		}
		
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
			
			if (string.IsNullOrEmpty(user.AccessToken))
			{
				return false;
			}
		}
		else
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
			SourceProvider = KnownProviders.Facebook.ToString(),
			ConnectedAccounts = new ProviderAccountInfo[]
			{
				new()
				{
					Provider = KnownProviders.Facebook.ToString(),
					UserId = this.UserId,
					Token = this.AccessToken
				}
			}
		};
	}
	
	#endregion
}