using System.Collections.Generic;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Extensions.AspNetCore.Controllers;
using Ertis.Extensions.AspNetCore.Extensions;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Identity.Attributes;
using ErtisAuth.Extensions.Authorization.Annotations;
using ErtisAuth.WebAPI.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers
{
    [ApiController]
    [Authorized]
    [RbacResource("mailhooks")]
    [Route("api/v{v:apiVersion}/memberships/{membershipId}/[controller]")]
    public class MailHooksController : QueryControllerBase
    {
        #region Services

		private readonly IMailHookService mailHookService;
		private readonly IMembershipService membershipService;
		
		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="mailHookService"></param>
		/// <param name="membershipService"></param>
		public MailHooksController(IMailHookService mailHookService, IMembershipService membershipService)
		{
			this.mailHookService = mailHookService;
			this.membershipService = membershipService;
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
		public async Task<IActionResult> Create([FromRoute] string membershipId, [FromBody] MailHook model)
		{
			var membership = await this.membershipService.GetAsync(membershipId);
			if (membership == null)
			{
				return this.MembershipNotFound(membershipId);
			}

			model.MembershipId = membershipId;
			var utilizer = this.GetUtilizer();
			var mailHook = await this.mailHookService.CreateAsync(utilizer, membershipId, model);
			return this.Created($"{this.Request.Scheme}://{this.Request.Host}{this.Request.Path}/{mailHook.Id}", mailHook);
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
		public async Task<ActionResult<MailHook>> Get([FromRoute] string membershipId, [FromRoute] string id)
		{
			var mailHook = await this.mailHookService.GetAsync(membershipId, id);
			if (mailHook != null)
			{
				return this.Ok(mailHook);
			}
			else
			{
				return this.MailHookNotFound(id);
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
				
			var mailHooks = await this.mailHookService.GetAsync(membershipId, skip, limit, withCount, orderBy, sortDirection);
			return this.Ok(mailHooks);
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
			return await this.mailHookService.QueryAsync(query, skip, limit, withCount, sortField, sortDirection, selectFields);
		}
		
		[HttpGet("search")]
		[RbacAction(Rbac.CrudActions.Read)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<IActionResult> Search([FromRoute] string membershipId, [FromQuery] string keyword)
		{
			if (string.IsNullOrEmpty(keyword) || string.IsNullOrEmpty(keyword.Trim()))
			{
				return this.SearchKeywordRequired();
			}
			
			this.ExtractPaginationParameters(out var skip, out var limit, out var withCount);
			this.ExtractSortingParameters(out var orderBy, out var sortDirection);
			
			return this.Ok(await this.mailHookService.SearchAsync(membershipId, keyword, skip, limit, withCount, orderBy, sortDirection));
		}
		
		#endregion
		
		#region Update Methods

		[HttpPut("{id}")]
		[RbacObject("id")]
		[RbacAction(Rbac.CrudActions.Update)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<IActionResult> Update([FromRoute] string membershipId, [FromRoute] string id, [FromBody] MailHook model)
		{
			model.MembershipId = membershipId;
			var utilizer = this.GetUtilizer();
			var mailHook = await this.mailHookService.UpdateAsync(utilizer, membershipId, model);
			return this.Ok(mailHook);
		}
		
		#endregion

		#region Delete Methods

		[HttpDelete("{id}")]
		[RbacObject("id")]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[RbacAction(Rbac.CrudActions.Delete)]
		public async Task<IActionResult> Delete([FromRoute] string membershipId, [FromRoute] string id)
		{
			var utilizer = this.GetUtilizer();
			if (await this.mailHookService.DeleteAsync(utilizer, membershipId, id))
			{
				return this.NoContent();
			}
			else
			{
				return this.MailHookNotFound(id);
			}
		}
		
		[HttpDelete]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[RbacAction(Rbac.CrudActions.Delete)]
		public async Task<IActionResult> BulkDelete([FromRoute] string membershipId, [FromBody] string[] ids)
		{
			return await this.BulkDeleteAsync(this.mailHookService, membershipId, ids);
		}

		#endregion
    }
}