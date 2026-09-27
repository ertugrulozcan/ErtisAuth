using Ertis.Core.Collections;
using Ertis.Core.Models.Response;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Sdk.Attributes;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global
namespace ErtisAuth.Sdk.Services.Interfaces;

[ServiceLifetime(ServiceLifetime.Singleton)]
public interface IMembershipService
{
	Task<IResponseResult<Membership>> CreateMembershipAsync(Membership membership, TokenBase token, CancellationToken cancellationToken = default);
	
	Task<IResponseResult<Membership>> GetMembershipAsync(string membershipId, TokenBase token, CancellationToken cancellationToken = default);
	
	Task<IResponseResult<IPaginationCollection<Membership>>> GetMembershipsAsync(TokenBase token, int? skip = null, int? limit = null, bool? withCount = null, string? orderBy = null, SortDirection? sortDirection = null, string? searchKeyword = null, CancellationToken cancellationToken = default);
	
	Task<IResponseResult<IPaginationCollection<Membership>>> QueryMembershipsAsync(TokenBase token, string query, int? skip = null, int? limit = null, bool? withCount = null, string? orderBy = null, SortDirection? sortDirection = null, CancellationToken cancellationToken = default);
	
	Task<IResponseResult<Membership>> UpdateMembershipAsync(Membership membership, TokenBase token, CancellationToken cancellationToken = default);
	
	Task<IResponseResult> DeleteMembershipAsync(string membershipId, TokenBase token, CancellationToken cancellationToken = default);
}