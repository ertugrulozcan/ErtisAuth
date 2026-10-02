using Ertis.Core.Models;
using Ertis.Core.Collections;
using Ertis.Extensions.AspNetCore.Controllers;
using Ertis.Extensions.AspNetCore.Extensions;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Core.Attributes;
using ErtisAuth.Extensions.Authorization.Attributes;
using ErtisAuth.Extensions.AspNetCore.Extensions;
using ErtisAuth.Extensions.AspNetCore.Services;
using ErtisAuth.Extensions.AspNetCore.Attributes;
using Ertis.Schema.Types;
using ErtisAuth.WebAPI.Models.UserTypes;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers;

[ApiController]
[Authorized]
[Tags("User Types")]
[RbacResource("user-types")]
[MembershipRoute("user-types")]
public class UserTypesController : QueryControllerBase
{
    #region Services
	
    private readonly IUserTypeService _userTypeService;
	private readonly IUtilizerService _utilizerService;
	
    #endregion
	
    #region Constructors
	
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="userTypeService"></param>
	/// <param name="utilizerService"></param>
    public UserTypesController(IUserTypeService userTypeService, IUtilizerService utilizerService)
    {
        this._userTypeService = userTypeService;
		this._utilizerService = utilizerService;
    }
	
    #endregion
    
    #region Read Methods
	
	/// <summary>Get a user type</summary>
	/// <remarks>Returns the user type with the fields inherited from its base types.</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="id">User type id</param>
	[HttpGet("{id}")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<UserType>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<UserType>> Get([FromRoute] string membershipId, [FromRoute] string id)
	{
		var userType = await this._userTypeService.GetAsync(id, membershipId);
		if (userType != null)
		{
			return this.Ok(userType);
		}
		else
		{
			return this.UserTypeNotFound(id);
		}
	}
	
	/// <summary>Get the declaring types of the fields</summary>
	/// <remarks>Groups the fields of the user type by the type that declares them in the inheritance chain, up to the origin user type <c>base-user</c>.</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="id">User type id</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpGet("relations/{id}")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<Dictionary<string, List<string>>>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<Dictionary<string, List<string>>>> GetFieldInfoOwnerRelations([FromRoute] string membershipId, [FromRoute] string id, CancellationToken cancellationToken = default)
	{
		var relations = await this._userTypeService.GetFieldInfoOwnerRelationsAsync(id, membershipId, cancellationToken: cancellationToken);
		if (relations != null)
		{
			return this.Ok(relations);
		}
		else
		{
			return this.UserTypeNotFound(id);
		}
	}
	
	/// <summary>List user types</summary>
	/// <remarks>Returns the stored user types of the membership; the origin user type is not included. Paginated with the <c>skip</c>, <c>limit</c> and <c>with_count</c> query parameters, sorted with <c>sort</c> (e.g. <c>sort=name desc</c>).</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpGet]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<PaginationCollection<UserType>>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	public async Task<IActionResult> Get([FromRoute] string membershipId, CancellationToken cancellationToken = default)
	{
		this.ExtractPaginationParameters(out var skip, out var limit, out var withCount);
		this.ExtractSortingParameters(out var orderBy, out var sortDirection);
		
		var userTypes = await this._userTypeService.GetAsync(membershipId, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
		return this.Ok(userTypes);
	}
	
	/// <summary>List all user types</summary>
	/// <remarks>Returns all user types of the membership without pagination, the origin user type <c>base-user</c> included.</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpGet("all")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<PaginationCollection<UserType>>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	public async Task<IActionResult> GetAll([FromRoute] string membershipId, CancellationToken cancellationToken = default)
	{
		var userTypes = await this._userTypeService.GetAsync(membershipId, null, null, false, null, null, cancellationToken: cancellationToken);
		var allUserTypes = new List<UserType>();
		allUserTypes.AddRange(userTypes.Items);
		
		var originUserType = await this._userTypeService.GetByNameOrSlugAsync(UserType.ORIGIN_USER_TYPE_SLUG, membershipId, cancellationToken: cancellationToken);
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
	
	/// <summary>Query user types</summary>
	/// <remarks>Filters the user types with the MongoDB query in the <c>where</c> field of the body and projects them with <c>select</c>; paginated and sorted with the query parameters of the list endpoint. JavaScript operators (<c>$where</c>, <c>$function</c>) and hidden fields are rejected.</remarks>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPost("_query")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<PaginationCollection<UserType>>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	public override async Task<IActionResult> Query(CancellationToken cancellationToken = default)
	{
		return await base.Query(cancellationToken: cancellationToken);
	}
	
	protected override async Task<IPaginationCollection<dynamic>> GetDataAsync(string query, int? skip, int? limit, bool? withCount, string? sortField, SortDirection? sortDirection, IDictionary<string, bool> projection, CancellationToken cancellationToken = default)
	{
		if (this.Request.RouteValues.TryGetValue("membershipId", out var membershipIdValue) && membershipIdValue is string membershipId && !string.IsNullOrEmpty(membershipId))
		{
			return await this._userTypeService.QueryAsync(query, membershipId, skip, limit, withCount, sortField, sortDirection, projection, cancellationToken: cancellationToken);	
		}
		else
		{
			throw ErtisAuthException.MembershipIdRequired();
		}
	}
	
	#endregion
	
	#region Create Methods
	
	/// <summary>Create a user type</summary>
	/// <remarks>Defines the schema of a kind of user. The base type (<c>baseType</c>) can be given by its slug or name and is stored by its slug; without it the type inherits from the origin user type <c>base-user</c>. The slug is derived from the name when not given.</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="model">User type</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPost]
	[RbacAction(Rbac.CrudActions.Create)]
	[ProducesResponseType<UserType>(StatusCodes.Status201Created)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> Create([FromRoute] string membershipId, [FromBody] CreateUserTypeFormModel model, CancellationToken cancellationToken = default)
	{
		var userTypeModel = ToUserType(membershipId, null, model.Name, model.Slug, model.Description, model.Properties, model.AllowAdditionalProperties, model.IsAbstract, model.IsSealed, model.BaseUserType);
		var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
		var userType = await this._userTypeService.CreateAsync(userTypeModel, membershipId, utilizer, cancellationToken: cancellationToken);
		return this.Created($"{this.Request.Scheme}://{this.Request.Host}{this.Request.Path}/{userType.Id}", userType);
	}
	
	#endregion
	
	#region Update Methods
	
	/// <summary>Update a user type</summary>
	/// <remarks>**Note:** the slug of a user type which has users or derived types does not change (a new slug is ignored), since they refer to it by its slug. **Note:** an update without any change answers 409 (<c>IdenticalDocument</c>).</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="id">User type id</param>
	/// <param name="model">User type</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPut("{id}")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Update)]
	[ProducesResponseType<UserType>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> Update([FromRoute] string membershipId, [FromRoute] string id, [FromBody] UpdateUserTypeFormModel model, CancellationToken cancellationToken = default)
	{
		var userTypeModel = ToUserType(membershipId, id, model.Name, model.Slug, model.Description, model.Properties, model.AllowAdditionalProperties, model.IsAbstract, model.IsSealed, model.BaseUserType);
		var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
		var userType = await this._userTypeService.UpdateAsync(userTypeModel, membershipId, utilizer, cancellationToken: cancellationToken);
		return this.Ok(userType);
	}
	
	private static UserType ToUserType(
		string membershipId, 
		string? id, 
		string? name, 
		string? slug, 
		string? description, 
		IReadOnlyCollection<IFieldInfo>? properties, 
		bool allowAdditionalProperties, 
		bool isAbstract, 
		bool isSealed, 
		string? baseUserType)
	{
		var userType = new UserType
		{
			Name = name ?? string.Empty,
			Description = description,
			Properties = properties ?? [],
			AllowAdditionalProperties = allowAdditionalProperties,
			IsAbstract = isAbstract,
			IsSealed = isSealed,
			BaseUserType = baseUserType,
			MembershipId = membershipId
		};
		
		if (id != null)
		{
			userType.Id = id;
		}
		
		// Otherwise derived from the name
		if (!string.IsNullOrEmpty(slug))
		{
			userType.Slug = slug;
		}
		
		return userType;
	}
	
	#endregion
	
	#region Delete Methods
	
	/// <summary>Delete a user type</summary>
	/// <remarks>A user type which has users or derived types can not be deleted.</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="id">User type id</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpDelete("{id}")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Delete)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Delete([FromRoute] string membershipId, [FromRoute] string id, CancellationToken cancellationToken = default)
	{
		var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
		if (await this._userTypeService.DeleteAsync(id, membershipId, utilizer, cancellationToken: cancellationToken))
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