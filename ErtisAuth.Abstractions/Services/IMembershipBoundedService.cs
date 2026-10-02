using Ertis.Core.Collections;
using ErtisAuth.Core.Models;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedParameter.Global
// ReSharper disable UnusedMemberInSuper.Global
namespace ErtisAuth.Abstractions.Services;

public interface IMembershipBoundedService
{
	Task<TModel?> GetAsync<TModel>(string id, string membershipId, CancellationToken cancellationToken = default) where TModel : class, IHasMembership;
	
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
	Task<IPaginationCollection<dynamic>> QueryAsync(
		string query,
		string membershipId, 
		int? skip = null, 
		int? limit = null,
		bool? withCount = null, 
		string? sortField = null, 
		SortDirection? sortDirection = null,
		IDictionary<string, bool>? projection = null, 
		CancellationToken cancellationToken = default);
	
	Task<TModel?> GetAsync(string id, string membershipId, CancellationToken cancellationToken = default);
	
	Task<IPaginationCollection<TModel>> GetAsync(
		string membershipId, 
		int? skip, 
		int? limit, 
		bool withCount, 
		string? orderBy, 
		SortDirection? sortDirection, 
		CancellationToken cancellationToken = default);
	
	Task<IPaginationCollection<TModel>> SearchAsync(
		string keyword, 
		string membershipId, 
		int? skip = null, 
		int? limit = null,
		bool? withCount = null, 
		string? sortField = null, 
		SortDirection? sortDirection = null, 
		CancellationToken cancellationToken = default);
	
	Task<dynamic> AggregateAsync(string aggregationStagesJson, string membershipId, CancellationToken cancellationToken = default);
}