using System.Collections.Generic;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Extensions.AspNetCore.Controllers;
using Ertis.Extensions.AspNetCore.Extensions;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Identity.Attributes;
using ErtisAuth.Extensions.Authorization.Annotations;
using ErtisAuth.WebAPI.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers
{
    [ApiController]
    [Authorized]
    [RbacResource("user-types")]
    [Route("api/v{v:apiVersion}/memberships/{membershipId}/user-types")]
    public class UserTypesController : QueryControllerBase
    {
        #region Services

        private readonly IUserTypeService userTypeService;
		
        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="userTypeService"></param>
        public UserTypesController(IUserTypeService userTypeService)
        {
            this.userTypeService = userTypeService;
        }

        #endregion
        
        #region Read Methods
		
		[HttpGet("{id}")]
		[RbacObject("id")]
		[RbacAction(Rbac.CrudActions.Read)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<ActionResult<UserType>> Get([FromRoute] string membershipId, [FromRoute] string id)
		{
			var userType = await this.userTypeService.GetAsync(membershipId, id);
			if (userType != null)
			{
				return this.Ok(userType);
			}
			else
			{
				return this.UserTypeNotFound(id);
			}
		}
		
		[HttpGet("relations/{id}")]
		[RbacObject("id")]
		[RbacAction(Rbac.CrudActions.Read)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<ActionResult<UserType>> GetFieldInfoOwnerRelations([FromRoute] string membershipId, [FromRoute] string id)
		{
			var relations = await this.userTypeService.GetFieldInfoOwnerRelationsAsync(membershipId, id);
			if (relations != null)
			{
				return this.Ok(relations);
			}
			else
			{
				return this.UserTypeNotFound(id);
			}
		}
		
		[HttpGet]
		[RbacAction(Rbac.CrudActions.Read)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<IActionResult> Get([FromRoute] string membershipId)
		{
			this.ExtractPaginationParameters(out int? skip, out int? limit, out bool withCount);
			this.ExtractSortingParameters(out string orderBy, out SortDirection? sortDirection);
				
			var userTypes = await this.userTypeService.GetAsync(membershipId, skip, limit, withCount, orderBy, sortDirection);
			return this.Ok(userTypes);
		}
		
		[HttpPost("_query")]
		[RbacAction(Rbac.CrudActions.Read)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public override async Task<IActionResult> Query()
		{
			return await base.Query();
		}
		
		protected override async Task<IPaginationCollection<dynamic>> GetDataAsync(string query, int? skip, int? limit, bool? withCount, string sortField, SortDirection? sortDirection, IDictionary<string, bool> selectFields)
		{
			return await this.userTypeService.QueryAsync(query, skip, limit, withCount, sortField, sortDirection, selectFields);
		}
		
		#endregion
		
		#region Create Methods
		
		[HttpPost]
		[RbacAction(Rbac.CrudActions.Create)]
		[ProducesResponseType(StatusCodes.Status201Created)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<IActionResult> Create([FromRoute] string membershipId, [FromBody] UserType model)
		{
			var utilizer = this.GetUtilizer();
			var userType = await this.userTypeService.CreateAsync(utilizer, membershipId, model);
			return this.Created($"{this.Request.Scheme}://{this.Request.Host}{this.Request.Path}/{userType.Id}", userType);
		}
		
		#endregion
		
		#region Update Methods

		[HttpPut("{id}")]
		[RbacObject("id")]
		[RbacAction(Rbac.CrudActions.Update)]
		public async Task<IActionResult> Update([FromRoute] string membershipId, [FromRoute] string id, [FromBody] UserType model)
		{
			model.Id = id;
			var utilizer = this.GetUtilizer();
			var userType = await this.userTypeService.UpdateAsync(utilizer, membershipId, model);
			return this.Ok(userType);
		}

		#endregion
		
		#region Delete Methods

		[HttpDelete("{id}")]
		[RbacObject("id")]
		[RbacAction(Rbac.CrudActions.Delete)]
		public async Task<IActionResult> Delete([FromRoute] string membershipId, [FromRoute] string id)
		{
			var utilizer = this.GetUtilizer();
			if (await this.userTypeService.DeleteAsync(utilizer, membershipId, id))
			{
				return this.NoContent();
			}
			else
			{
				return this.UserTypeNotFound(id);
			}
		}

		#endregion
    }
}