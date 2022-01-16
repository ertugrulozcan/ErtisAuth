using System;
using System.Linq;
using System.Threading.Tasks;
using ErtisAuth.Extensions.Authorization.Annotations;
using ErtisAuth.Sdk.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using ErtisAuth.Hub.Constants;
using ErtisAuth.Hub.Extensions;
using ErtisAuth.Hub.Models.DataTables;

namespace ErtisAuth.Hub.Controllers.API
{
    [Authorized]
    [Route("api/memberships")]
    public class MembershipsController : ControllerBase
    {
        #region Constants

        private readonly string[] Columns = 
        {
            "_id",
            "name",
            "sys.created_at",
            "sys.created_by",
            "sys.modified_at",
            "sys.modified_by"
        };  

        #endregion
		
        #region Services
		
        private readonly IMembershipService membershipService;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="membershipService"></param>
        public MembershipsController(IMembershipService membershipService)
        {
            this.membershipService = membershipService;
        }

        #endregion
		
        #region Get DataTable

        [HttpGet]
        public async Task<ActionResult<DataTableResponseModel>> GetMembershipsForDataTableAsync()
        {
            try
            {
                this.ExtractPaginationParameters(this.Columns, out var skip, out var limit, out var itemCountPerPage, out var orderBy, out var sortDirection, out var searchKeyword);

                var getMembershipsResponse = await this.membershipService.GetMembershipsAsync(this.GetBearerToken(), skip, limit, true, orderBy, sortDirection, searchKeyword);
                if (getMembershipsResponse.IsSuccess)
                {
                    var list = Array.Empty<object[]>();
                    if (getMembershipsResponse.Data.Items != null)
                    {
                        list = getMembershipsResponse.Data.Items.Select(x => x.GetDataTableProperties()).ToArray();
                    }
					
                    var table = new DataTableResponseModel
                    {
                        Data = list,
                        TotalCount = getMembershipsResponse.Data.Count,
                        FilteredCount = getMembershipsResponse.Data.Count,
                        ItemCountPerPage = itemCountPerPage ?? Pagination.ITEM_COUNT_PER_PAGE
                    };
					
                    return this.Ok(table);
                }
                else
                {
                    return this.Ok(new DataTableResponseModel
                    {
                        ErrorMessage = getMembershipsResponse.Message
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