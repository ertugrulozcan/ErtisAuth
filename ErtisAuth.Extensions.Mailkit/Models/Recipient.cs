using Newtonsoft.Json;

namespace ErtisAuth.Extensions.Mailkit.Models;

public class Recipient
{
	#region Properties

	[JsonProperty("displayName")]
	public string DisplayName { get; set; }
	
	[JsonProperty("emailAddress")]
	public string EmailAddress { get; set; }

	#endregion
}