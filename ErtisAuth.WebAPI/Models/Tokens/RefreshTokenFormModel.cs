using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Tokens;

public class RefreshTokenFormModel
{
	#region Properties
	
	[JsonProperty("token")]
	[JsonPropertyName("token")]
	public string? Token { get; set; }
	
	#endregion
}