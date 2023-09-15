using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ErtisAuth.Extensions.Mailkit.Providers;

public interface IMailProvider
{
	#region Properties
	
	[JsonProperty("guid")]
	string Guid { get; }

	[JsonProperty("type")]
	[JsonConverter(typeof(StringEnumConverter))]
	MailProviderType Type { get; }
	
	[JsonProperty("name")]
	string Name { get; }
	
	[JsonProperty("slug")]
	string Slug { get; }

	#endregion

	#region Methods

	Task SendMailAsync(
		string fromName,
		string fromAddress,
		string toName,
		string toAddress,
		string subject,
		string htmlBody,
		CancellationToken cancellationToken = default);

	#endregion
}