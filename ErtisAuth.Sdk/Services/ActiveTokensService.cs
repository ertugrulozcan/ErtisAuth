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
    public class ActiveTokensService : MembershipBoundedService, IActiveTokensService
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ertisAuthOptions"></param>
        /// <param name="restHandler"></param>
        public ActiveTokensService(IErtisAuthOptions ertisAuthOptions, IRestHandler restHandler) : base(ertisAuthOptions, restHandler)
        {
			
        }

        #endregion
        
        #region Methods

        public IResponseResult<IPaginationCollection<ActiveToken>> GetActiveTokens(
            TokenBase token, 
            int? skip = null, 
            int? limit = null, 
            bool? withCount = null, 
            string orderBy = null, 
            SortDirection? sortDirection = null) =>
                this.GetActiveTokensAsync(
                    token, 
                    skip, 
                    limit, 
                    withCount, 
                    orderBy, 
                    sortDirection)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

        public async Task<IResponseResult<IPaginationCollection<ActiveToken>>> GetActiveTokensAsync(
            TokenBase token,
            int? skip = null,
            int? limit = null,
            bool? withCount = null,
            string orderBy = null,
            SortDirection? sortDirection = null)
        {
            return await this.ExecuteRequestAsync<PaginationCollection<ActiveToken>>(
                HttpMethod.Get, 
                $"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/active-tokens", 
                QueryStringHelper.GetQueryString(skip, limit, withCount, orderBy, sortDirection), 
                HeaderCollection.Add("Authorization", token.ToString()));
        }
		
        public IResponseResult<IPaginationCollection<ActiveToken>> QueryActiveTokens(
            TokenBase token, 
            string query, 
            int? skip = null, 
            int? limit = null, 
            bool? withCount = null, 
            string orderBy = null, 
            SortDirection? sortDirection = null) =>
                this.QueryActiveTokensAsync(
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

        public async Task<IResponseResult<IPaginationCollection<ActiveToken>>> QueryActiveTokensAsync(
            TokenBase token,
            string query,
            int? skip = null,
            int? limit = null,
            bool? withCount = null,
            string orderBy = null,
            SortDirection? sortDirection = null)
        {
            return await this.ExecuteRequestAsync<PaginationCollection<ActiveToken>>(
                HttpMethod.Post, 
                $"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/active-tokens/_query", 
                QueryStringHelper.GetQueryString(skip, limit, withCount, orderBy, sortDirection), 
                HeaderCollection.Add("Authorization", token.ToString()),
                new JsonRequestBody(Newtonsoft.Json.JsonConvert.DeserializeObject(query)));
        }

        #endregion
    }
}