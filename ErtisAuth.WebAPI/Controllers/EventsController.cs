using System.Collections.Generic;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Extensions.AspNetCore.Controllers;
using Ertis.Extensions.AspNetCore.Extensions;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Identity.Attributes;
using ErtisAuth.Extensions.Authorization.Annotations;
using ErtisAuth.WebAPI.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers
{
	[ApiController]
	[Authorized]
	[RbacResource("events")]
	[Route("api/v{v:apiVersion}/memberships/{membershipId}/[controller]")]
	public class EventsController : QueryControllerBase
	{
		#region Services

		private readonly IEventService eventService;
		private readonly IMembershipService membershipService;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="eventService"></param>
		/// <param name="membershipService"></param>
		public EventsController(IEventService eventService, IMembershipService membershipService)
		{
			this.eventService = eventService;
			this.membershipService = membershipService;
		}

		#endregion
		
		#region Read Methods
		
		[HttpGet("{id}")]
		[RbacObject("{id}")]
		[RbacAction(Rbac.CrudActions.Read)]
		public async Task<ActionResult<ErtisAuthEvent>> Get([FromRoute] string membershipId, [FromRoute] string id)
		{
			var ertisAuthEvent = await this.eventService.GetDynamicAsync(membershipId, id);
			if (ertisAuthEvent != null)
			{
				return this.Ok(ertisAuthEvent);
			}
			else
			{
				return this.EventNotFound(id);
			}
		}
		
		[HttpGet]
		[RbacAction(Rbac.CrudActions.Read)]
		public async Task<IActionResult> Get([FromRoute] string membershipId)
		{
			this.ExtractPaginationParameters(out int? skip, out int? limit, out bool withCount);
			this.ExtractSortingParameters(out string orderBy, out SortDirection? sortDirection);
				
			var events = await this.eventService.GetDynamicAsync(membershipId, skip, limit, withCount, orderBy, sortDirection);
			return this.Ok(events);
		}

		[HttpPost("_query")]
		[RbacAction(Rbac.CrudActions.Read)]
		public override async Task<IActionResult> Query()
		{
			return await base.Query();
		}

		protected override async Task<IPaginationCollection<dynamic>> GetDataAsync(string query, int? skip, int? limit, bool? withCount, string sortField, SortDirection? sortDirection, IDictionary<string, bool> selectFields)
		{
			return await this.eventService.QueryAsync(query, skip, limit, withCount, sortField, sortDirection, selectFields);
		}
		
		#endregion
		
		#region Create Methods

		[HttpPost]
		[RbacAction(Rbac.CrudActions.Create)]
		public async Task<IActionResult> Create([FromRoute] string membershipId, [FromBody] ErtisAuthCustomEvent model)
		{
			var membership = await this.membershipService.GetAsync(membershipId);
			if (membership == null)
			{
				return this.MembershipNotFound(membershipId);
			}
			
			// Validation
			if (string.IsNullOrEmpty(model.EventType))
			{
				return this.BadRequest("event_type is required field");
			}

			var eventType = model.EventType;
			eventType = eventType.Replace(" ", string.Empty);

			var utilizerId = model.UtilizerId;
			if (string.IsNullOrEmpty(utilizerId))
			{
				var utilizer = this.GetUtilizer();
				utilizerId = utilizer.Id;
			}
			
			var ertisAuthCustomEvent = new ErtisAuthCustomEvent
			{
				EventType = eventType,
				Document = model.Document,
				Prior = model.Prior,
				MembershipId = membershipId,
				UtilizerId = utilizerId
			};
				
			var ertisAuthEvent = await this.eventService.FireEventAsync(this, ertisAuthCustomEvent);
			return this.Created($"{this.Request.Scheme}://{this.Request.Host}{this.Request.Path}/{ertisAuthEvent.Id}", ertisAuthEvent);
		}

		#endregion
	}
}