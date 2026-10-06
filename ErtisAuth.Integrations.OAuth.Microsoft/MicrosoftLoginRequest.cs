using System.Text.Json.Serialization;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Integrations.OAuth.Core;

// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace ErtisAuth.Integrations.OAuth.Microsoft;

public class MicrosoftLoginRequest : IProviderLoginRequest<MicrosoftToken, MicrosoftUser>
{
	#region Properties
	
	[JsonIgnore]
	public ProviderType Provider => ProviderType.Microsoft;
	
	[JsonIgnore]
	public MicrosoftUser? User { get; set; }
	
	[JsonPropertyName("token")]
	public MicrosoftToken? Token { get; set; }
	
	[JsonPropertyName("clientId")]
	public string? ClientId { get; set; }
	
	[JsonIgnore]
	public string? UserId => this.User?.Id;
	
	[JsonIgnore]
	public string? EmailAddress => this.User?.EmailAddress;
	
	[JsonIgnore]
	public bool IsEmailVerified => false;
	
	[JsonIgnore]
	public string? AccessToken => this.Token?.AccessToken;
	
	[JsonIgnore]
	public string? AvatarUrl => this.User?.Photo;
	
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
			SourceProvider = ProviderType.Microsoft.ToString()
		};
	}
	
	#endregion
}