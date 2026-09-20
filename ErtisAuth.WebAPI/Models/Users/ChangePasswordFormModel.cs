using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Users;

public class ChangePasswordFormModel
{
	#region Properties
	
	[JsonProperty("password")]
	[JsonPropertyName("password")]
	public string? Password { get; set; }
	
	#endregion
}