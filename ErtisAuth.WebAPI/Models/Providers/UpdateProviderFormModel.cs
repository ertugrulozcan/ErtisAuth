using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Providers;

public class UpdateProviderFormModel
{
	#region Properties
	
	[JsonProperty("_id")]
	[JsonPropertyName("_id")]
	public string? Id { get; set; }
	
	[JsonProperty("name")]
	[JsonPropertyName("name")]
	public string? Name { get; set; }
	
	[JsonProperty("description")]
	[JsonPropertyName("description")]
	public string? Description { get; set; }
	
	[JsonProperty("defaultRole")]
	[JsonPropertyName("defaultRole")]
	public string? DefaultRole { get; set; }
	
	[JsonProperty("defaultUserType")]
	[JsonPropertyName("defaultUserType")]
	public string? DefaultUserType { get; set; }
	
	[JsonProperty("appClientId")]
	[JsonPropertyName("appClientId")]
	public string? AppClientId { get; set; }
	
	[JsonProperty("tenantId")]
	[JsonPropertyName("tenantId")]
	public string? TenantId { get; set; }
	
	[JsonProperty("teamId")]
	[JsonPropertyName("teamId")]
	public string? TeamId { get; set; }
	
	[JsonProperty("privateKey")]
	[JsonPropertyName("privateKey")]
	public string? PrivateKey { get; set; }
	
	[JsonProperty("privateKeyId")]
	[JsonPropertyName("privateKeyId")]
	public string? PrivateKeyId { get; set; }
	
	[JsonProperty("redirectUri")]
	[JsonPropertyName("redirectUri")]
	public string? RedirectUri { get; set; }
	
	[JsonProperty("isActive")]
	[JsonPropertyName("isActive")]
	public bool? IsActive { get; set; }
	
	[JsonProperty("membership_id")]
	[JsonPropertyName("membership_id")]
	public string? MembershipId { get; set; }
	
	#endregion
}