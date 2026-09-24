using ErtisAuth.Core.Models.Events;

// ReSharper disable EventNeverSubscribedTo.Global
namespace ErtisAuth.Abstractions.Services;

public interface IEventService : IMembershipBoundedService<ErtisAuthEvent>, IDynamicResourceService
{
	ValueTask<ErtisAuthEvent> FireEventAsync(object sender, ErtisAuthEvent ertisAuthEvent, CancellationToken cancellationToken = default);
	
	event EventHandler<ErtisAuthEvent>? EventFired;
}