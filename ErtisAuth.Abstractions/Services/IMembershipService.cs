using Ertis.Core.Collections;
using ErtisAuth.Core.Models;
using ErtisAuth.Core.Models.Memberships;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global
namespace ErtisAuth.Abstractions.Services;

public interface IMembershipService : IGenericCrudService<Membership>
{
	void RegisterService<T>(IMembershipBoundedService<T> service) where T : IHasMembership, IHasIdentifier;
	
	Task<IPaginationCollection<dynamic>> QueryAsync(
		string query, 
		int? skip = null, 
		int? limit = null,
		bool? withCount = null, 
		string? sortField = null, 
		SortDirection? sortDirection = null,
		IDictionary<string, bool>? projection = null,
		CancellationToken cancellationToken = default);
	
	Task<Membership?> GetBySecretKeyAsync(string secretKey, CancellationToken cancellationToken = default);
}