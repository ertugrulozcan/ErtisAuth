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
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        #region Constants

        private readonly string[] Columns = 
        {
            "_id",
            "firstname",
            "lastname",
            "username",
            "email",
            "role",
            "sys.created_at",
            "sys.created_by",
            "sys.modified_at",
            "sys.modified_by",
            "photo_url"
        };  

        #endregion
		
        #region Services
        
        private readonly IUserService userService;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="userService"></param>
        public UsersController(IUserService userService)
        {
            this.userService = userService;
        }

        #endregion
		
        #region Get DataTable

        [HttpGet]
        public async Task<ActionResult<DataTableResponseModel>> GetUsersForDataTableAsync()
        {
            try
            {
                this.ExtractPaginationParameters(this.Columns, out var skip, out var limit, out var itemCountPerPage, out var orderBy, out var sortDirection, out var searchKeyword);

                var getUsersResponse = await this.userService.GetAsync(this.GetBearerToken(), skip, limit, true, orderBy, sortDirection, searchKeyword);
                if (getUsersResponse.IsSuccess)
                {
                    var list = Array.Empty<object[]>();
                    if (getUsersResponse.Data.Items != null)
                    {
                        list = getUsersResponse.Data.Items.Select(x => x.GetDataTableProperties()).ToArray();
                    }
					
                    var table = new DataTableResponseModel
                    {
                        Data = list,
                        TotalCount = getUsersResponse.Data.Count,
                        FilteredCount = getUsersResponse.Data.Count,
                        ItemCountPerPage = itemCountPerPage ?? Pagination.ITEM_COUNT_PER_PAGE
                    };
					
                    return this.Ok(table);
                }
                else
                {
                    return this.Ok(new DataTableResponseModel
                    {
                        ErrorMessage = getUsersResponse.Message
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