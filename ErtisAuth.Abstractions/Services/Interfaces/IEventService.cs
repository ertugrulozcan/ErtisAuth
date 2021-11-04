using System;
using System.Threading.Tasks;
using ErtisAuth.Core.Models.Events;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface IEventService : IMembershipBoundedService<ErtisAuthEventBase>, IDynamicResourceService
	{
		ValueTask<ErtisAuthEvent> FireEventAsync(object sender, ErtisAuthEvent ertisAuthEvent);
		
		ValueTask<ErtisAuthCustomEvent> FireEventAsync(object sender, ErtisAuthCustomEvent ertisAuthCustomEvent);

		event EventHandler<ErtisAuthEvent> EventFired;
		
		event EventHandler<ErtisAuthCustomEvent> CustomEventFired;
	}
}