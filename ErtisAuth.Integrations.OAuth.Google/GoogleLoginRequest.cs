using System.Text.Json.Serialization;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Integrations.OAuth.Core;

// ReSharper disable UnusedMember.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Integrations.OAuth.Google;

public class GoogleLoginRequest : IProviderLoginRequest<GoogleToken, GoogleUser>
{
	#region Properties
	
	[JsonIgnore]
	public ProviderType Provider => ProviderType.Google;
	
	[JsonIgnore]
	public GoogleUser? User { get; set; }
	
	[JsonPropertyName("token")]
	public GoogleToken? Token { get; set; }
	
	[JsonPropertyName("clientId")]
	public string? ClientId { get; set; }
	
	[JsonIgnore]
	public string? UserId => this.User?.Id;
	
	[JsonIgnore]
	public string? EmailAddress => this.User?.EmailAddress;
	
	[JsonIgnore]
	public bool IsEmailVerified => this.User?.EmailVerified ?? false;
	
	[JsonIgnore]
	public string? AccessToken => this.Token?.AccessToken;
	
	[JsonIgnore]
	public string? AvatarUrl => this.User?.Picture;
	
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
			FirstName = this.User?.FirstName,
			LastName = this.User?.LastName,
			Username = this.User?.EmailAddress ?? string.Empty,
			EmailAddress = this.User?.EmailAddress,
			Role = role ?? string.Empty,
			UserType = userType,
			SourceProvider = ProviderType.Google.ToString()
		};
	}
	
	#endregion
}