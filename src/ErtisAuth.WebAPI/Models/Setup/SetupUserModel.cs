using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.WebAPI.Models.Setup;

public class SetupUserModel
{
	#region Properties
	
	[JsonPropertyName("username")]
	public string? Username { get; set; }
	
	[JsonPropertyName("firstname")]
	public string? FirstName { get; set; }
	
	[JsonPropertyName("lastname")]
	public string? LastName { get; set; }
	
	[JsonPropertyName("email_address")]
	public string? EmailAddress { get; set; }
	
	[JsonPropertyName("password")]
	public string? Password { get; set; }
	
	[JsonPropertyName("user_type")]
	public string? UserType { get; set; }
	
	#endregion
}