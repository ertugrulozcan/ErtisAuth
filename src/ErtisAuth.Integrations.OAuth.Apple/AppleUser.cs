using System.Text.Json.Serialization;
using ErtisAuth.Integrations.OAuth.Core;

// ReSharper disable UnusedMember.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Integrations.OAuth.Apple;

public class AppleUser : IProviderUser
{
	#region Properties
	
	[JsonPropertyName("id")]
	public string? Id { get; set; }
	
	[JsonPropertyName("firstName")]
	public string? FirstName { get; set; }
	
	[JsonPropertyName("lastName")]
	public string? LastName { get; set; }
	
	[JsonPropertyName("email")]
	public string? EmailAddress { get; set; }
	
	/// <summary>
	/// Set by AppleAuthenticator from Apple's id_token; never read from the client payload.
	/// </summary>
	[JsonIgnore]
	public bool EmailVerified { get; set; }
	
	#endregion
}