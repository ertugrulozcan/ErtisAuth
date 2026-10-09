using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Integrations.OAuth.Facebook;

public class FacebookImage
{
	#region Properties
	
	[JsonPropertyName("url")]
	public string? Url { get; set; }
	
	[JsonPropertyName("width")]
	public int Width { get; set; }
	
	[JsonPropertyName("height")]
	public int Height { get; set; }
	
	[JsonPropertyName("is_silhouette")]
	public bool IsSilhouette { get; set; }
	
	#endregion
}