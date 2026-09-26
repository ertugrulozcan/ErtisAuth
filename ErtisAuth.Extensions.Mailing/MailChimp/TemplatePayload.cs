using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Extensions.Mailing.MailChimp;

public class TemplatePayload
{
	#region Properties
	
	[JsonProperty("key")]
	[JsonPropertyName("key")]
	public string? Key { get; set; }
	
	[JsonProperty("template_name")]
	[JsonPropertyName("template_name")]
	public string? TemplateName { get; set; }
	
	[JsonProperty("template_content")]
	[JsonPropertyName("template_content")]
	public TemplateContentItem[]? TemplateContent { get; set; }
	
	[JsonProperty("message")]
	[JsonPropertyName("message")]
	public Message? Message { get; set; }
	
	[JsonProperty("async")]
	[JsonPropertyName("async")]
	public bool Async { get; set; }
	
	#endregion
}