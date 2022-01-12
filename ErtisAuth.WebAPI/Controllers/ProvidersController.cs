using System.Collections.Generic;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Extensions.AspNetCore.Controllers;
using Ertis.Extensions.AspNetCore.Extensions;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Identity.Attributes;
using ErtisAuth.Extensions.Authorization.Annotations;
using ErtisAuth.WebAPI.Extensions;
using ErtisAuth.WebAPI.Models.Request.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers
{
	[ApiController]
	[Authorized]
	[RbacResource("providers")]
	[Route("api/v{v:apiVersion}/memberships/{membershipId}/[controller]")]
	public class ProvidersController : QueryControllerBase
	{
		#region Services

		private readonly IProviderService providerService;
		private readonly IMembershipService membershipService;

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="providerService"></param>
		/// <param name="membershipService"></param>
		public ProvidersController(IProviderService providerService, IMembershipService membershipService)
		{
			this.providerService = providerService;
			this.membershipService = membershipService;
		}

		#endregion
		
		#region Create Methods

		[HttpPost]
		[RbacAction(Rbac.CrudActions.Create)]
		public async Task<IActionResult> Create([FromRoute] string membershipId, [FromBody] CreateProviderFormModel model)
		{
			var membership = await this.membershipService.GetAsync(membershipId);
			if (membership == null)
			{
				return this.MembershipNotFound(membershipId);
			}
			
			var providerModel = new OAuthProvider
			{
				Name = model.Name,
				Description = model.Description,
				MembershipId = membershipId
			};
			
			var utilizer = this.GetUtilizer();
			var provider = await this.providerService.CreateAsync(utilizer, membershipId, providerModel);
			return this.Created($"{this.Request.Scheme}://{this.Request.Host}{this.Request.Path}/{provider.Id}", provider);
		}

		#endregion
		
		#region Read Methods

		[HttpGet("{id}")]
		[RbacObject("id")]
		[RbacAction(Rbac.CrudActions.Read)]
		public async Task<ActionResult<OAuthProvider>> Get([FromRoute] string membershipId, [FromRoute] string id)
		{
			var provider = await this.providerService.GetAsync(membershipId, id);
			if (provider != null)
			{
				return this.Ok(provider);
			}
			else
			{
				return this.ProviderNotFound(id);
			}
		}
		
		[HttpGet]
		[RbacAction(Rbac.CrudActions.Read)]
		public async Task<IActionResult> Get([FromRoute] string membershipId)
		{
			this.ExtractPaginationParameters(out int? skip, out int? limit, out bool withCount);
			this.ExtractSortingParameters(out string orderBy, out SortDirection? sortDirection);
				
			var providers = await this.providerService.GetAsync(membershipId, skip, limit, withCount, orderBy, sortDirection);
			return this.Ok(providers);
		}
		
		[HttpPost("_query")]
		[RbacAction(Rbac.CrudActions.Read)]
		public override async Task<IActionResult> Query()
		{
			return await base.Query();
		}

		protected override async Task<IPaginationCollection<dynamic>> GetDataAsync(string query, int? skip, int? limit, bool? withCount, string sortField, SortDirection? sortDirection, IDictionary<string, bool> selectFields)
		{
			return await this.providerService.QueryAsync(query, skip, limit, withCount, sortField, sortDirection, selectFields);
		}
		
		#endregion
		
		#region Update Methods

		[HttpPut("{id}")]
		[RbacObject("id")]
		[RbacAction(Rbac.CrudActions.Update)]
		public async Task<IActionResult> Update([FromRoute] string membershipId, [FromRoute] string id, [FromBody] UpdateProviderFormModel model)
		{
			var providerModel = new OAuthProvider
			{
				Id = id,
				Name = model.Name,
				Description = model.Description,
				MembershipId = membershipId
			};
			
			var utilizer = this.GetUtilizer();
			var provider = await this.providerService.UpdateAsync(utilizer, membershipId, providerModel);
			return this.Ok(provider);
		}

		#endregion
		
		#region Delete Methods

		[HttpDelete("{id}")]
		[RbacObject("id")]
		[RbacAction(Rbac.CrudActions.Delete)]
		public async Task<IActionResult> Delete([FromRoute] string membershipId, [FromRoute] string id)
		{
			var utilizer = this.GetUtilizer();
			if (await this.providerService.DeleteAsync(utilizer, membershipId, id))
			{
				return this.NoContent();
			}
			else
			{
				return this.ProviderNotFound(id);
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
			return await this.BulkDeleteAsync(this.providerService, membershipId, ids);
		}

		#endregion
	}
}