using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Extensions.AspNetCore.Controllers;
using Ertis.Extensions.AspNetCore.Extensions;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Models.Webhooks;
using ErtisAuth.Identity.Attributes;
using ErtisAuth.Extensions.Authorization.Annotations;
using ErtisAuth.WebAPI.Extensions;
using ErtisAuth.WebAPI.Models.Request.Webhooks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers
{
	[ApiController]
	[Authorized]
	[RbacResource("webhooks")]
	[Route("api/v{v:apiVersion}/memberships/{membershipId}/[controller]")]
	public class WebhooksController : QueryControllerBase
	{
		#region Services

		private readonly IWebhookService webhookService;
		private readonly IMembershipService membershipService;
		
		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="webhookService"></param>
		/// <param name="membershipService"></param>
		public WebhooksController(IWebhookService webhookService, IMembershipService membershipService)
		{
			this.webhookService = webhookService;
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
		public async Task<IActionResult> Create([FromRoute] string membershipId, [FromBody] CreateWebhookFormModel model)
		{
			var membership = await this.membershipService.GetAsync(membershipId);
			if (membership == null)
			{
				return this.MembershipNotFound(membershipId);
			}
			
			var webhookModel = new Webhook
			{
				Name = model.Name,
				Description = model.Description,
				Event = model.Event,
				Status = model.Status,
				TryCount = model.TryCount,
				RequestList = model.RequestList?.ToArray(),
				MembershipId = membershipId
			};
			
			var utilizer = this.GetUtilizer();
			var webhook = await this.webhookService.CreateAsync(utilizer, membershipId, webhookModel);
			return this.Created($"{this.Request.Scheme}://{this.Request.Host}{this.Request.Path}/{webhook.Id}", webhook);
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
		public async Task<ActionResult<Webhook>> Get([FromRoute] string membershipId, [FromRoute] string id)
		{
			var webhook = await this.webhookService.GetAsync(membershipId, id);
			if (webhook != null)
			{
				return this.Ok(webhook);
			}
			else
			{
				return this.WebhookNotFound(id);
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
				
			var webhooks = await this.webhookService.GetAsync(membershipId, skip, limit, withCount, orderBy, sortDirection);
			return this.Ok(webhooks);
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
			return await this.webhookService.QueryAsync(query, skip, limit, withCount, sortField, sortDirection, selectFields);
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
			
			return this.Ok(await this.webhookService.SearchAsync(membershipId, keyword, skip, limit, withCount, orderBy, sortDirection));
		}
		
		#endregion
		
		#region Update Methods

		[HttpPut("{id}")]
		[RbacObject("{id}")]
		[RbacAction(Rbac.CrudActions.Update)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<IActionResult> Update([FromRoute] string membershipId, [FromRoute] string id, [FromBody] UpdateWebhookFormModel model)
		{
			var webhookModel = new Webhook
			{
				Id = id,
				Name = model.Name,
				Description = model.Description,
				Event = model.Event,
				Status = model.Status,
				TryCount = model.TryCount,
				RequestList = model.RequestList?.ToArray(),
				MembershipId = membershipId
			};
			
			var utilizer = this.GetUtilizer();
			var webhook = await this.webhookService.UpdateAsync(utilizer, membershipId, webhookModel);
			return this.Ok(webhook);
		}
		
		#endregion

		#region Delete Methods

		[HttpDelete("{id}")]
		[RbacObject("{id}")]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[RbacAction(Rbac.CrudActions.Delete)]
		public async Task<IActionResult> Delete([FromRoute] string membershipId, [FromRoute] string id)
		{
			var utilizer = this.GetUtilizer();
			if (await this.webhookService.DeleteAsync(utilizer, membershipId, id))
			{
				return this.NoContent();
			}
			else
			{
				return this.WebhookNotFound(id);
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
			return await this.BulkDeleteAsync(this.webhookService, membershipId, ids);
		}

		#endregion
	}
}