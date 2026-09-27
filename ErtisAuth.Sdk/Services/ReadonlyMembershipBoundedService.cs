using Ertis.Core.Collections;
using Ertis.Core.Models.Response;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Core.Models;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Helpers;
using ErtisAuth.Sdk.Services.Interfaces;

// ReSharper disable UnusedType.Global
namespace ErtisAuth.Sdk.Services;

public abstract class ReadonlyMembershipBoundedService<T> : MembershipBoundedService, IReadonlyMembershipBoundedService<T> where T : IHasIdentifier
{
    #region Properties
	
    protected abstract string Slug { get; }
	
    #endregion
    
    #region Constructors
	
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="ertisAuthOptions"></param>
    /// <param name="restHandler"></param>
    protected ReadonlyMembershipBoundedService(IErtisAuthOptions ertisAuthOptions, ISystemRestHandler restHandler) : base(ertisAuthOptions, restHandler)
    {
	    
    }
	
    #endregion
    
    #region Read Methods
	
	public async Task<IResponseResult<T>> GetAsync(string modelId, TokenBase token, CancellationToken cancellationToken = default)
	{
		return await this.ExecuteRequestAsync<T>(
			HttpMethod.Get, 
			$"{this.BaseUrl}/memberships/{this.MembershipId}/{this.Slug}/{modelId}", 
			null, 
			HeaderCollection.Add("Authorization", token.ToString()),
			cancellationToken: cancellationToken).ConfigureAwait(false);
	}
	
	public async Task<IResponseResult<TReturn>> GetAsync<TReturn>(string modelId, TokenBase token, CancellationToken cancellationToken = default) where TReturn : T
	{
		return await this.ExecuteRequestAsync<TReturn>(
			HttpMethod.Get, 
			$"{this.BaseUrl}/memberships/{this.MembershipId}/{this.Slug}/{modelId}", 
			null, 
			HeaderCollection.Add("Authorization", token.ToString()),
			cancellationToken: cancellationToken).ConfigureAwait(false);
	}
	
	public async Task<IResponseResult<IPaginationCollection<T>>> GetAsync(
		TokenBase token,
		int? skip = null,
		int? limit = null,
		bool? withCount = null,
		Sorting? sorting = null,
		string? searchKeyword = null,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(searchKeyword) || string.IsNullOrEmpty(searchKeyword.Trim()))
		{
			return await this.ExecuteRequestAsync<PaginationCollection<T>>(
				HttpMethod.Get,
				$"{this.BaseUrl}/memberships/{this.MembershipId}/{this.Slug}",
				QueryStringHelper.GetQueryString(skip, limit, withCount, sorting),
				HeaderCollection.Add("Authorization", token.ToString()),
				cancellationToken: cancellationToken).ConfigureAwait(false);
		}
		else
		{
			return await this.ExecuteRequestAsync<PaginationCollection<T>>(
				HttpMethod.Get,
				$"{this.BaseUrl}/memberships/{this.MembershipId}/{this.Slug}/search",
				QueryStringHelper.GetQueryString(skip, limit, withCount, sorting).Add("keyword", searchKeyword),
				HeaderCollection.Add("Authorization", token.ToString()),
				cancellationToken: cancellationToken).ConfigureAwait(false);
		}
	}
	
	public async Task<IResponseResult<IPaginationCollection<T>>> GetAsync(
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
			return await this.ExecuteRequestAsync<PaginationCollection<T>>(
				HttpMethod.Get, 
				$"{this.BaseUrl}/memberships/{this.MembershipId}/{this.Slug}", 
				QueryStringHelper.GetQueryString(skip, limit, withCount, orderBy, sortDirection), 
				HeaderCollection.Add("Authorization", token.ToString()),
				cancellationToken: cancellationToken).ConfigureAwait(false);
		}
		else
		{
			return await this.ExecuteRequestAsync<PaginationCollection<T>>(
				HttpMethod.Get, 
				$"{this.BaseUrl}/memberships/{this.MembershipId}/{this.Slug}/search", 
				QueryStringHelper.GetQueryString(skip, limit, withCount, orderBy, sortDirection).Add("keyword", searchKeyword), 
				HeaderCollection.Add("Authorization", token.ToString()),
				cancellationToken: cancellationToken).ConfigureAwait(false);
		}
	}
	
	#endregion
	
	#region Query Methods
	
	public async Task<IResponseResult<IPaginationCollection<T>>> QueryAsync(
		TokenBase token,
		string query,
		int? skip = null,
		int? limit = null,
		bool? withCount = null,
		Sorting? sorting = null,
		CancellationToken cancellationToken = default)
	{
		return await this.ExecuteRequestAsync<PaginationCollection<T>>(
			HttpMethod.Post,
			$"{this.BaseUrl}/memberships/{this.MembershipId}/{this.Slug}/_query",
			QueryStringHelper.GetQueryString(skip, limit, withCount, sorting),
			HeaderCollection.Add("Authorization", token.ToString()),
			new JsonRequestBody(query),
			cancellationToken: cancellationToken).ConfigureAwait(false);
	}
	
	public async Task<IResponseResult<IPaginationCollection<T>>> QueryAsync(
		TokenBase token,
		string query,
		int? skip = null,
		int? limit = null,
		bool? withCount = null,
		string? orderBy = null,
		SortDirection? sortDirection = null, 
		CancellationToken cancellationToken = default)
	{
		return await this.ExecuteRequestAsync<PaginationCollection<T>>(
			HttpMethod.Post, 
			$"{this.BaseUrl}/memberships/{this.MembershipId}/{this.Slug}/_query", 
			QueryStringHelper.GetQueryString(skip, limit, withCount, orderBy, sortDirection), 
			HeaderCollection.Add("Authorization", token.ToString()),
			new JsonRequestBody(query),
			cancellationToken: cancellationToken).ConfigureAwait(false);
	}
	
	#endregion
}