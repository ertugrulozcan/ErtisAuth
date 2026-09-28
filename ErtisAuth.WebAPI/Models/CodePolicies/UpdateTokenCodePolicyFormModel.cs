using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.CodePolicies;

/// <summary>
/// The update request of a token code policy; the id and the membership come from the route.
/// </summary>
public class UpdateTokenCodePolicyFormModel
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
	
	[JsonProperty("length")]
	[JsonPropertyName("length")]
	public int Length { get; set; }
	
	[JsonProperty("contains_letters")]
	[JsonPropertyName("contains_letters")]
	public bool ContainsLetters { get; set; }
	
	[JsonProperty("contains_digits")]
	[JsonPropertyName("contains_digits")]
	public bool ContainsDigits { get; set; }
	
	[JsonProperty("expires_in")]
	[JsonPropertyName("expires_in")]
	public int ExpiresIn { get; set; }
	
	#endregion
}