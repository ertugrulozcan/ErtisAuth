using ErtisAuth.Core.Models.Webhooks;
using ErtisAuth.Sdk.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace ErtisAuth.Sdk.Services.Interfaces
{
	[ServiceLifetime(ServiceLifetime.Singleton)]
	public interface IWebhookService : IMembershipBoundedService<Webhook>
	{
		
	}
}