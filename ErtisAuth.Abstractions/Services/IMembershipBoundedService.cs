using Ertis.Core.Collections;
using ErtisAuth.Core.Models;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedParameter.Global
// ReSharper disable UnusedMemberInSuper.Global
namespace ErtisAuth.Abstractions.Services;

public interface IMembershipBoundedService
{
	TModel? Get<TModel>(string membershipId, string id) where TModel : class, IHasMembership;
	
	Task<TModel?> GetAsync<TModel>(string membershipId, string id, CancellationToken cancellationToken = default) where TModel : class, IHasMembership;
	
	IPaginationCollection<TModel> Get<TModel>(
		string membershipId, 
		int? skip, 
		int? limit, 
		bool withCount, 
		string? orderBy, 
		SortDirection? sortDirection) 
		where TModel : class, IHasMembership;
	
	Task<IPaginationCollection<TModel>> GetAsync<TModel>(
		string membershipId, 
		int? skip, 
		int? limit, 
		bool withCount, 
		string? orderBy, 
		SortDirection? sortDirection, 
		CancellationToken cancellationToken = default) 
		where TModel : class, IHasMembership;
}

public interface IMembershipBoundedService<TModel> : IMembershipBoundedService where TModel : IHasMembership
{
	IPaginationCollection<dynamic> Query(
		string membershipId, 
		string query, 
		int? skip = null, 
		int? limit = null,
		bool? withCount = null, 
		string? sortField = null, 
		SortDirection? sortDirection = null,
		IDictionary<string, bool>? selectFields = null);
	
	Task<IPaginationCollection<dynamic>> QueryAsync(
		string membershipId, 
		string query, 
		int? skip = null, 
		int? limit = null,
		bool? withCount = null, 
		string? sortField = null, 
		SortDirection? sortDirection = null,
		IDictionary<string, bool>? selectFields = null, 
		CancellationToken cancellationToken = default);
	
	TModel? Get(string membershipId, string id);
	
	Task<TModel?> GetAsync(string membershipId, string id, CancellationToken cancellationToken = default);
	
	IPaginationCollection<TModel> Get(string membershipId, int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection);
	
	Task<IPaginationCollection<TModel>> GetAsync(
		string membershipId, 
		int? skip, 
		int? limit, 
		bool withCount, 
		string? orderBy, 
		SortDirection? sortDirection, 
		CancellationToken cancellationToken = default);
	
	IPaginationCollection<TModel> Search(
		string membershipId, 
		string keyword, 
		int? skip = null, 
		int? limit = null,
		bool? withCount = null, 
		string? sortField = null, 
		SortDirection? sortDirection = null);
	
	Task<IPaginationCollection<TModel>> SearchAsync(
		string membershipId, 
		string keyword, 
		int? skip = null, 
		int? limit = null,
		bool? withCount = null, 
		string? sortField = null, 
		SortDirection? sortDirection = null, 
		CancellationToken cancellationToken = default);
	
	dynamic Aggregate(string membershipId, string aggregationStagesJson);
	
	Task<dynamic> AggregateAsync(string membershipId, string aggregationStagesJson, CancellationToken cancellationToken = default);
}