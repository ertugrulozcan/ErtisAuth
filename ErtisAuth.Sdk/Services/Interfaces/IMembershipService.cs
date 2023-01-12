using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Core.Models.Response;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Sdk.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace ErtisAuth.Sdk.Services.Interfaces
{
	[ServiceLifetime(ServiceLifetime.Singleton)]
	public interface IMembershipService
	{
		IResponseResult<Membership> CreateMembership(Membership membership, TokenBase token);
		
		Task<IResponseResult<Membership>> CreateMembershipAsync(Membership membership, TokenBase token, CancellationToken cancellationToken = default);
		
		IResponseResult<Membership> GetMembership(string membershipId, TokenBase token);
		
		Task<IResponseResult<Membership>> GetMembershipAsync(string membershipId, TokenBase token, CancellationToken cancellationToken = default);
		
		IResponseResult<IPaginationCollection<Membership>> GetMemberships(TokenBase token, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null, string searchKeyword = null);
		
		Task<IResponseResult<IPaginationCollection<Membership>>> GetMembershipsAsync(TokenBase token, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null, string searchKeyword = null, CancellationToken cancellationToken = default);
		
		IResponseResult<IPaginationCollection<Membership>> QueryMemberships(TokenBase token, string query, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null);
		
		Task<IResponseResult<IPaginationCollection<Membership>>> QueryMembershipsAsync(TokenBase token, string query, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null, CancellationToken cancellationToken = default);
		
		IResponseResult<Membership> UpdateMembership(Membership membership, TokenBase token);
		
		Task<IResponseResult<Membership>> UpdateMembershipAsync(Membership membership, TokenBase token, CancellationToken cancellationToken = default);

		IResponseResult DeleteMembership(string membershipId, TokenBase token);
		
		Task<IResponseResult> DeleteMembershipAsync(string membershipId, TokenBase token, CancellationToken cancellationToken = default);
	}
}