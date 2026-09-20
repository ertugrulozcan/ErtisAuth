using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Roles;

public class UpdateRoleFormModel
{
	#region Properties
	
	[JsonProperty("name")]
	[JsonPropertyName("name")]
	public string? Name { get; set; }
	
	[JsonProperty("slug")]
	[JsonPropertyName("slug")]
	public string? Slug { get; set; }
	
	[JsonProperty("description")]
	[JsonPropertyName("description")]
	public string? Description { get; set; }
	
	[JsonProperty("permissions")]
	[JsonPropertyName("permissions")]
	public IEnumerable<string>? Permissions { get; set; }
	
	[JsonProperty("forbidden")]
	[JsonPropertyName("forbidden")]
	public IEnumerable<string>? Forbidden { get; set; }
	
	#endregion
}