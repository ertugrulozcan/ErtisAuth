using System.Text.Json.Nodes;
using Ertis.Core.Collections;
using Ertis.Core.Models;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Helpers;
using ErtisAuth.Sdk.Services.Interfaces;

// ReSharper disable UnusedType.Global
namespace ErtisAuth.Sdk.Services;

public class UserService : MembershipBoundedService<User>, IUserService
{
	#region Properties
	
	protected override string Slug => "users";	
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="ertisAuthOptions"></param>
	/// <param name="restHandler"></param>
	public UserService(IErtisAuthOptions ertisAuthOptions, IRestHandler restHandler) : base(ertisAuthOptions, restHandler)
	{
		
	}
	
	#endregion
	
	#region Read Methods
	
	public async Task<IResponseResult<IPaginationCollection<T>>> GetAsync<T>(
		TokenBase token, 
		int? skip = null,
		int? limit = null, 
		bool? withCount = null, 
		Sorting? sorting = null,
		string? searchKeyword = null, 
		CancellationToken cancellationToken = default) where T : class
	{
		var queryString = QueryStringHelper.GetQueryString(skip, limit, withCount, sorting);
		if (!string.IsNullOrWhiteSpace(searchKeyword))
		{
			queryString = queryString.Add("keyword", searchKeyword);
		}
		
		return await this.ExecuteRequestAsync<PaginationCollection<T>>(
			HttpMethod.Get,
			$"{this.BaseUrl}/memberships/{this.MembershipId}/{this.Slug}",
			queryString,
			HeaderCollection.Add("Authorization", token.ToString()),
			cancellationToken: cancellationToken).ConfigureAwait(false);
	}
	
	public async Task<IResponseResult<IPaginationCollection<T>>> QueryAsync<T>(
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
	
	#endregion
	
	#region Active Tokens
	
	public async Task<IResponseResult<IPaginationCollection<ActiveToken>>> GetActiveTokensAsync(
		string userId, 
		TokenBase token,
		int? skip = null, 
		int? limit = null, 
		bool? withCount = null, 
		string? orderBy = null, 
		SortDirection? sortDirection = null, 
		CancellationToken cancellationToken = default)
	{
		var query = this.UserTokensQuery(userId);
		return await this.ExecuteRequestAsync<PaginationCollection<ActiveToken>>(
			HttpMethod.Post, 
			$"{this.BaseUrl}/memberships/{this.MembershipId}/active-tokens/_query", 
			QueryStringHelper.GetQueryString(skip, limit, withCount, orderBy, sortDirection), 
			HeaderCollection.Add("Authorization", token.ToString()),
			new JsonRequestBody(query),
			cancellationToken: cancellationToken).ConfigureAwait(false);
	}
	
	/// <summary>
	/// Built as a json document: a valid json (single quotes are not) whose values can't change the query
	/// </summary>
	private string UserTokensQuery(string userId, string? tokenType = null)
	{
		var where = new JsonObject
		{
			["user_id"] = userId,
			["membership_id"] = this.MembershipId
		};
		
		if (tokenType != null)
		{
			where["token_type"] = tokenType;
		}
		
		return new JsonObject { ["where"] = where }.ToJsonString();
	}
	
	#endregion
	
	#region Revoked Tokens
	
	public async Task<IResponseResult<IPaginationCollection<RevokedToken>>> GetRevokedTokensAsync(
		string userId, 
		TokenBase token,
		int? skip = null, 
		int? limit = null, 
		bool? withCount = null, 
		string? orderBy = null, 
		SortDirection? sortDirection = null, 
		CancellationToken cancellationToken = default)
	{
		var query = this.UserTokensQuery(userId, tokenType: "bearer_token");
		return await this.ExecuteRequestAsync<PaginationCollection<RevokedToken>>(
			HttpMethod.Post, 
			$"{this.BaseUrl}/memberships/{this.MembershipId}/revoked-tokens/_query", 
			QueryStringHelper.GetQueryString(skip, limit, withCount, orderBy, sortDirection), 
			HeaderCollection.Add("Authorization", token.ToString()),
			new JsonRequestBody(query),
			cancellationToken: cancellationToken).ConfigureAwait(false);
	}
	
	#endregion
}