using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.WebAPI.Models.Applications;

public class CreateApplicationFormModel
{
	#region Properties
	
	[JsonPropertyName("name")]
	public string? Name { get; set; }
	
	[JsonPropertyName("slug")]
	public string? Slug { get; set; }
	
	[JsonPropertyName("role")]
	public string? Role { get; set; }
	
	[JsonPropertyName("permissions")]
	public IEnumerable<string>? Permissions { get; set; }
	
	[JsonPropertyName("forbidden")]
	public IEnumerable<string>? Forbidden { get; set; }
	
	#endregion
}