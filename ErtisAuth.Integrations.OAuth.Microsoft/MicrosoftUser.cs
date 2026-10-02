using System.Text.Json.Serialization;
using ErtisAuth.Integrations.OAuth.Core;

// ReSharper disable UnusedMember.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Integrations.OAuth.Microsoft;

public class MicrosoftUser : IProviderUser
{
	#region Properties
	
	[JsonPropertyName("id")]
	public string? Id { get; set; }
	
	[JsonPropertyName("displayName")]
	public string? DisplayName { get; set; }
	
	[JsonPropertyName("givenName")]
	public string? FirstName { get; set; }
	
	[JsonPropertyName("surname")]
	public string? LastName { get; set; }
	
	[JsonPropertyName("userPrincipalName")]
	public string? UserPrincipalName { get; set; }
	
	[JsonPropertyName("mail")]
	public string? EmailAddress { get; set; }
	
	[JsonPropertyName("jobTitle")]
	public string? JobTitle { get; set; }
	
	[JsonPropertyName("mobilePhone")]
	public string? MobilePhone { get; set; }
	
	[JsonPropertyName("businessPhones")]
	public object[]? BusinessPhones { get; set; }
	
	[JsonPropertyName("officeLocation")]
	public object? OfficeLocation { get; set; }
	
	[JsonPropertyName("preferredLanguage")]
	public string? PreferredLanguage { get; set; }
	
	[JsonPropertyName("photo")]
	public string? Photo { get; set; }
	
	#endregion
}