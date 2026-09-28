using Ertis.Core.Collections;
using Ertis.Extensions.AspNetCore.Controllers;
using Ertis.Extensions.AspNetCore.Extensions;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Core.Attributes;
using ErtisAuth.Extensions.Authorization.Attributes;
using ErtisAuth.Extensions.AspNetCore.Extensions;
using ErtisAuth.Extensions.AspNetCore.Services;
using ErtisAuth.Extensions.AspNetCore.Attributes;
using ErtisAuth.WebAPI.Models.MailHooks;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers;

[ApiController]
[Authorized]
[RbacResource("mailhooks")]
[MembershipRoute("mailhooks")]
public class MailHooksController : QueryControllerBase
{
    #region Services
	
	private readonly IMailHookService _mailHookService;
	private readonly IMembershipService _membershipService;
	private readonly IUtilizerService _utilizerService;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="mailHookService"></param>
	/// <param name="membershipService"></param>
	/// <param name="utilizerService"></param>
	public MailHooksController(
		IMailHookService mailHookService, 
		IMembershipService membershipService,
		IUtilizerService utilizerService)
	{
		this._mailHookService = mailHookService;
		this._membershipService = membershipService;
		this._utilizerService = utilizerService;
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
	public async Task<IActionResult> Create([FromRoute] string membershipId, [FromBody] CreateMailHookFormModel model, CancellationToken cancellationToken = default)
	{
		var membership = await this._membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
		if (membership == null)
		{
			return this.MembershipNotFound(membershipId);
		}
		
		var mailHookModel = ToMailHook(membershipId, null, model.Name, model.Slug, model.Description, model.Event, model.Status, model.MailSubject, model.MailTemplate, model.FromName, model.FromAddress, model.SendToUtilizer, model.Recipients, model.MailProvider, model.Variables);
		var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
		var mailHook = await this._mailHookService.CreateAsync(utilizer, membershipId, mailHookModel, cancellationToken: cancellationToken);
		return this.Created($"{this.Request.Scheme}://{this.Request.Host}{this.Request.Path}/{mailHook.Id}", mailHook);
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
	public async Task<ActionResult<MailHook>> Get([FromRoute] string membershipId, [FromRoute] string id)
	{
		var mailHook = await this._mailHookService.GetAsync(membershipId, id);
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
	public async Task<IActionResult> Get([FromRoute] string membershipId, CancellationToken cancellationToken = default)
	{
		this.ExtractPaginationParameters(out var skip, out var limit, out var withCount);
		this.ExtractSortingParameters(out var orderBy, out var sortDirection);
		
		var mailHooks = await this._mailHookService.GetAsync(membershipId, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
		return this.Ok(mailHooks);
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
		if (this.Request.RouteValues.TryGetValue("membershipId", out var membershipIdValue) && membershipIdValue is string membershipId && !string.IsNullOrEmpty(membershipId))
		{
			return await this._mailHookService.QueryAsync(membershipId, query, skip, limit, withCount, sortField, sortDirection, selectFields, cancellationToken: cancellationToken);	
		}
		else
		{
			throw ErtisAuthException.MembershipIdRequired();
		}
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
	public async Task<IActionResult> Update([FromRoute] string membershipId, [FromRoute] string id, [FromBody] UpdateMailHookFormModel model, CancellationToken cancellationToken = default)
	{
		// The route id, not an id in the body, identifies the updated mail hook (RBAC checks the route id)
		var mailHookModel = ToMailHook(membershipId, id, model.Name, model.Slug, model.Description, model.Event, model.Status, model.MailSubject, model.MailTemplate, model.FromName, model.FromAddress, model.SendToUtilizer, model.Recipients, model.MailProvider, model.Variables);
		var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
		var mailHook = await this._mailHookService.UpdateAsync(utilizer, membershipId, mailHookModel, cancellationToken: cancellationToken);
		return this.Ok(mailHook);
	}
	
	private static MailHook ToMailHook(
		string membershipId, 
		string? id, 
		string? name, 
		string? slug, 
		string? description, 
		string? eventName, 
		string? status, 
		string? mailSubject, 
		string? mailTemplate, 
		string? fromName, 
		string? fromAddress, 
		bool sendToUtilizer, 
		Recipient[]? recipients, 
		string? mailProvider, 
		MailHookVariable[]? variables)
	{
		var mailHook = new MailHook
		{
			Name = name ?? string.Empty,
			Description = description,
			Event = eventName,
			Status = status,
			MailSubject = mailSubject,
			MailTemplate = mailTemplate,
			FromName = fromName,
			FromAddress = fromAddress,
			SendToUtilizer = sendToUtilizer,
			Recipients = recipients,
			MailProvider = mailProvider,
			Variables = variables,
			MembershipId = membershipId
		};
		
		if (id != null)
		{
			mailHook.Id = id;
		}
		
		// Otherwise derived from the name
		if (!string.IsNullOrEmpty(slug))
		{
			mailHook.Slug = slug;
		}
		
		return mailHook;
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
	public async Task<IActionResult> Delete([FromRoute] string membershipId, [FromRoute] string id, CancellationToken cancellationToken = default)
	{
		var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
		if (await this._mailHookService.DeleteAsync(utilizer, membershipId, id, cancellationToken: cancellationToken))
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
	public async Task<IActionResult> BulkDelete([FromRoute] string membershipId, [FromBody] string[]? ids, CancellationToken cancellationToken = default)
	{
		if (ids != null)
		{
			var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
			var isDeleted = await this._mailHookService.BulkDeleteAsync(utilizer, membershipId, ids, cancellationToken);
			if (isDeleted != null)
			{
				if (isDeleted.Value)
				{
					return this.NoContent();
				}
				else
				{
					return this.BulkDeleteFailed(ids);
				}
			}
			else
			{
				return this.BulkDeletePartial();
			}
		}
		else
		{
			return this.BadRequest();
		}
	}
	
	#endregion
}