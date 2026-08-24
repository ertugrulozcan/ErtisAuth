using ErtisAuth.Core.Models.Events;

// ReSharper disable EventNeverSubscribedTo.Global
namespace ErtisAuth.Abstractions.Services;

public interface IEventService : IMembershipBoundedService<ErtisAuthEventBase>, IDynamicResourceService
{
	ValueTask<ErtisAuthEvent> FireEventAsync(object sender, ErtisAuthEvent ertisAuthEvent, CancellationToken cancellationToken = default);
	
	ValueTask<ErtisAuthCustomEvent> FireEventAsync(object sender, ErtisAuthCustomEvent ertisAuthCustomEvent, CancellationToken cancellationToken = default);
	
	event EventHandler<ErtisAuthEvent>? EventFired;
	
	event EventHandler<ErtisAuthCustomEvent>? CustomEventFired;
}