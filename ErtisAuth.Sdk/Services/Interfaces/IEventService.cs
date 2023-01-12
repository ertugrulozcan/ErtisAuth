using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Models.Response;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Sdk.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace ErtisAuth.Sdk.Services.Interfaces
{
	[ServiceLifetime(ServiceLifetime.Singleton)]
	public interface IEventService : IReadonlyMembershipBoundedService<ErtisAuthEventLog>
	{
		IResponseResult<ErtisAuthCustomEvent> FireCustomEvent(string eventType, string utilizerId, object document, object prior, TokenBase token);
		
		Task<IResponseResult<ErtisAuthCustomEvent>> FireCustomEventAsync(string eventType, string utilizerId, object document, object prior, TokenBase token, CancellationToken cancellationToken = default);
	}
}