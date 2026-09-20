using System.Text.Json.Serialization;
using JsonProperty = Newtonsoft.Json.JsonPropertyAttribute;
using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

namespace ErtisAuth.WebAPI.Models.Users;

public class SetPasswordFormModel
{
	#region Properties
	
	[JsonProperty("email_address")]
	[JsonPropertyName("email_address")]
	public string? EmailAddress { get; set; }
	
	[JsonProperty("username")]
	[JsonPropertyName("username")]
	public string? Username { get; set; }
	
	[JsonProperty("reset_token")]
	[JsonPropertyName("reset_token")]
	public string? ResetToken { get; set; }
	
	[JsonProperty("password")]
	[JsonPropertyName("password")]
	public string? Password { get; set; }
	
	[JsonIgnore]
	[NewtonsoftJsonIgnore]
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