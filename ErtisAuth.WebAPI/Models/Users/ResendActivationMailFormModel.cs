using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.WebAPI.Models.Users;

public class ResendActivationMailFormModel
{
	#region Properties
	
	[JsonPropertyName("email_address")]
	public string? EmailAddress { get; set; }
	
	#endregion
}