using System.Text.Json.Serialization;
using ErtisAuth.Integrations.OAuth.Core;

// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Integrations.OAuth.Google;

public class GoogleUser : IProviderUser
{
	#region Properties
	
	[JsonPropertyName("id")]
	public string? Id { get; set; }
	
	[JsonPropertyName("first_name")]
	public string? FirstName { get; set; }
	
	[JsonPropertyName("last_name")]
	public string? LastName { get; set; }
	
	[JsonPropertyName("name")]
	public string? FullName { get; set; }
	
	[JsonPropertyName("email")]
	public string? EmailAddress { get; set; }
	
	[JsonPropertyName("scope")]
    public string? Scope { get; set; }
	
	[JsonPropertyName("prn")]
    public string? Prn { get; set; }
	
	[JsonPropertyName("hd")]
    public string? HostedDomain { get; set; }
	
	[JsonPropertyName("email_verified")]
    public bool EmailVerified { get; set; }
	
	[JsonPropertyName("picture")]
    public string? Picture { get; set; }
	
	[JsonPropertyName("locale")]
    public string? Locale { get; set; }
	
	#endregion
}