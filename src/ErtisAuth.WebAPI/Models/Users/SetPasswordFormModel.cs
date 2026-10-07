using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.WebAPI.Models.Users;

public class SetPasswordFormModel
{
	#region Properties
	
	[JsonPropertyName("email_address")]
	public string? EmailAddress { get; set; }
	
	[JsonPropertyName("username")]
	public string? Username { get; set; }
	
	[JsonPropertyName("reset_token")]
	public string? ResetToken { get; set; }
	
	[JsonPropertyName("password")]
	public string? Password { get; set; }
	
	[JsonIgnore]
	public string? UsernameOrEmailAddress
	{
		get
		{
			if (!string.IsNullOrEmpty(this.EmailAddress))
			{
				return this.EmailAddress;
			}
			else if (!string.IsNullOrEmpty(this.Username))
			{
				return this.Username;
			}
			
			return null;
		}
	}
	
	#endregion
}