using System.Threading.Tasks;
using Ertis.Core.Models.Response;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Sdk.Services.Interfaces
{
	public interface IEventService : IReadonlyMembershipBoundedService<ErtisAuthEventLog>
	{
		IResponseResult<ErtisAuthCustomEvent> FireCustomEvent(string eventType, string utilizerId, object document, object prior, TokenBase token);
		
		Task<IResponseResult<ErtisAuthCustomEvent>> FireCustomEventAsync(string eventType, string utilizerId, object document, object prior, TokenBase token);
	}
}