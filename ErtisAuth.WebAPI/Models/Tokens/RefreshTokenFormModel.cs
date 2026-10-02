using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.WebAPI.Models.Tokens;

public class RefreshTokenFormModel
{
	#region Properties
	
	[JsonPropertyName("token")]
	public string? Token { get; set; }
	
	#endregion
}