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
    [Route("api/webhooks")]
    public class WebhooksController : ControllerBase
    {
        #region Constants

        private readonly string[] Columns = 
        {
            "_id",
            "name",
            "event",
            "status",
            "sys.created_at",
            "sys.created_by",
            "sys.modified_at",
            "sys.modified_by"
        };  

        #endregion
		
        #region Services
		
        private readonly IWebhookService webhookService;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="webhookService"></param>
        public WebhooksController(IWebhookService webhookService)
        {
            this.webhookService = webhookService;
        }

        #endregion
		
        #region Get DataTable

        [HttpGet]
        public async Task<ActionResult<DataTableResponseModel>> GetWebhooksForDataTableAsync()
        {
            try
            {
                this.ExtractPaginationParameters(this.Columns, out var skip, out var limit, out var itemCountPerPage, out var orderBy, out var sortDirection, out var searchKeyword);

                var getWebhooksResponse = await this.webhookService.GetAsync(this.GetBearerToken(), skip, limit, true, orderBy, sortDirection, searchKeyword);
                if (getWebhooksResponse.IsSuccess)
                {
                    var list = Array.Empty<object[]>();
                    if (getWebhooksResponse.Data.Items != null)
                    {
                        list = getWebhooksResponse.Data.Items.Select(x => x.GetDataTableProperties()).ToArray();
                    }
					
                    var table = new DataTableResponseModel
                    {
                        Data = list,
                        TotalCount = getWebhooksResponse.Data.Count,
                        FilteredCount = getWebhooksResponse.Data.Count,
                        ItemCountPerPage = itemCountPerPage ?? Pagination.ITEM_COUNT_PER_PAGE
                    };
					
                    return this.Ok(table);
                }
                else
                {
                    return this.Ok(new DataTableResponseModel
                    {
                        ErrorMessage = getWebhooksResponse.Message
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