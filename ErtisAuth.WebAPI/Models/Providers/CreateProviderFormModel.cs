using System.Text.Json.Serialization;
using ErtisAuth.Integrations.OAuth.Core;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.WebAPI.Models.Providers;

public class CreateProviderFormModel
{
	#region Properties
	
	[JsonPropertyName("type")]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public ProviderType? Type { get; set; }
	
	[JsonPropertyName("name")]
	public string? Name { get; set; }
	
	[JsonPropertyName("slug")]
	public string? Slug { get; set; }
	
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
	public bool IsActive { get; set; }
	
	/// <summary>
	/// Omitted: the current value is kept.
	/// </summary>
	[JsonPropertyName("trust_email")]
	public bool TrustEmail { get; set; }
	
	#endregion
}