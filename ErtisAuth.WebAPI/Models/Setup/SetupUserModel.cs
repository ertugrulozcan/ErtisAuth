using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Setup;

public class SetupUserModel
{
	#region Properties
	
	[JsonProperty("username")]
	[JsonPropertyName("username")]
	public string? Username { get; set; }
	
	[JsonProperty("firstname")]
	[JsonPropertyName("firstname")]
	public string? FirstName { get; set; }
	
	[JsonProperty("lastname")]
	[JsonPropertyName("lastname")]
	public string? LastName { get; set; }
	
	[JsonProperty("email_address")]
	[JsonPropertyName("email_address")]
	public string? EmailAddress { get; set; }
	
	[JsonProperty("password")]
	[JsonPropertyName("password")]
	public string? Password { get; set; }
	
	[JsonProperty("user_type")]
	[JsonPropertyName("user_type")]
	public string? UserType { get; set; }
	
	#endregion
}