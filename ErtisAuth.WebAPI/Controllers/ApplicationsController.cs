using System.Collections.Generic;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Extensions.AspNetCore.Controllers;
using Ertis.Extensions.AspNetCore.Extensions;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.WebAPI.Annotations;
using ErtisAuth.WebAPI.Extensions;
using ErtisAuth.WebAPI.Models.Request.Applications;
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
		public async Task<IActionResult> Create([FromRoute] string membershipId, [FromBody] CreateApplicationFormModel model)
		{
			var membership = await this.membershipService.GetAsync(membershipId);
			if (membership == null)
			{
				return this.MembershipNotFound(membershipId);
			}
			
			var applicationModel = new Application
			{
				Name = model.Name,
				Secret = model.Secret,
				Role = model.Role,
				MembershipId = membershipId
			};
			
			var app = await this.applicationService.CreateAsync(membershipId, applicationModel);
			return this.Created($"{this.Request.Scheme}://{this.Request.Host}{this.Request.Path}/{app.Id}", app);
		}
		
		#endregion
		
		#region Read Methods
		
		[HttpGet("{id}")]
		[RbacObject("id")]
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
		public async Task<IActionResult> Get([FromRoute] string membershipId)
		{
			this.ExtractPaginationParameters(out int? skip, out int? limit, out bool withCount);
			this.ExtractSortingParameters(out string orderBy, out SortDirection? sortDirection);
				
			var apps = await this.applicationService.GetAsync(membershipId, skip, limit, withCount, orderBy, sortDirection);
			return this.Ok(apps);
		}
		
		protected override async Task<IPaginationCollection<dynamic>> GetDataAsync(string query, int? skip, int? limit, bool? withCount, string sortField, SortDirection? sortDirection, IDictionary<string, bool> selectFields)
		{
			return await this.applicationService.QueryAsync(query, skip, limit, withCount, sortField, sortDirection, selectFields);
		}
		
		#endregion
		
		#region Update Methods

		[HttpPut("{id}")]
		[RbacObject("id")]
		[RbacAction(Rbac.CrudActions.Update)]
		public async Task<IActionResult> Update([FromRoute] string membershipId, [FromRoute] string id, [FromBody] UpdateApplicationFormModel model)
		{
			var applicationModel = new Application
			{
				Id = id,
				Name = model.Name,
				Secret = model.Secret,
				Role = model.Role,
				MembershipId = membershipId
			};
			
			var app = await this.applicationService.UpdateAsync(membershipId, applicationModel);
			return this.Ok(app);
		}

		#endregion
		
		#region Delete Methods

		[HttpDelete("{id}")]
		[RbacObject("id")]
		[RbacAction(Rbac.CrudActions.Delete)]
		public async Task<IActionResult> Delete([FromRoute] string membershipId, [FromRoute] string id)
		{
			if (await this.applicationService.DeleteAsync(membershipId, id))
			{
				return this.NoContent();
			}
			else
			{
				return this.RoleNotFound(id);
			}
		}

		#endregion
	}
}