using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Extensions.Mailing.MailChimp;

public class Message
{
	#region Properties
	
	[JsonPropertyName("html")]
	public string? Html { get; set; }
	
	[JsonPropertyName("text")]
	public string? Text { get; set; }
	
	[JsonPropertyName("subject")]
	public string? Subject { get; set; }
	
	[JsonPropertyName("from_email")]
	public string? FromEmail { get; set; }
	
	[JsonPropertyName("from_name")]
	public string? FromName { get; set; }
	
	[JsonPropertyName("to")]
	public Recipient[]? To { get; set; }
	
	[JsonPropertyName("global_merge_vars")]
	public Variable[]? GlobalMergeVars { get; set; }
	
	#endregion
}