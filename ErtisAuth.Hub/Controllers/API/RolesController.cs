using System;
using System.Linq;
using System.Threading.Tasks;
using ErtisAuth.Extensions.Authorization.Annotations;
using ErtisAuth.Sdk.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using ErtisAuth.Hub.Constants;
using ErtisAuth.Hub.Extensions;
using ErtisAuth.Hub.Models.DataTables;
using ErtisAuth.Core.Models.Roles;

namespace ErtisAuth.Hub.Controllers.API
{
    [Authorized]
    [Route("api/roles")]
    public class RolesController : ControllerBase
    {
        #region Constants

        private readonly string[] Columns = 
        {
            "_id",
            "name",
            "description",
            "sys.created_at",
            "sys.created_by",
            "sys.modified_at",
            "sys.modified_by"
        };  

        #endregion
		
        #region Services
		
        private readonly IRoleService roleService;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="roleService"></param>
        public RolesController(IRoleService roleService)
        {
            this.roleService = roleService;
        }

        #endregion

        #region Get Role

        [HttpGet("{id}")]
        public async Task<ActionResult<Role>> GetRole([FromRoute] string id)
        {
            return this.Ok(await this.roleService.GetAsync(id, this.GetBearerToken()));
        }

        #endregion
		
        #region Get DataTable

        [HttpGet]
        public async Task<ActionResult<DataTableResponseModel>> GetRolesForDataTableAsync()
        {
            try
            {
                this.ExtractPaginationParameters(this.Columns, out var skip, out var limit, out var itemCountPerPage, out var orderBy, out var sortDirection, out var searchKeyword);

                var getRolesResponse = await this.roleService.GetAsync(this.GetBearerToken(), skip, limit, true, orderBy, sortDirection, searchKeyword);
                if (getRolesResponse.IsSuccess)
                {
                    var list = Array.Empty<object[]>();
                    if (getRolesResponse.Data.Items != null)
                    {
                        list = getRolesResponse.Data.Items.Select(x => x.GetDataTableProperties()).ToArray();
                    }
					
                    var table = new DataTableResponseModel
                    {
                        Data = list,
                        TotalCount = getRolesResponse.Data.Count,
                        FilteredCount = getRolesResponse.Data.Count,
                        ItemCountPerPage = itemCountPerPage ?? Pagination.ITEM_COUNT_PER_PAGE
                    };
					
                    return this.Ok(table);
                }
                else
                {
                    return this.Ok(new DataTableResponseModel
                    {
                        ErrorMessage = getRolesResponse.Message
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