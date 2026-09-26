using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Extensions.Mailing.MailChimp;

public class Message
{
	#region Properties
	
	[JsonProperty("html")]
	[JsonPropertyName("html")]
	public string? Html { get; set; }
	
	[JsonProperty("text")]
	[JsonPropertyName("text")]
	public string? Text { get; set; }
	
	[JsonProperty("subject")]
	[JsonPropertyName("subject")]
	public string? Subject { get; set; }
	
	[JsonProperty("from_email")]
	[JsonPropertyName("from_email")]
	public string? FromEmail { get; set; }
	
	[JsonProperty("from_name")]
	[JsonPropertyName("from_name")]
	public string? FromName { get; set; }
	
	[JsonProperty("to")]
	[JsonPropertyName("to")]
	public Recipient[]? To { get; set; }
	
	[JsonProperty("global_merge_vars")]
	[JsonPropertyName("global_merge_vars")]
	public Variable[]? GlobalMergeVars { get; set; }
	
	#endregion
}