using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Extensions.Mailing.MailChimp;

public class TemplateContentItem
{
	#region Properties
	
	[JsonProperty("name")]
	[JsonPropertyName("name")]
	public string? Name { get; set; }
	
	[JsonProperty("content")]
	[JsonPropertyName("content")]
	public string? Content { get; set; }
	
	#endregion
}