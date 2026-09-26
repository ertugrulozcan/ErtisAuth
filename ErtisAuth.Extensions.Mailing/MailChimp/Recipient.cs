using System.Text.Json.Serialization;
using Newtonsoft.Json;

using JsonConverter = System.Text.Json.Serialization.JsonConverterAttribute;
using NewtonsoftJsonConverter = Newtonsoft.Json.JsonConverterAttribute;
using NewtonsoftStringEnumConverter = Newtonsoft.Json.Converters.StringEnumConverter;

namespace ErtisAuth.Extensions.Mailing.MailChimp;

public class Recipient
{
	#region Properties
	
	[JsonProperty("email")]
	[JsonPropertyName("email")]
	public string? Email { get; set; }
	
	[JsonProperty("name")]
	[JsonPropertyName("name")]
	public string? Name { get; set; }
	
	[JsonProperty("type")]
	[JsonPropertyName("type")]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	[NewtonsoftJsonConverter(typeof(NewtonsoftStringEnumConverter))]
	public RecipientType Type { get; set; }
	
	#endregion
}