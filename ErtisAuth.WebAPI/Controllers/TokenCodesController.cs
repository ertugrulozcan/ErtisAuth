using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Extensions.Authorization.Annotations;
using ErtisAuth.Identity.Attributes;
using ErtisAuth.WebAPI.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers;

[ApiController]
[Authorized]
[RbacResource("tokens")]
[Route("api/v{v:apiVersion}/memberships/{membershipId}/codes")]
public class TokenCodesController : ControllerBase
{
    #region Services

	private readonly ITokenCodeService _tokenCodeService;
	
	#endregion

	#region Constructors

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="tokenCodeService"></param>
	public TokenCodesController(ITokenCodeService tokenCodeService)
	{
		this._tokenCodeService = tokenCodeService;
	}

	#endregion
	
	#region Methods
	
	[HttpPost]
	[RbacAction(Rbac.CrudActions.Create)]
	public async Task<ActionResult<TokenCode>> GenerateCode([FromRoute] string membershipId, CancellationToken cancellationToken = default)
	{
		return this.Ok(await this._tokenCodeService.CreateAsync(membershipId, cancellationToken: cancellationToken));
	}
	
	[HttpGet("approve/{code}")]
	[RbacAction(Rbac.CrudActions.Create)]
	public async Task<ActionResult<TokenCode>> AuthorizeCode([FromRoute] string membershipId, [FromRoute] string code, CancellationToken cancellationToken = default)
	{
		var token = this.GetToken();
		if (token.TokenType != SupportedTokenTypes.Bearer)
		{
			throw ErtisAuthException.UnsupportedTokenType();
		}

		var utilizer = this.GetUtilizer();
		await this._tokenCodeService.AuthorizeCodeAsync(code, utilizer, membershipId, cancellationToken: cancellationToken);
		return this.Ok();
	}
	
	[Unauthorized]
	[HttpGet("generate-token/{code}")]
	public async Task<IActionResult> GenerateToken([FromRoute] string membershipId, [FromRoute] string code)
	{
		var token = await this._tokenCodeService.GenerateTokenAsync(code, membershipId);
		if (token != null)
		{
			return this.Created($"{this.Request.Scheme}://{this.Request.Host}", token);
		}
		else
		{
			return this.InvalidCredentials();
		}
	}
	
	private TokenBase GetToken()
	{
		var stringToken = this.GetTokenFromHeader(out var tokenTypeStr);
		if (string.IsNullOrEmpty(stringToken))
		{
			throw ErtisAuthException.AuthorizationHeaderMissing();
		}

		if (!TokenTypeExtensions.TryParseTokenType(tokenTypeStr, out var tokenType))
		{
			throw ErtisAuthException.UnsupportedTokenType();
		}

		TokenBase token = tokenType switch
		{
			SupportedTokenTypes.None => throw ErtisAuthException.UnsupportedTokenType(),
			SupportedTokenTypes.Basic => new BasicToken(stringToken),
			SupportedTokenTypes.Bearer => BearerToken.CreateTemp(stringToken),
			_ => throw ErtisAuthException.UnsupportedTokenType()
		};

		return token;
	}
	
	#endregion
}