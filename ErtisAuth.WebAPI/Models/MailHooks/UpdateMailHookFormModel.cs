using System.Text.Json.Serialization;
using ErtisAuth.Core.Models.Mailing;
using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.MailHooks;

/// <summary>
/// The update request of a mail hook; the id and the membership come from the route.
/// </summary>
public class UpdateMailHookFormModel
{
	#region Properties
	
	[JsonProperty("name")]
	[JsonPropertyName("name")]
	public string? Name { get; set; }
	
	[JsonProperty("slug")]
	[JsonPropertyName("slug")]
	public string? Slug { get; set; }
	
	[JsonProperty("description")]
	[JsonPropertyName("description")]
	public string? Description { get; set; }
	
	[JsonProperty("event")]
	[JsonPropertyName("event")]
	public string? Event { get; set; }
	
	[JsonProperty("status")]
	[JsonPropertyName("status")]
	public string? Status { get; set; }
	
	[JsonProperty("mailSubject")]
	[JsonPropertyName("mailSubject")]
	public string? MailSubject { get; set; }
	
	[JsonProperty("mailTemplate")]
	[JsonPropertyName("mailTemplate")]
	public string? MailTemplate { get; set; }
	
	[JsonProperty("fromName")]
	[JsonPropertyName("fromName")]
	public string? FromName { get; set; }
	
	[JsonProperty("fromAddress")]
	[JsonPropertyName("fromAddress")]
	public string? FromAddress { get; set; }
	
	[JsonProperty("sendToUtilizer")]
	[JsonPropertyName("sendToUtilizer")]
	public bool SendToUtilizer { get; set; }
	
	[JsonProperty("recipients")]
	[JsonPropertyName("recipients")]
	public Recipient[]? Recipients { get; set; }
	
	[JsonProperty("mailProvider")]
	[JsonPropertyName("mailProvider")]
	public string? MailProvider { get; set; }
	
	[JsonProperty("variables")]
	[JsonPropertyName("variables")]
	public MailHookVariable[]? Variables { get; set; }
	
	#endregion
}