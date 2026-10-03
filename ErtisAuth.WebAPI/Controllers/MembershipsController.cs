using Ertis.Core.Models;
using Ertis.Core.Collections;
using Ertis.Extensions.AspNetCore.Controllers;
using Ertis.Extensions.AspNetCore.Extensions;
using Ertis.MongoDB.Queries;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models;
using ErtisAuth.Core.Models.Cryptography;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Attributes;
using ErtisAuth.Extensions.Authorization.Attributes;
using ErtisAuth.Extensions.AspNetCore.Extensions;
using ErtisAuth.Extensions.AspNetCore.Services;
using ErtisAuth.WebAPI.Models.Memberships;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers;

[ApiController]
[Tags("Memberships")]
[Authorized]
[RbacResource("memberships")]
[Route("memberships")]
public class MembershipsController : QueryControllerBase
{
	#region Services
	
	private readonly IMembershipService _membershipService;
	private readonly IUtilizerService _utilizerService;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="membershipService"></param>
	/// <param name="utilizerService"></param>
	public MembershipsController(IMembershipService membershipService, IUtilizerService utilizerService)
	{
		this._membershipService = membershipService;
		this._utilizerService = utilizerService;
	}
	
	#endregion
	
	#region Read Methods
	
	/// <summary>Get a membership</summary>
	/// <remarks>**Note:** the response contains the secret key of the membership (the key the tokens are signed with); grant the read permission of memberships carefully.</remarks>
	/// <param name="id">Membership id</param>
	[HttpGet("{id}")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<Membership>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Get([FromRoute] string id)
	{
		var membership = await this._membershipService.GetAsync(id);
		if (membership != null)
		{
			return this.Ok(membership);
		}
		else
		{
			return this.MembershipNotFound(id);
		}
	}
	
	/// <summary>List memberships</summary>
	/// <remarks>Paginated with the <c>skip</c>, <c>limit</c> and <c>with_count</c> query parameters, sorted with <c>sort</c> (e.g. <c>sort=name desc</c>). **Note:** the response contains the secret keys of the memberships.</remarks>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpGet]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<PaginationCollection<Membership>>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	public async Task<IActionResult> Get(CancellationToken cancellationToken = default)
	{
		this.ExtractPaginationParameters(out var skip, out var limit, out var withCount);
		this.ExtractSortingParameters(out var orderBy, out var sortDirection);
		
		var memberships = await this._membershipService.GetAsync(skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
		return this.Ok(memberships);
	}
	
	/// <summary>Query memberships</summary>
	/// <remarks>Filters the memberships with the MongoDB query in the <c>where</c> field of the body and projects them with <c>select</c>; paginated and sorted with the query parameters of the list endpoint. JavaScript operators (<c>$where</c>, <c>$function</c>) and hidden fields are rejected.</remarks>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPost("_query")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<PaginationCollection<Membership>>(StatusCodes.Status200OK)]
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
		return await this._membershipService.QueryAsync(query, skip, limit, withCount, sortField, sortDirection, projection, cancellationToken: cancellationToken);
	}
	
	/// <summary>Search memberships</summary>
	/// <remarks>Full text search with the <c>keyword</c> query parameter; paginated and sorted like the list endpoint.</remarks>
	/// <param name="keyword">Text to search for</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpGet("search")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<PaginationCollection<Membership>>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	public async Task<IActionResult> Search([FromQuery] string keyword, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(keyword) || string.IsNullOrEmpty(keyword.Trim()))
		{
			return this.SearchKeywordRequired();
		}
		
		this.ExtractPaginationParameters(out var skip, out var limit, out var withCount);
		this.ExtractSortingParameters(out var orderBy, out var sortDirection);
		
		return this.Ok(await this._membershipService.SearchAsync(keyword, null, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken));
	}
	
	#endregion
	
	#region Create Methods
	
	/// <summary>Create a membership</summary>
	/// <remarks>Creates an isolated tenant (like a realm) with its own users, roles, applications and token settings.</remarks>
	/// <param name="model">Membership</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPost]
	[RbacAction(Rbac.CrudActions.Create)]
	[ProducesResponseType<Membership>(StatusCodes.Status201Created)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> Create([FromBody] CreateMembershipFormModel model, CancellationToken cancellationToken = default)
	{
		var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
		var membership = await this._membershipService.CreateAsync(ToMembership(model), utilizer, cancellationToken: cancellationToken);
		return this.Created($"{this.Request.Scheme}://{this.Request.Host}{this.Request.Path}/{membership.Id}", membership);
	}
	
	#endregion
	
	#region Update Methods
	
	/// <summary>Update a membership</summary>
	/// <param name="id">Membership id</param>
	/// <param name="model">Membership</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPut("{id}")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Update)]
	[ProducesResponseType<Membership>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> Update([FromRoute] string id, [FromBody] UpdateMembershipFormModel model, CancellationToken cancellationToken = default)
	{
		// The route id is the one authorized (RbacObject), so it is the one updated
		var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
		var membership = ToMembership(id, model);
		return this.Ok(await this._membershipService.UpdateAsync(membership, utilizer, cancellationToken: cancellationToken));
	}
	
	#endregion
	
	#region Delete Methods
	
	/// <summary>Delete a membership</summary>
	/// <remarks>A membership which still has resources (users, roles, applications...) can not be deleted.</remarks>
	/// <param name="id">Membership id</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpDelete("{id}")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Delete)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> Delete([FromRoute] string id, CancellationToken cancellationToken = default)
	{
		if (await this._membershipService.DeleteAsync(id, cancellationToken: cancellationToken))
		{
			return this.NoContent();
		}
		else
		{
			return this.MembershipNotFound(id);
		}
	}
	
	#endregion
	
	#region Membership Setting Methods
	
	/// <summary>Get the membership settings</summary>
	/// <remarks>Returns the supported encodings, hash algorithms and database locales with their defaults, in one response.</remarks>
	[HttpGet("settings")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	public IActionResult GetAllSettings()
	{
		return this.Ok(new
		{
			encodings = System.Text.Encoding.GetEncodings().Select(x => new
			{
				displayName = x.DisplayName,
				name = x.Name.ToUpperInvariant()
			}).ToArray(),
			defaultEncoding = Core.Constants.Defaults.DEFAULT_ENCODING.HeaderName,
			hashAlgorithms = Enum.GetNames<HashAlgorithms>().Select(x => x.Replace('_', '-')).ToArray(),
			defaultHashAlgorithm = Core.Constants.Defaults.RECOMMENDED_HASH_ALGORITHM.ToString().Replace('_', '-'),
			dbLocales = TextSearchLanguage.All.ToArray(),
			defaultDbLocale = TextSearchLanguage.None.ISO6391Code
		});
	}
	
	/// <summary>List the encodings</summary>
	/// <remarks>The encodings a membership can use.</remarks>
	[HttpGet("settings/encodings")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	public IActionResult GetEncodingList()
	{
		var encodings = System.Text.Encoding.GetEncodings();
		return this.Ok(encodings.Select(x => new
		{
			displayName = x.DisplayName,
			name = x.Name.ToUpperInvariant()
		}).ToArray());
	}
	
	/// <summary>Get the default encoding</summary>
	[HttpGet("settings/encodings/default")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<string>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	public IActionResult GetDefaultEncoding()
	{
		return this.Ok(Core.Constants.Defaults.DEFAULT_ENCODING.HeaderName);
	}
	
	/// <summary>List the hash algorithms</summary>
	/// <remarks>The password hash algorithms a membership can use.</remarks>
	[HttpGet("settings/hash-algorithms")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<string[]>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	public IActionResult GetHashAlgorithmList()
	{
		var hashAlgorithms = Enum.GetNames<HashAlgorithms>().Select(x => x.Replace('_', '-'));
		return this.Ok(hashAlgorithms.ToArray());
	}
	
	/// <summary>Get the recommended hash algorithm</summary>
	/// <remarks>**Note:** a membership has no default hash algorithm; this is only the recommended one, the algorithm must be given when the membership is created.</remarks>
	[HttpGet("settings/hash-algorithms/default")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<string>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	public IActionResult GetDefaultHashAlgorithm()
	{
		return this.Ok(Core.Constants.Defaults.RECOMMENDED_HASH_ALGORITHM.ToString().Replace('_', '-'));
	}
	
	/// <summary>List the database locales</summary>
	/// <remarks>The languages of the MongoDB text search.</remarks>
	[HttpGet("settings/db-locales")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<TextSearchLanguage[]>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	public IActionResult GetDbLocales()
	{
		var languages = TextSearchLanguage.All;
		return this.Ok(languages.ToArray());
	}
	
	/// <summary>Get the default database locale</summary>
	[HttpGet("settings/db-locales/default")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<string>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	public IActionResult GetDefaultDbLocale()
	{
		return this.Ok(TextSearchLanguage.None.ISO6391Code);
	}
	
	#endregion
	
	#region Mapping Methods
	
	[NonAction]
	private static Membership ToMembership(CreateMembershipFormModel model)
	{
		return ToMembership(null, model.Name, model.Slug, model.SecretKey, model.ExpiresIn, model.ScopedTokenExpiresIn, model.RefreshTokenExpiresIn, model.ResetPasswordTokenExpiresIn, model.HashAlgorithm, model.DefaultEncoding, model.DefaultLanguage, model.MailProviders, model.UserActivation, model.CodePolicy, model.OtpSettings);
	}
	
	[NonAction]
	private static Membership ToMembership(string id, UpdateMembershipFormModel model)
	{
		var membership = ToMembership(id, model.Name, model.Slug, model.SecretKey, model.ExpiresIn, model.ScopedTokenExpiresIn, model.RefreshTokenExpiresIn, model.ResetPasswordTokenExpiresIn, model.HashAlgorithm, model.DefaultEncoding, model.DefaultLanguage, model.MailProviders, model.UserActivation, model.CodePolicy, model.OtpSettings);
		membership.AllowMembershipSecretForApplications = model.AllowMembershipSecretForApplications; // LEGACY-APP-SECRET
		return membership;
	}
	
	[NonAction]
	private static Membership ToMembership(
		string? id,
		string? name,
		string? slug,
		string? secretKey,
		int expiresIn,
		int scopedTokenExpiresIn,
		int refreshTokenExpiresIn,
		int? resetPasswordTokenExpiresIn,
		string? hashAlgorithm,
		string? defaultEncoding,
		string? defaultLanguage,
		IMailProvider[]? mailProviders,
		Status userActivation,
		string? codePolicy,
		OtpSettings? otpSettings)
	{
		var membership = new Membership
		{
			Name = name ?? string.Empty,
			SecretKey = secretKey ?? string.Empty,
			ExpiresIn = expiresIn,
			ScopedTokenExpiresIn = scopedTokenExpiresIn,
			RefreshTokenExpiresIn = refreshTokenExpiresIn,
			ResetPasswordTokenExpiresIn = resetPasswordTokenExpiresIn,
			HashAlgorithm = hashAlgorithm,
			DefaultEncoding = defaultEncoding,
			DefaultLanguage = defaultLanguage,
			MailProviders = mailProviders,
			UserActivation = userActivation,
			CodePolicy = codePolicy,
			OtpSettings = otpSettings
		};
		
		if (id != null)
		{
			membership.Id = id;
		}
		
		// Otherwise derived from the name
		if (!string.IsNullOrEmpty(slug))
		{
			membership.Slug = slug;
		}
		
		return membership;
	}
	
	#endregion
}