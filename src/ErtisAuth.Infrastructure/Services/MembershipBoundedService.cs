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
	
	#region Properties
	
	/// <summary>
	/// Fields never returned (e.g. secrets), which therefore can not be filtered or sorted on either.
	/// </summary>
	protected virtual IReadOnlyCollection<string> HiddenFields => [];
	
	#endregion
	
	#region Query Methods
	
	public virtual async Task<IPaginationCollection<dynamic>> QueryAsync(
		string query,
		string membershipId, 
		int? skip = null, 
		int? limit = null, 
		bool? withCount = null, 
		string? sortField = null,
		SortDirection? sortDirection = null, 
		IDictionary<string, bool>? projection = null, 
		CancellationToken cancellationToken = default)
	{
		query = QueryHelper.InjectMembershipIdToQuery<dynamic>(query, membershipId, this.HiddenFields);
		QueryHelper.EnsureSortable(sortField, this.HiddenFields);
		limit ??= Constants.PaginationDefaults.MAX_LIMIT;
		return await this._repository.QueryAsync(query, skip, limit, withCount, sortField, sortDirection, projection, cancellationToken: cancellationToken);
	}
	
	#endregion
	
	#region Get Methods
	
	public virtual async Task<TModel?> GetAsync(string id, string membershipId, CancellationToken cancellationToken = default)
	{
		var membership = await this._membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
		if (membership == null)
		{
			throw ErtisAuthException.MembershipNotFound(membershipId);
		}
		
		return await this._repository.FindOneAsync(x => x.Id == id && x.MembershipId == membershipId, cancellationToken: cancellationToken);
	}
	
	protected async Task<TModel?> GetAsync(
		Expression<Func<TModel, bool>> expression, 
		string membershipId, 
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
	
	public virtual async Task<IPaginationCollection<TModel>> GetAsync(
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
		
		QueryHelper.EnsureSortable(orderBy, this.HiddenFields);
		limit ??= Constants.PaginationDefaults.MAX_LIMIT;
		return await this._repository.FindAsync(x => x.MembershipId == membershipId, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
	}
	
	public async Task<T?> GetAsync<T>(string id, string membershipId, CancellationToken cancellationToken = default) where T : class, Core.Models.IHasMembership
	{
		return await this.GetAsync(id, membershipId, cancellationToken: cancellationToken) as T;
	}
	
	public async Task<IPaginationCollection<T>> GetAsync<T>(
		string membershipId, 
		int? skip,
		int? limit, 
		bool withCount, 
		string? orderBy, 
		SortDirection? sortDirection,
		CancellationToken cancellationToken = default)
		where T : class, Core.Models.IHasMembership
	{
		limit ??= Constants.PaginationDefaults.MAX_LIMIT;
		return (IPaginationCollection<T>) await this.GetAsync(membershipId, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
	}
	
	#endregion
	
	#region Search Methods
	
	public async Task<IPaginationCollection<TModel>> SearchAsync(
		string keyword, 
		string membershipId, 
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
		
		var query = QueryHelper.FullTextSearchQuery(membershipId, keyword, textSearchLanguage.ISO6391Code);
		QueryHelper.EnsureSortable(sortField, this.HiddenFields);
		limit ??= Constants.PaginationDefaults.MAX_LIMIT;
		return await this._repository.FindAsync(query, skip, limit, withCount, sortField, sortDirection, cancellationToken: cancellationToken);
	}
	
	#endregion
	
	#region Aggregation Methods
	
	public async Task<dynamic> AggregateAsync(string aggregationStagesJson, string membershipId, CancellationToken cancellationToken = default)
	{
		return await this._repository.AggregateAsync(QueryHelper.InjectMembershipIdToAggregation(aggregationStagesJson, membershipId), cancellationToken: cancellationToken);
	}
	
	#endregion
}