using Ertis.Core.Collections;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Dao.Repositories.Interfaces;

namespace ErtisAuth.Infrastructure.Services;

public class EventService : MembershipBoundedService<ErtisAuthEvent>, IEventService
{
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="membershipService"></param>
	/// <param name="repository"></param>
	public EventService(IMembershipService membershipService, IEventRepository repository) : base(membershipService, repository)
	{
		
	}
	
	#endregion
	
	#region Events
	
	public event EventHandler<ErtisAuthEvent>? OnEventFired;
	
	#endregion
	
	#region Fire Methods
	
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
		this.OnEventFired?.Invoke(this, insertedEvent);
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
		this.OnEventFired?.Invoke(this, insertedEvent);
		return insertedEvent;
	}
	
	#endregion
	
	#region Get Methods
	
	public override ErtisAuthEvent? Get(string membershipId, string id)
	{
		var membership = this._membershipService.Get(membershipId);
		if (membership == null)
		{
			throw ErtisAuthException.MembershipNotFound(membershipId);
		}
		
		return this._repository.FindOne(x => x.Id == id && x.MembershipId == membershipId);
	}
	
	public override async ValueTask<ErtisAuthEvent?> GetAsync(string membershipId, string id, CancellationToken cancellationToken = default)
	{
		var membership = await this._membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
		if (membership == null)
		{
			throw ErtisAuthException.MembershipNotFound(membershipId);
		}
		
		return await this._repository.FindOneAsync(x => x.Id == id && x.MembershipId == membershipId, cancellationToken: cancellationToken);
	}
	
	public override IPaginationCollection<ErtisAuthEvent> Get(
		string membershipId, 
		int? skip = null, 
		int? limit = null, 
		bool withCount = false, 
		string? orderBy = null, 
		SortDirection? sortDirection = null)
	{
		var membership = this._membershipService.Get(membershipId);
		if (membership == null)
		{
			throw ErtisAuthException.MembershipNotFound(membershipId);
		}
		
		return this._repository.Find(x => x.MembershipId == membershipId, skip, limit, withCount, orderBy, sortDirection);
	}
	
	public override async ValueTask<IPaginationCollection<ErtisAuthEvent>> GetAsync(
		string membershipId, 
		int? skip = null, 
		int? limit = null, 
		bool withCount = false, 
		string? orderBy = null, 
		SortDirection? sortDirection = null, 
		CancellationToken cancellationToken = default)
	{
		var membership = await this._membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
		if (membership == null)
		{
			throw ErtisAuthException.MembershipNotFound(membershipId);
		}
		
		limit ??= Constants.PaginationDefaults.MAX_LIMIT;
		return await this._repository.FindAsync(x => x.MembershipId == membershipId, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
	}
	
	#endregion
}