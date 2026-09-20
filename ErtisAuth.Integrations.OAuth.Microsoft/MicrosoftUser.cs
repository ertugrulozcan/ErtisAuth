using System.Text.Json.Serialization;
using ErtisAuth.Integrations.OAuth.Core;
using Newtonsoft.Json;

namespace ErtisAuth.Integrations.OAuth.Microsoft;

public class MicrosoftUser : IProviderUser
{
	#region Properties
	
	[JsonProperty("id")]
	[JsonPropertyName("id")]
	public string? Id { get; set; }
	
	[JsonProperty("displayName")]
	[JsonPropertyName("displayName")]
	public string? DisplayName { get; set; }
	
	[JsonProperty("givenName")]
	[JsonPropertyName("givenName")]
	public string? FirstName { get; set; }
	
	[JsonProperty("surname")]
	[JsonPropertyName("surname")]
	public string? LastName { get; set; }
	
	[JsonProperty("userPrincipalName")]
	[JsonPropertyName("userPrincipalName")]
	public string? UserPrincipalName { get; set; }
	
	[JsonProperty("mail")]
	[JsonPropertyName("mail")]
	public string? EmailAddress { get; set; }
	
	[JsonProperty("jobTitle")]
	[JsonPropertyName("jobTitle")]
	public string? JobTitle { get; set; }
	
	[JsonProperty("mobilePhone")]
	[JsonPropertyName("mobilePhone")]
	public string? MobilePhone { get; set; }
	
	[JsonProperty("businessPhones")]
	[JsonPropertyName("businessPhones")]
	public object[]? BusinessPhones { get; set; }
	
	[JsonProperty("officeLocation")]
	[JsonPropertyName("officeLocation")]
	public object? OfficeLocation { get; set; }
	
	[JsonProperty("preferredLanguage")]
	[JsonPropertyName("preferredLanguage")]
	public string? PreferredLanguage { get; set; }
	
	[JsonProperty("photo")]
	[JsonPropertyName("photo")]
	public string? Photo { get; set; }
	
	#endregion
}