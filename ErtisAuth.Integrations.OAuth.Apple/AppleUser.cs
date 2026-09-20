using System.Text.Json.Serialization;
using ErtisAuth.Integrations.OAuth.Core;
using Newtonsoft.Json;

namespace ErtisAuth.Integrations.OAuth.Apple;

public class AppleUser : IProviderUser
{
	#region Properties
	
	[JsonProperty("id")]
	[JsonPropertyName("id")]
	public string? Id { get; set; }
	
	[JsonProperty("firstName")]
	[JsonPropertyName("firstName")]
	public string? FirstName { get; set; }
	
	[JsonProperty("lastName")]
	[JsonPropertyName("lastName")]
	public string? LastName { get; set; }
	
	[JsonProperty("email")]
	[JsonPropertyName("email")]
	public string? EmailAddress { get; set; }
	
	#endregion
}