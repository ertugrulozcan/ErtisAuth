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
    [Route("api/applications")]
    public class ApplicationsController : ControllerBase
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
		
        private readonly IApplicationService applicationService;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="applicationService"></param>
        public ApplicationsController(IApplicationService applicationService)
        {
            this.applicationService = applicationService;
        }

        #endregion
		
        #region Get DataTable

        [HttpGet]
        public async Task<ActionResult<DataTableResponseModel>> GetApplicationsForDataTableAsync()
        {
            try
            {
                this.ExtractPaginationParameters(this.Columns, out var skip, out var limit, out var itemCountPerPage, out var orderBy, out var sortDirection, out var searchKeyword);

                var getApplicationsResponse = await this.applicationService.GetAsync(this.GetBearerToken(), skip, limit, true, orderBy, sortDirection, searchKeyword);
                if (getApplicationsResponse.IsSuccess)
                {
                    var list = Array.Empty<object[]>();
                    if (getApplicationsResponse.Data.Items != null)
                    {
                        list = getApplicationsResponse.Data.Items.Select(x => x.GetDataTableProperties()).ToArray();
                    }
					
                    var table = new DataTableResponseModel
                    {
                        Data = list,
                        TotalCount = getApplicationsResponse.Data.Count,
                        FilteredCount = getApplicationsResponse.Data.Count,
                        ItemCountPerPage = itemCountPerPage ?? Pagination.ITEM_COUNT_PER_PAGE
                    };
					
                    return this.Ok(table);
                }
                else
                {
                    return this.Ok(new DataTableResponseModel
                    {
                        ErrorMessage = getApplicationsResponse.Message
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