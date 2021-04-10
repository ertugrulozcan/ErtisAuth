using System.Net.Http;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Core.Models.Response;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Helpers;
using ErtisAuth.Sdk.Services.Interfaces;

namespace ErtisAuth.Sdk.Services
{
	public class MembershipService : BaseRestService, IMembershipService
	{
		#region Properties

		protected string AuthApiBaseUrl { get; }
		
		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ertisAuthOptions"></param>
		/// <param name="restHandler"></param>
		public MembershipService(IErtisAuthOptions ertisAuthOptions, IRestHandler restHandler) : base(restHandler)
		{
			this.AuthApiBaseUrl = ertisAuthOptions.BaseUrl;
		}

		#endregion
		
		#region Read Methods
		
		public IResponseResult<Membership> GetMembership(string membershipId, TokenBase token) =>
			this.GetMembershipAsync(membershipId, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<Membership>> GetMembershipAsync(string membershipId, TokenBase token)
		{
			return await this.ExecuteRequestAsync<Membership>(
				HttpMethod.Get, 
				$"{this.AuthApiBaseUrl}/memberships/{membershipId}", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()));
		}
		
		public IResponseResult<IPaginationCollection<Membership>> GetMemberships(
			TokenBase token, 
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string orderBy = null, 
			SortDirection? sortDirection = null, 
			string searchKeyword = null) =>
			this.GetMembershipsAsync(
				token,
				skip,
				limit,
				withCount,
				orderBy,
				sortDirection,
				searchKeyword)
				.ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<IPaginationCollection<Membership>>> GetMembershipsAsync(
			TokenBase token,
			int? skip = null,
			int? limit = null,
			bool? withCount = null,
			string orderBy = null,
			SortDirection? sortDirection = null,
			string searchKeyword = null)
		{
			return await this.ExecuteRequestAsync<PaginationCollection<Membership>>(
				HttpMethod.Get, 
				$"{this.AuthApiBaseUrl}/memberships", 
				QueryStringHelper.GetQueryString(skip, limit, withCount, orderBy, sortDirection), 
				HeaderCollection.Add("Authorization", token.ToString()));
		}
		
		#endregion
		
		#region Query Methods
		
		public IResponseResult<IPaginationCollection<Membership>> QueryMemberships(
			TokenBase token, 
			string query, 
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string orderBy = null, 
			SortDirection? sortDirection = null) =>
			this.QueryMembershipsAsync(
				token,
				query,
				skip,
				limit,
				withCount,
				orderBy,
				sortDirection)
				.ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<IPaginationCollection<Membership>>> QueryMembershipsAsync(
			TokenBase token,
			string query,
			int? skip = null,
			int? limit = null,
			bool? withCount = null,
			string orderBy = null,
			SortDirection? sortDirection = null)
		{
			return await this.ExecuteRequestAsync<PaginationCollection<Membership>>(
				HttpMethod.Post, 
				$"{this.AuthApiBaseUrl}/memberships", 
				QueryStringHelper.GetQueryString(skip, limit, withCount, orderBy, sortDirection), 
				HeaderCollection.Add("Authorization", token.ToString()),
				new JsonRequestBody(Newtonsoft.Json.JsonConvert.DeserializeObject(query)));
		}
		
		#endregion
		
		#region Update Methods
		
		public IResponseResult<Membership> UpdateMembership(Membership membership, TokenBase token) =>
			this.UpdateMembershipAsync(membership, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<Membership>> UpdateMembershipAsync(Membership membership, TokenBase token)
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
				new JsonRequestBody(membership));
		}
		
		#endregion
	}
}