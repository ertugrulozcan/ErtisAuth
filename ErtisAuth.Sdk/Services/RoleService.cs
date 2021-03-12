using System.Net.Http;
using System.Threading.Tasks;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Sdk.Configuration;
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
	}
}