using ErtisAuth.Integrations.OAuth.Core;
using Newtonsoft.Json;

namespace ErtisAuth.Integrations.OAuth.Apple;

public class AppleUser : IProviderUser
{
	#region Properties
	
	[JsonProperty("id")]
	public string? Id { get; set; }
	
	[JsonProperty("firstName")]
	public string? FirstName { get; set; }
	
	[JsonProperty("lastName")]
	public string? LastName { get; set; }
	
	[JsonProperty("email")]
	public string? EmailAddress { get; set; }
	
	#endregion
}