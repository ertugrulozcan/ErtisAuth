using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Dao.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Infrastructure.Services;

public class EventService : MembershipBoundedService<ErtisAuthEvent>, IEventService
{
	#region Services
	
	private readonly ILogger<EventService> _logger;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="membershipService"></param>
	/// <param name="repository"></param>
	/// <param name="logger"></param>
	public EventService(IMembershipService membershipService, IEventRepository repository, ILogger<EventService> logger) : base(membershipService, repository)
	{
		this._logger = logger;
	}
	
	#endregion
	
	#region Events
	
	public event EventHandler<ErtisAuthEvent>? OnEventFired;
	
	#endregion
	
	#region Fire Methods
	
	/// <summary>
	/// Each subscriber is isolated: a failing one doesn't fail the caller (the event is already stored) or skip the others.
	/// </summary>
	private void NotifySubscribers(ErtisAuthEvent ertisAuthEvent)
	{
		var subscribers = this.OnEventFired?.GetInvocationList() ?? [];
		foreach (var subscriber in subscribers.Cast<EventHandler<ErtisAuthEvent>>())
		{
			try
			{
				subscriber(this, ertisAuthEvent);
			}
			catch (Exception ex)
			{
				this._logger.LogError(ex, "EventService: an OnEventFired subscriber failed for the {EventType} event", ertisAuthEvent.EventType);
			}
		}
	}
	
	public async Task<ErtisAuthEvent> FireEventAsync(
		ErtisAuthEventType type, 
		Utilizer utilizer,
		string? membershipId,
		object? document = null, 
		object? prior = null, 
		CancellationToken cancellationToken = default)
	{
		var ertisAuthEvent = new ErtisAuthEvent
		{
			EventType = type,
			UtilizerId = utilizer.Id,
			Document = document,
			Prior = prior,
			EventTime = DateTime.UtcNow,
			MembershipId = membershipId ?? utilizer.MembershipId ?? string.Empty
		};
		
		var insertedEvent = await this._repository.InsertAsync(ertisAuthEvent, cancellationToken: cancellationToken);
		this.NotifySubscribers(insertedEvent);
		return insertedEvent;
	}
	
	public async Task<ErtisAuthEvent> FireEventAsync(
		ErtisAuthEventType type,
		string utilizerId,
		string? membershipId,
		object? document = null,
		object? prior = null,
		CancellationToken cancellationToken = default)
	{
		var ertisAuthEvent = new ErtisAuthEvent
		{
			EventType = type,
			UtilizerId = utilizerId,
			Document = document,
			Prior = prior,
			EventTime = DateTime.UtcNow,
			MembershipId = membershipId ?? string.Empty
		};
		
		var insertedEvent = await this._repository.InsertAsync(ertisAuthEvent, cancellationToken: cancellationToken);
		this.NotifySubscribers(insertedEvent);
		return insertedEvent;
	}
	
	#endregion
}