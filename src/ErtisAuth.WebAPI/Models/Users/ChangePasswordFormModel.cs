using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.WebAPI.Models.Users;

public class ChangePasswordFormModel
{
	#region Properties
	
	[JsonPropertyName("password")]
	public string? Password { get; set; }
	
	#endregion
}