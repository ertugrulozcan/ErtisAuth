using System.Text.Json.Serialization;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Integrations.OAuth.Core;
using JsonProperty = Newtonsoft.Json.JsonPropertyAttribute;
using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

namespace ErtisAuth.Integrations.OAuth.Google;

public class GoogleLoginRequest : IProviderLoginRequest<GoogleToken, GoogleUser>
{
	#region Properties
	
	[JsonIgnore]
	[NewtonsoftJsonIgnore]
	public KnownProviders Provider => KnownProviders.Google;
	
	[JsonIgnore]
	[NewtonsoftJsonIgnore]
	public required GoogleUser User { get; set; }
	
	[JsonProperty("token")]
	[JsonPropertyName("token")]
	public GoogleToken? Token { get; set; }
	
	[JsonProperty("clientId")]
	[JsonPropertyName("clientId")]
	public string? ClientId { get; set; }
	
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
	public string? AvatarUrl => this.User.Picture;
	
	#endregion
	
	#region Methods
	
	public bool IsValid()
	{
		if (string.IsNullOrEmpty(this.ClientId))
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
		}
		else
		{
			return false;
		}
		
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
			SourceProvider = KnownProviders.Google.ToString(),
			ConnectedAccounts = new ProviderAccountInfo[]
			{
				new()
				{
					Provider = KnownProviders.Google.ToString(),
					UserId = this.UserId,
					Token = this.AccessToken
				}
			}
		};
	}
	
	#endregion
}