using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Users;

public class ResetPasswordFormModel
{
	#region Properties
	
	[JsonProperty("email_address")]
	[JsonPropertyName("email_address")]
	public string? EmailAddress { get; set; }
		
	#endregion
}