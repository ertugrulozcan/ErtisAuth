using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Extensions.Mailing.MailChimp;

public class TemplatePayload
{
	#region Properties
	
	[JsonPropertyName("key")]
	public string? Key { get; set; }
	
	[JsonPropertyName("template_name")]
	public string? TemplateName { get; set; }
	
	[JsonPropertyName("template_content")]
	public TemplateContentItem[]? TemplateContent { get; set; }
	
	[JsonPropertyName("message")]
	public Message? Message { get; set; }
	
	[JsonPropertyName("async")]
	public bool Async { get; set; }
	
	#endregion
}