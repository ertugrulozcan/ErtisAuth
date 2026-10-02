using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.WebAPI.Models.Providers;

public class UpdateProviderFormModel
{
	#region Properties
	
	[JsonPropertyName("_id")]
	public string? Id { get; set; }
	
	[JsonPropertyName("name")]
	public string? Name { get; set; }
	
	[JsonPropertyName("description")]
	public string? Description { get; set; }
	
	[JsonPropertyName("defaultRole")]
	public string? DefaultRole { get; set; }
	
	[JsonPropertyName("defaultUserType")]
	public string? DefaultUserType { get; set; }
	
	[JsonPropertyName("appClientId")]
	public string? AppClientId { get; set; }
	
	[JsonPropertyName("tenantId")]
	public string? TenantId { get; set; }
	
	[JsonPropertyName("teamId")]
	public string? TeamId { get; set; }
	
	[JsonPropertyName("privateKey")]
	public string? PrivateKey { get; set; }
	
	[JsonPropertyName("privateKeyId")]
	public string? PrivateKeyId { get; set; }
	
	[JsonPropertyName("redirectUri")]
	public string? RedirectUri { get; set; }
	
	[JsonPropertyName("isActive")]
	public bool? IsActive { get; set; }
	
	/// <summary>
	/// Omitted: the current value is kept.
	/// </summary>
	[JsonPropertyName("trust_email")]
	public bool? TrustEmail { get; set; }
	
	[JsonPropertyName("membership_id")]
	public string? MembershipId { get; set; }
	
	#endregion
}