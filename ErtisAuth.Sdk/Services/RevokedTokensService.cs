using System.Net.Http;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Core.Models.Response;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Helpers;
using ErtisAuth.Sdk.Services.Interfaces;

namespace ErtisAuth.Sdk.Services
{
    public class RevokedTokensService : MembershipBoundedService, IRevokedTokensService
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ertisAuthOptions"></param>
        /// <param name="restHandler"></param>
        public RevokedTokensService(IErtisAuthOptions ertisAuthOptions, IRestHandler restHandler) : base(ertisAuthOptions, restHandler)
        {
			
        }

        #endregion
        
        #region Methods

        public IResponseResult<IPaginationCollection<RevokedToken>> GetRevokedTokens(
            TokenBase token, 
            int? skip = null, 
            int? limit = null, 
            bool? withCount = null, 
            string orderBy = null, 
            SortDirection? sortDirection = null) =>
                this.GetRevokedTokensAsync(
                    token, 
                    skip, 
                    limit, 
                    withCount, 
                    orderBy, 
                    sortDirection)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

        public async Task<IResponseResult<IPaginationCollection<RevokedToken>>> GetRevokedTokensAsync(
            TokenBase token,
            int? skip = null,
            int? limit = null,
            bool? withCount = null,
            string orderBy = null,
            SortDirection? sortDirection = null)
        {
            return await this.ExecuteRequestAsync<PaginationCollection<RevokedToken>>(
                HttpMethod.Get, 
                $"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/revoked-tokens", 
                QueryStringHelper.GetQueryString(skip, limit, withCount, orderBy, sortDirection), 
                HeaderCollection.Add("Authorization", token.ToString()));
        }
		
        public IResponseResult<IPaginationCollection<RevokedToken>> QueryRevokedTokens(
            TokenBase token, 
            string query, 
            int? skip = null, 
            int? limit = null, 
            bool? withCount = null, 
            string orderBy = null, 
            SortDirection? sortDirection = null) =>
                this.QueryRevokedTokensAsync(
                    token, 
                    query, 
                    skip, 
                    limit, 
                    withCount, 
                    orderBy, 
                    sortDirection)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

        public async Task<IResponseResult<IPaginationCollection<RevokedToken>>> QueryRevokedTokensAsync(
            TokenBase token,
            string query,
            int? skip = null,
            int? limit = null,
            bool? withCount = null,
            string orderBy = null,
            SortDirection? sortDirection = null)
        {
            return await this.ExecuteRequestAsync<PaginationCollection<RevokedToken>>(
                HttpMethod.Post, 
                $"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/revoked-tokens/_query", 
                QueryStringHelper.GetQueryString(skip, limit, withCount, orderBy, sortDirection), 
                HeaderCollection.Add("Authorization", token.ToString()),
                new JsonRequestBody(Newtonsoft.Json.JsonConvert.DeserializeObject(query)));
        }

        #endregion
    }
}