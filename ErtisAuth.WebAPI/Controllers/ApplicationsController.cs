using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Extensions.AspNetCore.Controllers;
using Ertis.Extensions.AspNetCore.Extensions;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Extensions.Authorization.Annotations;
using ErtisAuth.Identity.Attributes;
using ErtisAuth.WebAPI.Extensions;
using ErtisAuth.WebAPI.Models.Request.Applications;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers
{
	[ApiController]
	[Authorized]
	[RbacResource("applications")]
	[Route("api/v{v:apiVersion}/memberships/{membershipId}/[controller]")]
	public class ApplicationsController : QueryControllerBase
	{
		#region Services

		private readonly IApplicationService applicationService;
		private readonly IMembershipService membershipService;
		
		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="applicationService"></param>
		/// <param name="membershipService"></param>
		public ApplicationsController(IApplicationService applicationService, IMembershipService membershipService)
		{
			this.applicationService = applicationService;
			this.membershipService = membershipService;
		}

		#endregion
		
		#region Create Methods
		
		[HttpPost]
		[RbacAction(Rbac.CrudActions.Create)]
		public async Task<IActionResult> Create([FromRoute] string membershipId, [FromBody] CreateApplicationFormModel model, CancellationToken cancellationToken = default)
		{
			var membership = await this.membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
			if (membership == null)
			{
				return this.MembershipNotFound(membershipId);
			}
			
			var applicationModel = new Application
			{ 
				Name = model.Name, 
				Slug = model.Slug,
				Role = model.Role,
				MembershipId = membershipId
			};
			
			var utilizer = this.GetUtilizer();
			var app = await this.applicationService.CreateAsync(utilizer, membershipId, applicationModel, cancellationToken: cancellationToken);
			
			return this.Created($"{this.Request.Scheme}://{this.Request.Host}{this.Request.Path}/{app.Id}", app);
		}
		
		#endregion
		
		#region Read Methods
		
		[HttpGet("{id}")]
		[RbacObject("{id}")]
		[RbacAction(Rbac.CrudActions.Read)]
		public async Task<ActionResult<Application>> Get([FromRoute] string membershipId, [FromRoute] string id)
		{
			var app = await this.applicationService.GetAsync(membershipId, id);
			if (app != null)
			{
				return this.Ok(app);
			}
			else
			{
				return this.ApplicationNotFound(id);
			}
		}
		
		[HttpGet]
		[RbacAction(Rbac.CrudActions.Read)]
		public async Task<IActionResult> Get([FromRoute] string membershipId, CancellationToken cancellationToken = default)
		{
			this.ExtractPaginationParameters(out int? skip, out int? limit, out bool withCount);
			this.ExtractSortingParameters(out string orderBy, out SortDirection? sortDirection);
				
			var apps = await this.applicationService.GetAsync(membershipId, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
			return this.Ok(apps);
		}
		
		[HttpPost("_query")]
		[RbacAction(Rbac.CrudActions.Read)]
		public override async Task<IActionResult> Query(CancellationToken cancellationToken = default)
		{
			return await base.Query(cancellationToken: cancellationToken);
		}
		
		protected override async Task<IPaginationCollection<dynamic>> GetDataAsync(string query, int? skip, int? limit, bool? withCount, string sortField, SortDirection? sortDirection, IDictionary<string, bool> selectFields, CancellationToken cancellationToken = default)
		{
			if (this.Request.RouteValues.TryGetValue("membershipId", out var membershipId) && membershipId is string)
			{
				return await this.applicationService.QueryAsync(membershipId.ToString(), query, skip, limit, withCount, sortField, sortDirection, selectFields, cancellationToken: cancellationToken);	
			}
			else
			{
				throw ErtisAuthException.MembershipIdRequired();
			}
		}
		
		[HttpGet("search")]
		[RbacAction(Rbac.CrudActions.Read)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<IActionResult> Search([FromRoute] string membershipId, [FromQuery] string keyword, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrEmpty(keyword) || string.IsNullOrEmpty(keyword.Trim()))
			{
				return this.SearchKeywordRequired();
			}
			
			this.ExtractPaginationParameters(out var skip, out var limit, out var withCount);
			this.ExtractSortingParameters(out var orderBy, out var sortDirection);
			
			return this.Ok(await this.applicationService.SearchAsync(membershipId, keyword, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken));
		}
		
		#endregion
		
		#region Update Methods

		[HttpPut("{id}")]
		[RbacObject("{id}")]
		[RbacAction(Rbac.CrudActions.Update)]
		public async Task<IActionResult> Update([FromRoute] string membershipId, [FromRoute] string id, [FromBody] UpdateApplicationFormModel model, CancellationToken cancellationToken = default)
		{
			var applicationModel = new Application
			{
				Id = id,
				Name = model.Name,
				Slug = model.Slug,
				Role = model.Role,
				MembershipId = membershipId
			};
			
			var utilizer = this.GetUtilizer();
			var app = await this.applicationService.UpdateAsync(utilizer, membershipId, applicationModel, cancellationToken: cancellationToken);
			return this.Ok(app);
		}

		#endregion
		
		#region Delete Methods

		[HttpDelete("{id}")]
		[RbacObject("{id}")]
		[RbacAction(Rbac.CrudActions.Delete)]
		public async Task<IActionResult> Delete([FromRoute] string membershipId, [FromRoute] string id, CancellationToken cancellationToken = default)
		{
			var utilizer = this.GetUtilizer();
			if (await this.applicationService.DeleteAsync(utilizer, membershipId, id, cancellationToken: cancellationToken))
			{
				return this.NoContent();
			}
			else
			{
				return this.RoleNotFound(id);
			}
		}
		
		[HttpDelete]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[RbacAction(Rbac.CrudActions.Delete)]
		public async Task<IActionResult> BulkDelete([FromRoute] string membershipId, [FromBody] string[] ids, CancellationToken cancellationToken = default)
		{
			return await this.BulkDeleteAsync(this.applicationService, membershipId, ids, cancellationToken: cancellationToken);
		}

		#endregion
	}
}