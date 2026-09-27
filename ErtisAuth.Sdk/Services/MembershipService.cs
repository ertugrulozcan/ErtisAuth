using Ertis.Core.Collections;
using Ertis.Core.Models.Response;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Helpers;
using ErtisAuth.Sdk.Services.Interfaces;

// ReSharper disable UnusedType.Global
namespace ErtisAuth.Sdk.Services;

public class MembershipService : BaseRestService, IMembershipService
{
	#region Properties
	
	// ReSharper disable once MemberCanBePrivate.Global
	protected string? AuthApiBaseUrl { get; }
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="ertisAuthOptions"></param>
	/// <param name="restHandler"></param>
	public MembershipService(IErtisAuthOptions ertisAuthOptions, ISystemRestHandler restHandler) : base(restHandler)
	{
		this.AuthApiBaseUrl = ertisAuthOptions.BaseUrl;
	}
	
	#endregion
	
	#region Create Methods
	
	public async Task<IResponseResult<Membership>> CreateMembershipAsync(Membership membership, TokenBase token, CancellationToken cancellationToken = default)
	{
		return await this.ExecuteRequestAsync<Membership>(
			HttpMethod.Post, 
			$"{this.AuthApiBaseUrl}/memberships", 
			null, 
			HeaderCollection.Add("Authorization", token.ToString()),
			new JsonRequestBody(membership),
			cancellationToken: cancellationToken).ConfigureAwait(false);
	}
	
	#endregion
	
	#region Read Methods
	
	public async Task<IResponseResult<Membership>> GetMembershipAsync(string membershipId, TokenBase token, CancellationToken cancellationToken = default)
	{
		return await this.ExecuteRequestAsync<Membership>(
			HttpMethod.Get, 
			$"{this.AuthApiBaseUrl}/memberships/{membershipId}", 
			null, 
			HeaderCollection.Add("Authorization", token.ToString()), 
			cancellationToken: cancellationToken).ConfigureAwait(false);
	}
	
	public async Task<IResponseResult<IPaginationCollection<Membership>>> GetMembershipsAsync(
		TokenBase token,
		int? skip = null,
		int? limit = null,
		bool? withCount = null,
		string? orderBy = null,
		SortDirection? sortDirection = null,
		string? searchKeyword = null, 
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(searchKeyword) || string.IsNullOrEmpty(searchKeyword.Trim()))
		{
			return await this.ExecuteRequestAsync<PaginationCollection<Membership>>(
				HttpMethod.Get, 
				$"{this.AuthApiBaseUrl}/memberships", 
				QueryStringHelper.GetQueryString(skip, limit, withCount, orderBy, sortDirection), 
				HeaderCollection.Add("Authorization", token.ToString()),
				cancellationToken: cancellationToken).ConfigureAwait(false);	
		}
		else
		{
			return await this.ExecuteRequestAsync<PaginationCollection<Membership>>(
				HttpMethod.Get, 
				$"{this.AuthApiBaseUrl}/memberships/search", 
				QueryStringHelper.GetQueryString(skip, limit, withCount, orderBy, sortDirection).Add("keyword", searchKeyword), 
				HeaderCollection.Add("Authorization", token.ToString()),
				cancellationToken: cancellationToken).ConfigureAwait(false);
		}
	}
	
	#endregion
	
	#region Query Methods
	
	public async Task<IResponseResult<IPaginationCollection<Membership>>> QueryMembershipsAsync(
		TokenBase token,
		string query,
		int? skip = null,
		int? limit = null,
		bool? withCount = null,
		string? orderBy = null,
		SortDirection? sortDirection = null, 
		CancellationToken cancellationToken = default)
	{
		return await this.ExecuteRequestAsync<PaginationCollection<Membership>>(
			HttpMethod.Post, 
			$"{this.AuthApiBaseUrl}/memberships/_query", 
			QueryStringHelper.GetQueryString(skip, limit, withCount, orderBy, sortDirection), 
			HeaderCollection.Add("Authorization", token.ToString()),
			new JsonRequestBody(query),
			cancellationToken: cancellationToken).ConfigureAwait(false);
	}
	
	#endregion
	
	#region Update Methods
	
	public async Task<IResponseResult<Membership>> UpdateMembershipAsync(Membership membership, TokenBase token, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(membership.Id))
		{
			return new ResponseResult<Membership>(false, "Membership id is required!");
		}
		
		return await this.ExecuteRequestAsync<Membership>(
			HttpMethod.Put, 
			$"{this.AuthApiBaseUrl}/memberships/{membership.Id}", 
			null, 
			HeaderCollection.Add("Authorization", token.ToString()),
			new JsonRequestBody(membership),
			cancellationToken: cancellationToken).ConfigureAwait(false);
	}
	
	#endregion
	
	#region Delete Methods
	
	public async Task<IResponseResult> DeleteMembershipAsync(string membershipId, TokenBase token, CancellationToken cancellationToken = default)
	{
		return await this.ExecuteRequestAsync<Membership>(
			HttpMethod.Delete, 
			$"{this.AuthApiBaseUrl}/memberships/{membershipId}", 
			null, 
			HeaderCollection.Add("Authorization", token.ToString()),
			cancellationToken: cancellationToken).ConfigureAwait(false);
	}
	
	#endregion
}