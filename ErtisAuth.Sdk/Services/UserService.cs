using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Core.Models.Response;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Helpers;
using ErtisAuth.Sdk.Services.Interfaces;

namespace ErtisAuth.Sdk.Services
{
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
			Sorting sorting = null,
			CancellationToken cancellationToken = default) where T : class
		{
			return await this.ExecuteRequestAsync<PaginationCollection<T>>(
				HttpMethod.Get,
				$"{this.BaseUrl}/memberships/{this.MembershipId}/{this.Slug}",
				QueryStringHelper.GetQueryString(skip, limit, withCount, sorting),
				HeaderCollection.Add("Authorization", token.ToString()),
				cancellationToken: cancellationToken);
		}
		
		#endregion
		
		#region Active Tokens

		public IResponseResult<IPaginationCollection<ActiveToken>> GetActiveTokens(
			string userId, 
			TokenBase token,
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string orderBy = null, 
			SortDirection? sortDirection = null) =>
			this.GetActiveTokensAsync(userId, token, skip, limit, withCount, orderBy, sortDirection).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<IPaginationCollection<ActiveToken>>> GetActiveTokensAsync(
			string userId, 
			TokenBase token,
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string orderBy = null, 
			SortDirection? sortDirection = null, 
			CancellationToken cancellationToken = default)
		{
			var query = "{ 'where': { 'user_id': '" + userId + "', 'membership_id': '" + this.MembershipId + "' } }";
			return await this.ExecuteRequestAsync<PaginationCollection<ActiveToken>>(
				HttpMethod.Post, 
				$"{this.BaseUrl}/memberships/{this.MembershipId}/active-tokens/_query", 
				QueryStringHelper.GetQueryString(skip, limit, withCount, orderBy, sortDirection), 
				HeaderCollection.Add("Authorization", token.ToString()),
				new JsonRequestBody(Newtonsoft.Json.JsonConvert.DeserializeObject(query)),
				cancellationToken: cancellationToken);
		}

		#endregion
		
		#region Revoked Tokens

		public IResponseResult<IPaginationCollection<RevokedToken>> GetRevokedTokens(
			string userId, 
			TokenBase token,
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string orderBy = null, 
			SortDirection? sortDirection = null) =>
			this.GetRevokedTokensAsync(userId, token, skip, limit, withCount, orderBy, sortDirection).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<IPaginationCollection<RevokedToken>>> GetRevokedTokensAsync(
			string userId, 
			TokenBase token,
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string orderBy = null, 
			SortDirection? sortDirection = null, 
			CancellationToken cancellationToken = default)
		{
			var query = "{ 'where': { 'user_id': '" + userId + "', 'membership_id': '" + this.MembershipId + "', 'token_type': 'bearer_token' } }";
			return await this.ExecuteRequestAsync<PaginationCollection<RevokedToken>>(
				HttpMethod.Post, 
				$"{this.BaseUrl}/memberships/{this.MembershipId}/revoked-tokens/_query", 
				QueryStringHelper.GetQueryString(skip, limit, withCount, orderBy, sortDirection), 
				HeaderCollection.Add("Authorization", token.ToString()),
				new JsonRequestBody(Newtonsoft.Json.JsonConvert.DeserializeObject(query)),
				cancellationToken: cancellationToken);
		}

		#endregion
	}
}