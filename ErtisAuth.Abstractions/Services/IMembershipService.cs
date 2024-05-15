using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using ErtisAuth.Core.Models;
using ErtisAuth.Core.Models.Memberships;

namespace ErtisAuth.Abstractions.Services
{
	public interface IMembershipService : IGenericCrudService<Membership>
	{
		void RegisterService<T>(IMembershipBoundedService<T> service) where T : IHasMembership, IHasIdentifier;
		
		Task<IPaginationCollection<dynamic>> QueryAsync(
			string query, 
			int? skip = null, 
			int? limit = null,
			bool? withCount = null, 
			string sortField = null, 
			SortDirection? sortDirection = null,
			IDictionary<string, bool> selectFields = null,
			CancellationToken cancellationToken = default);

		Membership GetBySecretKey(string secretKey);
		
		Task<Membership> GetBySecretKeyAsync(string secretKey, CancellationToken cancellationToken = default);
	}
}