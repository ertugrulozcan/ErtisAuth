using Ertis.Core.Models;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Extensions.Authorization.Attributes;
using ErtisAuth.Extensions.AspNetCore.Extensions;
using ErtisAuth.Extensions.AspNetCore.Services;
using ErtisAuth.Extensions.AspNetCore.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers;

[ApiController]
[Tags("Token Codes")]
[Authorized]
[RbacResource("tokens")]
[MembershipRoute("codes")]
public class TokenCodesController : ControllerBase
{
    #region Services
	
	private readonly ITokenCodeService _tokenCodeService;
	private readonly IUtilizerService _utilizerService;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="tokenCodeService"></param>
	/// <param name="utilizerService"></param>
	public TokenCodesController(ITokenCodeService tokenCodeService, IUtilizerService utilizerService)
	{
		this._tokenCodeService = tokenCodeService;
		this._utilizerService = utilizerService;
	}
	
	#endregion
	
	#region Methods
	
	/// <summary>Generate a token code</summary>
	/// <remarks>Starts the code flow for devices without a keyboard: generates a short code by the membership's code policy, to be approved by a signed in user and exchanged for a token by the device.</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPost]
	[RbacAction(Rbac.CrudActions.Create)]
	[ProducesResponseType<TokenCode>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<TokenCode>> GenerateCode([FromRoute] string membershipId, CancellationToken cancellationToken = default)
	{
		return this.Ok(await this._tokenCodeService.CreateAsync(membershipId, cancellationToken: cancellationToken));
	}
	
	/// <summary>Approve a token code</summary>
	/// <remarks>Approves the code for the user of the Bearer token, so that the device can get a token for that user.</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="code">Token code shown on the device</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpGet("approve/{code}")]
	[RbacAction(Rbac.CrudActions.Create)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> AuthorizeCode([FromRoute] string membershipId, [FromRoute] string code, CancellationToken cancellationToken = default)
	{
		var token = this.GetToken();
		if (token.TokenType != SupportedTokenTypes.Bearer)
		{
			throw ErtisAuthException.UnsupportedTokenType();
		}
		
		var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
		await this._tokenCodeService.AuthorizeCodeAsync(code, membershipId, utilizer, cancellationToken: cancellationToken);
		return this.Ok();
	}
	
	/// <summary>Exchange a token code for a token</summary>
	/// <remarks>Anonymous: returns a token for the user who approved the code. Fails while the code is not approved yet or after it expires.</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="code">Approved token code</param>
	[Unauthorized]
	[HttpGet("generate-token/{code}")]
	[ProducesResponseType<BearerToken>(StatusCodes.Status201Created)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GenerateToken([FromRoute] string membershipId, [FromRoute] string code)
	{
		var token = await this._tokenCodeService.GenerateTokenAsync(code, membershipId);
		return this.Created($"{this.Request.Scheme}://{this.Request.Host}", token);
	}
	
	#endregion
}