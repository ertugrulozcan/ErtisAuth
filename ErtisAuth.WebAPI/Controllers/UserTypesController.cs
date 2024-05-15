using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Extensions.AspNetCore.Controllers;
using Ertis.Extensions.AspNetCore.Extensions;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
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
		[RbacObject("{id}")]
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
		[RbacObject("{id}")]
		[RbacAction(Rbac.CrudActions.Read)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<ActionResult<UserType>> GetFieldInfoOwnerRelations([FromRoute] string membershipId, [FromRoute] string id, CancellationToken cancellationToken = default)
		{
			var relations = await this.userTypeService.GetFieldInfoOwnerRelationsAsync(membershipId, id, cancellationToken: cancellationToken);
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
		public async Task<IActionResult> Get([FromRoute] string membershipId, CancellationToken cancellationToken = default)
		{
			this.ExtractPaginationParameters(out int? skip, out int? limit, out bool withCount);
			this.ExtractSortingParameters(out string orderBy, out SortDirection? sortDirection);
				
			var userTypes = await this.userTypeService.GetAsync(membershipId, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
			return this.Ok(userTypes);
		}
		
		[HttpGet("all")]
		[RbacAction(Rbac.CrudActions.Read)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<IActionResult> GetAll([FromRoute] string membershipId, CancellationToken cancellationToken = default)
		{
			var userTypes = await this.userTypeService.GetAsync(membershipId, null, null, false, null, null, cancellationToken: cancellationToken);
			var allUserTypes = new List<UserType>();
			allUserTypes.AddRange(userTypes.Items);
			
			var originUserType = await this.userTypeService.GetByNameOrSlugAsync(membershipId, UserType.ORIGIN_USER_TYPE_SLUG, cancellationToken: cancellationToken);
			if (originUserType != null)
			{
				allUserTypes.Add(originUserType);	
			}
			 
			userTypes = new PaginationCollection<UserType>
			{
				Items = allUserTypes,
				Count = allUserTypes.Count
			};
			
			return this.Ok(userTypes);
		}
		
		[HttpPost("_query")]
		[RbacAction(Rbac.CrudActions.Read)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public override async Task<IActionResult> Query(CancellationToken cancellationToken = default)
		{
			return await base.Query(cancellationToken: cancellationToken);
		}
		
		protected override async Task<IPaginationCollection<dynamic>> GetDataAsync(string query, int? skip, int? limit, bool? withCount, string sortField, SortDirection? sortDirection, IDictionary<string, bool> selectFields, CancellationToken cancellationToken = default)
		{
			if (this.Request.RouteValues.TryGetValue("membershipId", out var membershipId) && membershipId is string)
			{
				return await this.userTypeService.QueryAsync(membershipId.ToString(), query, skip, limit, withCount, sortField, sortDirection, selectFields, cancellationToken: cancellationToken);	
			}
			else
			{
				throw ErtisAuthException.MembershipIdRequired();
			}
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
		public async Task<IActionResult> Create([FromRoute] string membershipId, [FromBody] UserType model, CancellationToken cancellationToken = default)
		{
			var utilizer = this.GetUtilizer();
			var userType = await this.userTypeService.CreateAsync(utilizer, membershipId, model, cancellationToken: cancellationToken);
			return this.Created($"{this.Request.Scheme}://{this.Request.Host}{this.Request.Path}/{userType.Id}", userType);
		}
		
		#endregion
		
		#region Update Methods

		[HttpPut("{id}")]
		[RbacObject("{id}")]
		[RbacAction(Rbac.CrudActions.Update)]
		public async Task<IActionResult> Update([FromRoute] string membershipId, [FromRoute] string id, [FromBody] UserType model, CancellationToken cancellationToken = default)
		{
			model.Id = id;
			var utilizer = this.GetUtilizer();
			var userType = await this.userTypeService.UpdateAsync(utilizer, membershipId, model, cancellationToken: cancellationToken);
			return this.Ok(userType);
		}

		#endregion
		
		#region Delete Methods

		[HttpDelete("{id}")]
		[RbacObject("{id}")]
		[RbacAction(Rbac.CrudActions.Delete)]
		public async Task<IActionResult> Delete([FromRoute] string membershipId, [FromRoute] string id, CancellationToken cancellationToken = default)
		{
			var utilizer = this.GetUtilizer();
			if (await this.userTypeService.DeleteAsync(utilizer, membershipId, id, cancellationToken: cancellationToken))
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