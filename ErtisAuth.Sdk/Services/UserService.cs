using System.Net.Http;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Core.Models.Response;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Services.Interfaces;

namespace ErtisAuth.Sdk.Services
{
	public class UserService : MembershipBoundedService, IUserService
	{
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
		
		#region Create Methods

		public IResponseResult<User> CreateUser(UserWithPassword user, TokenBase token) =>
			this.CreateUserAsync(user, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<User>> CreateUserAsync(UserWithPassword user, TokenBase token)
		{
			return await this.ExecuteRequestAsync<User>(
				HttpMethod.Post, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/users", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()),
				new JsonRequestBody(user));
		}

		#endregion
		
		#region Read Methods

		public IResponseResult<User> GetUser(string userId, TokenBase token) =>
			this.GetUserAsync(userId, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<User>> GetUserAsync(string userId, TokenBase token)
		{
			return await this.ExecuteRequestAsync<User>(
				HttpMethod.Get, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/users/{userId}", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()));
		}

		public IResponseResult<IPaginationCollection<User>> GetUsers(
			TokenBase token, 
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string orderBy = null, 
			SortDirection? sortDirection = null,
			string searchKeyword = null) =>
				this.GetUsersAsync(token, skip, limit, withCount, orderBy, sortDirection, searchKeyword)
					.ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<IPaginationCollection<User>>> GetUsersAsync(
			TokenBase token, 
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string orderBy = null, 
			SortDirection? sortDirection = null,
			string searchKeyword = null)
		{
			return await this.ExecuteRequestAsync<PaginationCollection<User>>(
				HttpMethod.Get, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/users", 
				GetQueryString(skip, limit, withCount, orderBy, sortDirection), 
				HeaderCollection.Add("Authorization", token.ToString()));
		}

		private static IQueryString GetQueryString(
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string orderBy = null, 
			SortDirection? sortDirection = null)
		{
			var queryString = QueryString.Empty;
			if (skip != null)
				queryString.Add("skip", skip.Value);
			if (limit != null)
				queryString.Add("limit", limit.Value);
			if (withCount != null)
				queryString.Add("with_count", withCount.Value.ToString().ToLower());

			if (!string.IsNullOrEmpty(orderBy))
			{
				var sortQueryParam = orderBy;
				if (sortDirection == SortDirection.Descending)
					sortQueryParam += "%20desc";
				
				queryString.Add("sort", sortQueryParam);
			}

			return queryString;
		}
		
		#endregion

		#region Query Methods

		public IResponseResult<IPaginationCollection<User>> QueryUsers(
			TokenBase token, 
			string query, 
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string orderBy = null, 
			SortDirection? sortDirection = null) =>
			this.QueryUsersAsync(
				token,
				query,
				skip,
				limit,
				withCount,
				orderBy,
				sortDirection)
				.ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<IPaginationCollection<User>>> QueryUsersAsync(
			TokenBase token,
			string query,
			int? skip = null,
			int? limit = null,
			bool? withCount = null,
			string orderBy = null,
			SortDirection? sortDirection = null)
		{
			return await this.ExecuteRequestAsync<PaginationCollection<User>>(
				HttpMethod.Post, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/users", 
				GetQueryString(skip, limit, withCount, orderBy, sortDirection), 
				HeaderCollection.Add("Authorization", token.ToString()),
				new JsonRequestBody(Newtonsoft.Json.JsonConvert.DeserializeObject(query)));
		}

		#endregion
		
		#region Update Methods

		public IResponseResult<User> UpdateUser(User user, TokenBase token) =>
			this.UpdateUserAsync(user, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<User>> UpdateUserAsync(User user, TokenBase token)
		{
			if (string.IsNullOrEmpty(user.Id))
			{
				return new ResponseResult<User>(false, "User id is required!");
			}
			
			return await this.ExecuteRequestAsync<User>(
				HttpMethod.Put, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/users/{user.Id}", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()),
				new JsonRequestBody(user));
		}

		#endregion
		
		#region Delete Methods

		public IResponseResult DeleteUser(string userId, TokenBase token) =>
			this.DeleteUserAsync(userId, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult> DeleteUserAsync(string userId, TokenBase token)
		{
			return await this.ExecuteRequestAsync<User>(
				HttpMethod.Delete, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/users/{userId}", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()));
		}

		#endregion
	}
}