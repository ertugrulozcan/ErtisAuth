using System.Net.Http;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Core.Models.Response;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Core.Models;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Helpers;
using ErtisAuth.Sdk.Services.Interfaces;

namespace ErtisAuth.Sdk.Services
{
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
	    protected ReadonlyMembershipBoundedService(IErtisAuthOptions ertisAuthOptions, IRestHandler restHandler) : base(ertisAuthOptions, restHandler)
	    {
		    
	    }

	    #endregion
	    
        #region Read Methods

		public IResponseResult<T> Get(string modelId, TokenBase token) =>
			this.GetAsync(modelId, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<T>> GetAsync(string modelId, TokenBase token)
		{
			return await this.ExecuteRequestAsync<T>(
				HttpMethod.Get, 
				$"{this.BaseUrl}/memberships/{this.MembershipId}/{this.Slug}/{modelId}", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()));
		}

		public IResponseResult<IPaginationCollection<T>> Get(
			TokenBase token, 
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string orderBy = null, 
			SortDirection? sortDirection = null,
			string searchKeyword = null) =>
				this.GetAsync(token, skip, limit, withCount, orderBy, sortDirection, searchKeyword)
					.ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<IPaginationCollection<T>>> GetAsync(
			TokenBase token, 
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string orderBy = null, 
			SortDirection? sortDirection = null,
			string searchKeyword = null)
		{
			if (string.IsNullOrEmpty(searchKeyword) || string.IsNullOrEmpty(searchKeyword.Trim()))
			{
				return await this.ExecuteRequestAsync<PaginationCollection<T>>(
					HttpMethod.Get, 
					$"{this.BaseUrl}/memberships/{this.MembershipId}/{this.Slug}", 
					QueryStringHelper.GetQueryString(skip, limit, withCount, orderBy, sortDirection), 
					HeaderCollection.Add("Authorization", token.ToString()));
			}
			else
			{
				return await this.ExecuteRequestAsync<PaginationCollection<T>>(
					HttpMethod.Get, 
					$"{this.BaseUrl}/memberships/{this.MembershipId}/{this.Slug}/search", 
					QueryStringHelper.GetQueryString(skip, limit, withCount, orderBy, sortDirection).Add("keyword", searchKeyword), 
					HeaderCollection.Add("Authorization", token.ToString()));
			}
		}

		#endregion

		#region Query Methods

		public IResponseResult<IPaginationCollection<T>> Query(
			TokenBase token, 
			string query, 
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string orderBy = null, 
			SortDirection? sortDirection = null) =>
			this.QueryAsync(
				token,
				query,
				skip,
				limit,
				withCount,
				orderBy,
				sortDirection)
				.ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<IPaginationCollection<T>>> QueryAsync(
			TokenBase token,
			string query,
			int? skip = null,
			int? limit = null,
			bool? withCount = null,
			string orderBy = null,
			SortDirection? sortDirection = null)
		{
			return await this.ExecuteRequestAsync<PaginationCollection<T>>(
				HttpMethod.Post, 
				$"{this.BaseUrl}/memberships/{this.MembershipId}/{this.Slug}/_query", 
				QueryStringHelper.GetQueryString(skip, limit, withCount, orderBy, sortDirection), 
				HeaderCollection.Add("Authorization", token.ToString()),
				new JsonRequestBody(Newtonsoft.Json.JsonConvert.DeserializeObject(query)));
		}

		#endregion
    }
}