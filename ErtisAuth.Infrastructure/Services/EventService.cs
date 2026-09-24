using System.Text.Json;
using Ertis.Core.Collections;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Dao.Repositories.Interfaces;
using MongoDB.Bson;

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
	
	public event EventHandler<ErtisAuthEvent>? EventFired;
	
	#endregion
	
	#region Fire Methods
	
	public async ValueTask<ErtisAuthEvent> FireEventAsync(object sender, ErtisAuthEvent ertisAuthEvent, CancellationToken cancellationToken = default)
	{
		var insertedEvent = await this.SaveEventAsync(ertisAuthEvent, cancellationToken: cancellationToken);
		ertisAuthEvent.Id = insertedEvent.Id;
		this.EventFired?.Invoke(sender, ertisAuthEvent);
		return ertisAuthEvent;
	}
	
	private async Task<ErtisAuthEvent> SaveEventAsync(ErtisAuthEvent ertisAuthEvent, CancellationToken cancellationToken = default)
	{
		BsonDocument? documentBson = null;
		if (ertisAuthEvent.Document != null)
		{
			string documentJson = JsonSerializer.Serialize(ertisAuthEvent.Document);
			documentBson = BsonDocument.Parse(documentJson);
		}
		
		BsonDocument? priorBson = null;
		if (ertisAuthEvent.Prior != null)
		{
			string priorJson = JsonSerializer.Serialize(ertisAuthEvent.Prior);
			priorBson = BsonDocument.Parse(priorJson);
		}
		
		var now = DateTime.Now;
		var utc = now.ToUniversalTime();
		var local = now.ToLocalTime();
		var timeZoneDiff = local - utc;
		ertisAuthEvent.EventTime = utc.Add(timeZoneDiff);
		
		ertisAuthEvent.Document = documentBson;
		ertisAuthEvent.Prior = priorBson;
		
		return await this._repository.InsertAsync(ertisAuthEvent, cancellationToken: cancellationToken);
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
		
		return await this._repository.FindAsync(x => x.MembershipId == membershipId, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
	}
	
	#endregion
	
	#region Dynamics
	
	public dynamic? GetDynamic(string membershipId, string id)
	{
		var membership = this._membershipService.Get(membershipId);
		if (membership == null)
		{
			throw ErtisAuthException.MembershipNotFound(membershipId);
		}
		
		var result = this._repository.Query(x => x.Id == id && x.MembershipId == membershipId, 0, 1, sorting: null);
		return result.Items.FirstOrDefault();
	}
	
	public async ValueTask<dynamic?> GetDynamicAsync(string membershipId, string id, CancellationToken cancellationToken = default)
	{
		var membership = await this._membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
		if (membership == null)
		{
			throw ErtisAuthException.MembershipNotFound(membershipId);
		}
		
		var result = await this._repository.QueryAsync(x => x.Id == id && x.MembershipId == membershipId, 0, 1, sorting: null, cancellationToken: cancellationToken);
		return result.Items.FirstOrDefault();
	}
	
	public IPaginationCollection<dynamic> GetDynamic(string membershipId, int? skip, int? limit, bool withCount, string? orderBy, SortDirection? sortDirection)
	{
		var membership = this._membershipService.Get(membershipId);
		if (membership == null)
		{
			throw ErtisAuthException.MembershipNotFound(membershipId);
		}
		
		return this._repository.Query(x => x.MembershipId == membershipId, skip, limit, withCount, orderBy, sortDirection);
	}
	
	public async ValueTask<IPaginationCollection<dynamic>> GetDynamicAsync(string membershipId, int? skip, int? limit, bool withCount, string? orderBy, SortDirection? sortDirection, CancellationToken cancellationToken = default)
	{
		var membership = await this._membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
		if (membership == null)
		{
			throw ErtisAuthException.MembershipNotFound(membershipId);
		}
		
		return await this._repository.QueryAsync(x => x.MembershipId == membershipId, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
	}
	
	#endregion
}