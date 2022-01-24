using System;
using System.Linq;
using System.Threading.Tasks;
using ErtisAuth.Hub.Constants;
using ErtisAuth.Hub.Extensions;
using ErtisAuth.Hub.Models.DataTables;
using Ertis.MongoDB.Queries;
using ErtisAuth.Extensions.Authorization.Annotations;
using ErtisAuth.Sdk.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.Hub.Controllers.API
{
    [Authorized]
    [Route("api/sessions")]
    public class SessionsController : ControllerBase
    {
        #region Constants

        private readonly string[] Columns = 
        {
            "_id",
            "name",
            "role",
            "sys.created_at",
            "sys.created_by",
            "sys.modified_at",
            "sys.modified_by"
        };  

        #endregion
        
        #region Services
		
        private readonly IMembershipService membershipService;
        private readonly IActiveTokensService activeTokensService;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="membershipService"></param>
        /// <param name="activeTokensService"></param>
        public SessionsController(IMembershipService membershipService, IActiveTokensService activeTokensService)
        {
            this.membershipService = membershipService;
            this.activeTokensService = activeTokensService;
        }

        #endregion
		
        #region Get DataTable

        [HttpGet]
        public async Task<ActionResult<DataTableResponseModel>> GetApplicationsForDataTableAsync()
        {
            try
            {
                this.ExtractPaginationParameters(this.Columns, out var skip, out var limit, out var itemCountPerPage, out var orderBy, out var sortDirection, out var _);

                var token = this.GetBearerToken();
                var membershipId = this.GetClaim(Claims.MembershipId);
                var membership = await this.membershipService.GetMembershipAsync(membershipId, token);
                var tokenExpireTime = TimeSpan.FromSeconds(membership.Data.ExpiresIn);
                var query = QueryBuilder.GreaterThanOrEqual("created_at", DateTime.Now.Subtract(tokenExpireTime));
                var getActiveTokensResponse = await this.activeTokensService.QueryAsync(token, query.ToString(), skip, limit, true, orderBy, sortDirection);
                if (getActiveTokensResponse.IsSuccess)
                {
                    var list = Array.Empty<object[]>();
                    if (getActiveTokensResponse.Data.Items != null)
                    {
                        list = getActiveTokensResponse.Data.Items.Select(x => x.GetDataTableProperties(x.AccessToken == token.AccessToken)).ToArray();
                    }
					
                    var table = new DataTableResponseModel
                    {
                        Data = list,
                        TotalCount = getActiveTokensResponse.Data.Count,
                        FilteredCount = getActiveTokensResponse.Data.Count,
                        ItemCountPerPage = itemCountPerPage ?? Pagination.ITEM_COUNT_PER_PAGE
                    };
					
                    return this.Ok(table);
                }
                else
                {
                    return this.Ok(new DataTableResponseModel
                    {
                        ErrorMessage = getActiveTokensResponse.Message
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return this.StatusCode(500, ex.Message);
            }
        }

        #endregion
    }
}