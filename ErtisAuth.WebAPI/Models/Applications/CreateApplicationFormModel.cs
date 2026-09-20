using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Applications;

public class CreateApplicationFormModel
{
	#region Properties
	
	[JsonProperty("name")]
	[JsonPropertyName("name")]
	public string? Name { get; set; }
	
	[JsonProperty("slug")]
	[JsonPropertyName("slug")]
	public string? Slug { get; set; }
	
	[JsonProperty("role")]
	[JsonPropertyName("role")]
	public string? Role { get; set; }
	
	#endregion
}