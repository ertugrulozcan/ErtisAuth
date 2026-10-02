using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Integrations.OAuth.Facebook;

public class FacebookImageData
{
	#region Properties
	
	[JsonPropertyName("data")]
	public FacebookImage? Data { get; set; }
	
	#endregion
}