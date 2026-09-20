using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Integrations.OAuth.Facebook;

public class FacebookImage
{
	#region Properties
	
	[JsonProperty("url")]
	[JsonPropertyName("url")]
	public string? Url { get; set; }
	
	[JsonProperty("width")]
	[JsonPropertyName("width")]
	public int Width { get; set; }
	
	[JsonProperty("height")]
	[JsonPropertyName("height")]
	public int Height { get; set; }
	
	[JsonProperty("is_silhouette")]
	[JsonPropertyName("is_silhouette")]
	public bool IsSilhouette { get; set; }
	
	#endregion
}