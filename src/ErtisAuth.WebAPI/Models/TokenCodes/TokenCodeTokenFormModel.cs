using System.Text.Json.Serialization;

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.WebAPI.Models.TokenCodes;

/// <summary>
/// The token request of a device: the device code it got with its token code (sent in the body, not in the url, so
/// that it does not end up in access logs).
/// </summary>
public class TokenCodeTokenFormModel
{
	#region Properties
	
	[JsonPropertyName("device_code")]
	public string? DeviceCode { get; set; }
	
	#endregion
}
