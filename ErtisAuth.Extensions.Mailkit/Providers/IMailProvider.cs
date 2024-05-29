using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Extensions.Mailkit.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ErtisAuth.Extensions.Mailkit.Providers;

public interface IMailProvider
{
	#region Properties
	
	[JsonProperty("guid")]
	[JsonPropertyName("guid")]
	string Guid { get; }

	[JsonProperty("type")]
	[JsonPropertyName("type")]
	[Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
	[System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
	MailProviderType Type { get; }
	
	[JsonProperty("name")]
	[JsonPropertyName("name")]
	string Name { get; }
	
	[JsonProperty("slug")]
	[JsonPropertyName("slug")]
	string Slug { get; }

	#endregion

	#region Methods

	Task SendMailAsync(
		string fromName,
		string fromAddress,
		IEnumerable<Recipient> recipients,
		string subject,
		string htmlBody,
		CancellationToken cancellationToken = default);

	#endregion
}