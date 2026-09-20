using System.Text.Json.Serialization;
using ErtisAuth.Integrations.OAuth.Core;
using Newtonsoft.Json;

namespace ErtisAuth.Integrations.OAuth.Google;

public class GoogleUser : IProviderUser
{
	#region Properties
	
	[JsonProperty("id")]
	[JsonPropertyName("id")]
	public string? Id { get; set; }
	
	[JsonProperty("first_name")]
	[JsonPropertyName("first_name")]
	public string? FirstName { get; set; }
	
	[JsonProperty("last_name")]
	[JsonPropertyName("last_name")]
	public string? LastName { get; set; }
	
	[JsonProperty("name")]
	[JsonPropertyName("name")]
	public string? FullName { get; set; }
	
	[JsonProperty("email")]
	[JsonPropertyName("email")]
	public string? EmailAddress { get; set; }
	
	[JsonProperty("scope")]
	[JsonPropertyName("scope")]
    public string? Scope { get; set; }
	
    [JsonProperty("prn")]
	[JsonPropertyName("prn")]
    public string? Prn { get; set; }
	
    [JsonProperty("hd")]
	[JsonPropertyName("hd")]
    public string? HostedDomain { get; set; }
	
    [JsonProperty("email_verified")]
	[JsonPropertyName("email_verified")]
    public bool EmailVerified { get; set; }
	
    [JsonProperty("picture")]
	[JsonPropertyName("picture")]
    public string? Picture { get; set; }
	
    [JsonProperty("locale")]
	[JsonPropertyName("locale")]
    public string? Locale { get; set; }
	
	#endregion
}