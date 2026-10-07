using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Extensions.Mailing.MailChimp;

public class Variable
{
	#region Properties
	
	[JsonPropertyName("name")]
	public string? Name { get; set; }
	
	[JsonPropertyName("content")]
	public string? Content { get; set; }
	
	#endregion
}