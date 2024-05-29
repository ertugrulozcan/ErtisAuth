using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Extensions.Mailkit.Models;

public class Recipient
{
	#region Properties

	[JsonProperty("displayName")]
	[JsonPropertyName("displayName")]
	public string DisplayName { get; set; }
	
	[JsonProperty("emailAddress")]
	[JsonPropertyName("emailAddress")]
	public string EmailAddress { get; set; }

	#endregion
}