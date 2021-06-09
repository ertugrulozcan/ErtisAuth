using System.Net.Http;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Core.Models.Response;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Helpers;
using ErtisAuth.Sdk.Services.Interfaces;

namespace ErtisAuth.Sdk.Services
{
	public class RoleService : MembershipBoundedService, IRoleService
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ertisAuthOptions"></param>
		/// <param name="restHandler"></param>
		public RoleService(IErtisAuthOptions ertisAuthOptions, IRestHandler restHandler) : base(ertisAuthOptions, restHandler)
		{
			
		}

		#endregion
		
		#region Methods

		public bool CheckPermission(string rbac, TokenBase token) => this.CheckPermissionAsync(rbac, token).ConfigureAwait(false).GetAwaiter().GetResult();
		
		public async Task<bool> CheckPermissionAsync(string rbac, TokenBase token)
		{
			var url = $"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/roles/check-permission";
			var queryString = QueryString.Add("permission", rbac);
			var headers = HeaderCollection.Add("Authorization", token.ToString());
			var response = await this.ExecuteRequestAsync(HttpMethod.Get, url, queryString, headers);
			return response.IsSuccess;
		}

		#endregion
		
		#region Create Methods
		
		public IResponseResult<Role> CreateRole(Role role, TokenBase token) =>
			this.CreateRoleAsync(role, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<Role>> CreateRoleAsync(Role role, TokenBase token)
		{
			return await this.ExecuteRequestAsync<Role>(
				HttpMethod.Post, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/roles", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()),
				new JsonRequestBody(role));
		}
		
		#endregion
		
		#region Read Methods
		
		public IResponseResult<Role> GetRole(string roleId, TokenBase token) =>
			this.GetRoleAsync(roleId, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<Role>> GetRoleAsync(string roleId, TokenBase token)
		{
			return await this.ExecuteRequestAsync<Role>(
				HttpMethod.Get, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/roles/{roleId}", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()));
		}
		
		public IResponseResult<IPaginationCollection<Role>> GetRoles(
			TokenBase token, 
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string orderBy = null, 
			SortDirection? sortDirection = null, 
			string searchKeyword = null) =>
			this.GetRolesAsync(
				token,
				skip,
				limit,
				withCount,
				orderBy,
				sortDirection,
				searchKeyword)
				.ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<IPaginationCollection<Role>>> GetRolesAsync(
			TokenBase token,
			int? skip = null,
			int? limit = null,
			bool? withCount = null,
			string orderBy = null,
			SortDirection? sortDirection = null,
			string searchKeyword = null)
		{
			return await this.ExecuteRequestAsync<PaginationCollection<Role>>(
				HttpMethod.Get, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/roles", 
				QueryStringHelper.GetQueryString(skip, limit, withCount, orderBy, sortDirection), 
				HeaderCollection.Add("Authorization", token.ToString()));
		}
		
		#endregion
		
		#region Query Methods
		
		public IResponseResult<IPaginationCollection<Role>> QueryRoles(
			TokenBase token, 
			string query, 
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string orderBy = null, 
			SortDirection? sortDirection = null) =>
			this.QueryRolesAsync(
				token,
				query,
				skip,
				limit,
				withCount,
				orderBy,
				sortDirection)
				.ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<IPaginationCollection<Role>>> QueryRolesAsync(
			TokenBase token,
			string query,
			int? skip = null,
			int? limit = null,
			bool? withCount = null,
			string orderBy = null,
			SortDirection? sortDirection = null)
		{
			return await this.ExecuteRequestAsync<PaginationCollection<Role>>(
				HttpMethod.Post, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/roles/_query", 
				QueryStringHelper.GetQueryString(skip, limit, withCount, orderBy, sortDirection), 
				HeaderCollection.Add("Authorization", token.ToString()),
				new JsonRequestBody(Newtonsoft.Json.JsonConvert.DeserializeObject(query)));
		}
		
		#endregion
		
		#region Update Methods
		
		public IResponseResult<Role> UpdateRole(Role role, TokenBase token) =>
			this.UpdateRoleAsync(role, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<Role>> UpdateRoleAsync(Role role, TokenBase token)
		{
			if (string.IsNullOrEmpty(role.Id))
			{
				return new ResponseResult<Role>(false, "Role id is required!");
			}
			
			return await this.ExecuteRequestAsync<Role>(
				HttpMethod.Put, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/roles/{role.Id}", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()),
				new JsonRequestBody(role));
		}
		
		#endregion
		
		#region Delete Methods
		
		public IResponseResult DeleteRole(string roleId, TokenBase token) =>
			this.DeleteRoleAsync(roleId, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult> DeleteRoleAsync(string roleId, TokenBase token)
		{
			return await this.ExecuteRequestAsync<Role>(
				HttpMethod.Delete, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/roles/{roleId}", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()));
		}
		
		#endregion
	}
}