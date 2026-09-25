using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;

// ReSharper disable EventNeverSubscribedTo.Global
namespace ErtisAuth.Abstractions.Services;

public interface IEventService : IMembershipBoundedService<ErtisAuthEvent>
{
	Task<ErtisAuthEvent> FireEventAsync(
		ErtisAuthEventType type, 
		Utilizer utilizer, 
		string? membershipId, 
		object? document = null, 
		object? prior = null, 
		CancellationToken cancellationToken = default);
	
	Task<ErtisAuthEvent> FireEventAsync(
		ErtisAuthEventType type,
		string utilizerId,
		string? membershipId,
		object? document = null,
		object? prior = null,
		CancellationToken cancellationToken = default);
	
	event EventHandler<ErtisAuthEvent>? OnEventFired;
}