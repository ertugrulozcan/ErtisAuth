using System.Linq.Expressions;
using Ertis.Core.Collections;
using Ertis.MongoDB.Queries;
using Ertis.MongoDB.Repository;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Infrastructure.Helpers;

namespace ErtisAuth.Infrastructure.Services;

public abstract class MembershipBoundedService<TModel> : IMembershipBoundedService<TModel> 
	where TModel : class, Core.Models.IHasMembership, Core.Models.IHasIdentifier
{
	#region Services
	
	protected readonly IMembershipService _membershipService;
	protected readonly IMongoRepository<TModel> _repository;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="membershipService"></param>
	/// <param name="repository"></param>
	protected MembershipBoundedService(IMembershipService membershipService, IMongoRepository<TModel> repository)
	{
		this._membershipService = membershipService;
		this._repository = repository;
	}
	
	#endregion
	
	#region Query Methods
	
	public IPaginationCollection<dynamic> Query(
		string membershipId, 
		string query, 
		int? skip = null, 
		int? limit = null, 
		bool? withCount = null, 
		string? sortField = null,
		SortDirection? sortDirection = null, 
		IDictionary<string, bool>? selectFields = null)
	{
		return this._repository.Query(query, skip, limit, withCount, sortField, sortDirection, selectFields);
	}
	
	public async ValueTask<IPaginationCollection<dynamic>> QueryAsync(
		string membershipId, 
		string query, 
		int? skip = null, 
		int? limit = null, 
		bool? withCount = null, 
		string? sortField = null,
		SortDirection? sortDirection = null, 
		IDictionary<string, bool>? selectFields = null, 
		CancellationToken cancellationToken = default)
	{
		query = QueryHelper.InjectMembershipIdToQuery<dynamic>(query, membershipId);
		return await this._repository.QueryAsync(query, skip, limit, withCount, sortField, sortDirection, selectFields, cancellationToken: cancellationToken);
	}
	
	#endregion
	
	#region Get Methods
	
	public virtual TModel? Get(string membershipId, string id)
	{
		var membership = this._membershipService.Get(membershipId);
		if (membership == null)
		{
			throw ErtisAuthException.MembershipNotFound(membershipId);
		}
		
		return this._repository.FindOne(x => x.Id == id && x.MembershipId == membershipId);
	}
	
	public virtual async ValueTask<TModel?> GetAsync(
		string membershipId, 
		string id, 
		CancellationToken cancellationToken = default)
	{
		var membership = await this._membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
		if (membership == null)
		{
			throw ErtisAuthException.MembershipNotFound(membershipId);
		}
		
		return await this._repository.FindOneAsync(x => x.Id == id && x.MembershipId == membershipId, cancellationToken: cancellationToken);
	}
	
	protected async ValueTask<TModel?> GetAsync(
		string membershipId, 
		Expression<Func<TModel, bool>> expression, 
		CancellationToken cancellationToken = default)
	{
		var membership = await this._membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
		if (membership == null)
		{
			throw ErtisAuthException.MembershipNotFound(membershipId);
		}
		
		var entities = await this._repository.FindAsync(expression, sorting: null, cancellationToken: cancellationToken);
		return entities.Items.FirstOrDefault(x => x.MembershipId == membershipId);
	}
	
	public virtual IPaginationCollection<TModel> Get(
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
	
	public virtual async ValueTask<IPaginationCollection<TModel>> GetAsync(
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
	
	public T? Get<T>(string membershipId, string id) where T : class, Core.Models.IHasMembership
	{
		return this.Get(membershipId, id) as T;
	}
	
	public async ValueTask<T?> GetAsync<T>(string membershipId, string id, CancellationToken cancellationToken = default) where T : class, Core.Models.IHasMembership
	{
		return await this.GetAsync(membershipId, id, cancellationToken: cancellationToken) as T;
	}
	
	public IPaginationCollection<T> Get<T>(
		string membershipId, 
		int? skip, 
		int? limit, 
		bool withCount,
		string? orderBy, 
		SortDirection? sortDirection) 
		where T : class, Core.Models.IHasMembership
	{
		return (IPaginationCollection<T>) this.Get(membershipId, skip, limit, withCount, orderBy, sortDirection);
	}
	
	public async ValueTask<IPaginationCollection<T>> GetAsync<T>(
		string membershipId, 
		int? skip,
		int? limit, 
		bool withCount, 
		string? orderBy, 
		SortDirection? sortDirection,
		CancellationToken cancellationToken = default)
		where T : class, Core.Models.IHasMembership
	{
		return (IPaginationCollection<T>) await this.GetAsync(membershipId, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
	}
	
	#endregion
	
	#region Search Methods
	
	public IPaginationCollection<TModel> Search(
		string membershipId, 
		string keyword,
		int? skip = null,
		int? limit = null,
		bool? withCount = null,
		string? sortField = null,
		SortDirection? sortDirection = null)
	{
		var membership = this._membershipService.Get(membershipId);
		if (membership == null)
		{
			throw ErtisAuthException.MembershipNotFound(membershipId);
		}
		
		var textSearchLanguage = TextSearchLanguage.None;
		if (!string.IsNullOrEmpty(membership.DefaultLanguage) && TextSearchLanguage.All.Any(x => x.ISO6391Code == membership.DefaultLanguage))
		{
			textSearchLanguage = TextSearchLanguage.All.FirstOrDefault(x => x.ISO6391Code == membership.DefaultLanguage);
		}
		
		var textSearchOptions = new TextSearchOptions
		{
			Language = textSearchLanguage,
			IsCaseSensitive = true,
			IsDiacriticSensitive = false
		};
		
		return this._repository.Search(keyword, textSearchOptions, skip, limit, withCount, sortField, sortDirection);
	}
	
	public async ValueTask<IPaginationCollection<TModel>> SearchAsync(
		string membershipId, 
		string keyword,
		int? skip = null,
		int? limit = null,
		bool? withCount = null,
		string? sortField = null,
		SortDirection? sortDirection = null,
		CancellationToken cancellationToken = default)
	{
		var membership = await this._membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
		if (membership == null)
		{
			throw ErtisAuthException.MembershipNotFound(membershipId);
		}
		
		var textSearchLanguage = TextSearchLanguage.None;
		if (!string.IsNullOrEmpty(membership.DefaultLanguage) && TextSearchLanguage.All.Any(x => x.ISO6391Code == membership.DefaultLanguage))
		{
			textSearchLanguage = TextSearchLanguage.All.FirstOrDefault(x => x.ISO6391Code == membership.DefaultLanguage);
		}
		
		var textSearchOptions = new TextSearchOptions
		{
			Language = textSearchLanguage,
			IsCaseSensitive = true,
			IsDiacriticSensitive = false
		};
		
		return await this._repository.SearchAsync(keyword, textSearchOptions, skip, limit, withCount, sortField, sortDirection, cancellationToken: cancellationToken);
	}
	
	#endregion
	
	#region Aggregation Methods
	
	public dynamic Aggregate(string membershipId, string aggregationStagesJson)
	{
		return this._repository.Aggregate(QueryHelper.InjectMembershipIdToAggregation(aggregationStagesJson, membershipId));
	}
	
	public async Task<dynamic> AggregateAsync(string membershipId, string aggregationStagesJson, CancellationToken cancellationToken = default)
	{
		return await this._repository.AggregateAsync(QueryHelper.InjectMembershipIdToAggregation(aggregationStagesJson, membershipId), cancellationToken: cancellationToken);
	}
	
	#endregion
}