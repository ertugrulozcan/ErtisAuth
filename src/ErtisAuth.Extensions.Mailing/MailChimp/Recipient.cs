using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Extensions.Mailing.MailChimp;

public class Recipient
{
	#region Properties
	
	[JsonPropertyName("email")]
	public string? Email { get; set; }
	
	[JsonPropertyName("name")]
	public string? Name { get; set; }
	
	[JsonPropertyName("type")]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public RecipientType Type { get; set; }
	
	#endregion
}