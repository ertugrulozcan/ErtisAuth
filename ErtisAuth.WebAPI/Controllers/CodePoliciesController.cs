using Ertis.Core.Models;
using Ertis.Core.Collections;
using Ertis.Extensions.AspNetCore.Controllers;
using Ertis.Extensions.AspNetCore.Extensions;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Attributes;
using ErtisAuth.Extensions.AspNetCore.Extensions;
using ErtisAuth.Extensions.AspNetCore.Services;
using ErtisAuth.Extensions.Authorization.Attributes;
using ErtisAuth.Extensions.AspNetCore.Attributes;
using ErtisAuth.WebAPI.Models.CodePolicies;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers;

[ApiController]
[Tags("Code Policies")]
[Authorized]
[RbacResource("code-policies")]
[MembershipRoute("code-policies")]
public class CodePoliciesController : QueryControllerBase
{
    #region Services
	
	private readonly ITokenCodePolicyService _codePolicyService;
	private readonly IMembershipService _membershipService;
	private readonly IUtilizerService _utilizerService;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="codePolicyService"></param>
	/// <param name="membershipService"></param>
	/// <param name="utilizerService"></param>
	public CodePoliciesController(
		ITokenCodePolicyService codePolicyService, 
		IMembershipService membershipService,
		IUtilizerService utilizerService)
	{
		this._codePolicyService = codePolicyService;
		this._membershipService = membershipService;
		this._utilizerService = utilizerService;
	}
	
	#endregion
	
	#region Read Methods
	
	/// <summary>Get a code policy</summary>
	/// <param name="membershipId">Membership id</param>
	/// <param name="id">Code policy id</param>
	[HttpGet("{id}")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<TokenCodePolicy>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<TokenCodePolicy>> Get([FromRoute] string membershipId, [FromRoute] string id)
	{
		var policy = await this._codePolicyService.GetAsync(id, membershipId);
		if (policy != null)
		{
			return this.Ok(policy);
		}
		else
		{
			return this.CodePolicyNotFound(id);
		}
	}
	
	/// <summary>List code policies</summary>
	/// <remarks>Paginated with the <c>skip</c>, <c>limit</c> and <c>with_count</c> query parameters, sorted with <c>sort</c> (e.g. <c>sort=name desc</c>).</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpGet]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<PaginationCollection<TokenCodePolicy>>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	public async Task<IActionResult> Get([FromRoute] string membershipId, CancellationToken cancellationToken = default)
	{
		this.ExtractPaginationParameters(out var skip, out var limit, out var withCount);
		this.ExtractSortingParameters(out var orderBy, out var sortDirection);
		
		var policies = await this._codePolicyService.GetAsync(membershipId, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
		return this.Ok(policies);
	}
	
	/// <summary>Query code policies</summary>
	/// <remarks>Filters the code policies with the MongoDB query in the <c>where</c> field of the body and projects them with <c>select</c>; paginated and sorted with the query parameters of the list endpoint. JavaScript operators (<c>$where</c>, <c>$function</c>) and hidden fields are rejected.</remarks>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPost("_query")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<PaginationCollection<TokenCodePolicy>>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	public override async Task<IActionResult> Query(CancellationToken cancellationToken = default)
	{
		return await base.Query(cancellationToken: cancellationToken);
	}
	
	[NonAction]
	protected override async Task<IPaginationCollection<dynamic>> GetDataAsync(string query, int? skip, int? limit, bool? withCount, string? sortField, SortDirection? sortDirection, IDictionary<string, bool> projection, CancellationToken cancellationToken = default)
	{
		if (this.Request.RouteValues.TryGetValue("membershipId", out var membershipIdValue) && membershipIdValue is string membershipId && !string.IsNullOrEmpty(membershipId))
		{
			return await this._codePolicyService.QueryAsync(query, membershipId, skip, limit, withCount, sortField, sortDirection, projection, cancellationToken: cancellationToken);	
		}
		else
		{
			throw ErtisAuthException.MembershipIdRequired();
		}
	}
	
	#endregion
	
	#region Create Methods
	
	/// <summary>Create a code policy</summary>
	/// <remarks>Defines how token codes are generated: length, letters and/or digits, and lifetime in seconds (<c>expires_in</c>).</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="model">Code policy</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPost]
	[RbacAction(Rbac.CrudActions.Create)]
	[ProducesResponseType<TokenCodePolicy>(StatusCodes.Status201Created)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> Create([FromRoute] string membershipId, [FromBody] CreateTokenCodePolicyFormModel model, CancellationToken cancellationToken = default)
	{
		var membership = await this._membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
		if (membership == null)
		{
			return this.MembershipNotFound(membershipId);
		}
		
		var policyModel = ToTokenCodePolicy(membershipId, null, model.Name, model.Slug, model.Description, model.Length, model.ContainsLetters, model.ContainsDigits, model.ExpiresIn);
		var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
		var policy = await this._codePolicyService.CreateAsync(policyModel, membershipId, utilizer, cancellationToken: cancellationToken);
		return this.Created($"{this.Request.Scheme}://{this.Request.Host}{this.Request.Path}/{policy.Id}", policy);
	}
	
	#endregion
	
	#region Update Methods
	
	/// <summary>Update a code policy</summary>
	/// <remarks>**Note:** an update without any change answers 409 (<c>IdenticalDocument</c>).</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="id">Code policy id</param>
	/// <param name="model">Code policy</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPut("{id}")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Update)]
	[ProducesResponseType<TokenCodePolicy>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> Update([FromRoute] string membershipId, [FromRoute] string id, [FromBody] UpdateTokenCodePolicyFormModel model, CancellationToken cancellationToken = default)
	{
		var policyModel = ToTokenCodePolicy(membershipId, id, model.Name, model.Slug, model.Description, model.Length, model.ContainsLetters, model.ContainsDigits, model.ExpiresIn);
		var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
		var policy = await this._codePolicyService.UpdateAsync(policyModel, membershipId, utilizer, cancellationToken: cancellationToken);
		return this.Ok(policy);
	}
	
	[NonAction]
	private static TokenCodePolicy ToTokenCodePolicy(string membershipId, string? id, string? name, string? slug, string? description, int length, bool containsLetters, bool containsDigits, int expiresIn)
	{
		var policy = new TokenCodePolicy
		{
			Name = name ?? string.Empty,
			Description = description,
			Length = length,
			ContainsLetters = containsLetters,
			ContainsDigits = containsDigits,
			ExpiresIn = expiresIn,
			MembershipId = membershipId
		};
		
		if (id != null)
		{
			policy.Id = id;
		}
		
		// Otherwise derived from the name
		if (!string.IsNullOrEmpty(slug))
		{
			policy.Slug = slug;
		}
		
		return policy;
	}
	
	#endregion
	
	#region Delete Methods
	
	/// <summary>Delete a code policy</summary>
	/// <param name="membershipId">Membership id</param>
	/// <param name="id">Code policy id</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpDelete("{id}")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Delete)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> Delete([FromRoute] string membershipId, [FromRoute] string id, CancellationToken cancellationToken = default)
	{
		var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
		if (await this._codePolicyService.DeleteAsync(id, membershipId, utilizer, cancellationToken: cancellationToken))
		{
			return this.NoContent();
		}
		else
		{
			return this.CodePolicyNotFound(id);
		}
	}
	
	/// <summary>Delete code policies</summary>
	/// <remarks>Deletes the code policies with the ids in the body. Returns 204 when all of them are deleted and 404 when none of them is deleted. **Note:** when only some of them are deleted the response is 200 with an error body (<c>BulkDeletePartial</c>), not a success.</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="ids">Ids of the code policies to delete</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpDelete]
	[RbacAction(Rbac.CrudActions.Delete)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> BulkDelete([FromRoute] string membershipId, [FromBody] string[]? ids, CancellationToken cancellationToken = default)
	{
		if (ids != null)
		{
			var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
			var isDeleted = await this._codePolicyService.BulkDeleteAsync(ids, membershipId, utilizer, cancellationToken);
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