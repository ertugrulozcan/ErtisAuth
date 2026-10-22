using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;

// ReSharper disable UnusedParameter.Global
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
	
	/// <summary>
	/// If the setup process fails, it deletes all events for the purpose of a rollback.
	/// </summary>
	/// <param name="membershipId"></param>
	/// <param name="utilizer"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task ClearAsync(string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default);
	
	event EventHandler<ErtisAuthEvent>? OnEventFired;
}