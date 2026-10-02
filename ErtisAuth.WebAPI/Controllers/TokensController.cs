using Ertis.Core.Models;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Integrations.OAuth.Apple;
using ErtisAuth.Integrations.OAuth.Facebook;
using ErtisAuth.Integrations.OAuth.Google;
using ErtisAuth.Integrations.OAuth.Microsoft;
using ErtisAuth.Extensions.AspNetCore.Extensions;
using ErtisAuth.WebAPI.Models.Tokens;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers;

[ApiController]
[Tags("Tokens")]
public class TokensController : ControllerBase
{
	#region Services
	
	private readonly ITokenService _tokenService;
	private readonly IUserService _userService;
	private readonly IProviderService _providerService;
	private readonly IOneTimePasswordService _oneTimePasswordService;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="tokenService"></param>
	/// <param name="userService"></param>
	/// <param name="providerService"></param>
	/// <param name="oneTimePasswordService"></param>
	public TokensController(
		ITokenService tokenService, 
		IUserService userService, 
		IProviderService providerService, 
		IOneTimePasswordService oneTimePasswordService)
	{
		this._tokenService = tokenService;
		this._userService = userService;
		this._providerService = providerService;
		this._oneTimePasswordService = oneTimePasswordService;
	}
	
	#endregion
	
	#region Methods
	
	/// <summary>Get the token owner</summary>
	/// <remarks>Returns the user of a Bearer token, or the application of a Basic token. Same as whoami.</remarks>
	[HttpGet]
	[Route("me")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Me()
	{
		var token = this.GetToken();
		var utilizer = await this.GetTokenOwnerUtilizerAsync(token);
		if (utilizer != null)
		{
			if (token.TokenType == SupportedTokenTypes.Bearer)
			{
				var user = await this._userService.GetAsync(utilizer.Id, utilizer.MembershipId);
				if (user != null)
				{
					return this.Ok(user);
				}
				else
				{
					return this.UserNotFound(utilizer.Id);
				}
			}
			else
			{
				return this.Ok(utilizer);
			}
		}
		else
		{
			return this.InvalidToken();
		}
	}
	
	/// <summary>Who am I</summary>
	/// <remarks>Returns the user of a Bearer token, or the application of a Basic token. Same as me.</remarks>
	[HttpGet]
	[Route("whoami")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> WhoAmI()
	{
		var token = this.GetToken();
		var utilizer = await this.GetTokenOwnerUtilizerAsync(token);
		if (utilizer != null)
		{
			if (token.TokenType == SupportedTokenTypes.Bearer)
			{
				var user = await this._userService.GetAsync(utilizer.Id, utilizer.MembershipId);
				if (user != null)
				{
					return this.Ok(user);
				}
				else
				{
					return this.UserNotFound(utilizer.Id);
				}
			}
			else
			{
				return this.Ok(utilizer);
			}
		}
		else
		{
			return this.InvalidToken();
		}
	}
	
	private TokenBase GetToken()
	{
		var stringToken = this.GetTokenFromHeader(out var tokenTypeStr);
		if (string.IsNullOrEmpty(stringToken))
		{
			throw ErtisAuthException.AuthorizationHeaderMissing();
		}
		
		if (tokenTypeStr == null || !TokenTypeExtensions.TryParseTokenType(tokenTypeStr, out var tokenType))
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
	
	private async Task<IUtilizer?> GetTokenOwnerUtilizerAsync(TokenBase token, CancellationToken cancellationToken = default)
	{
		return token.TokenType switch
		{
			SupportedTokenTypes.None => throw ErtisAuthException.UnsupportedTokenType(),
			SupportedTokenTypes.Basic => await this._tokenService.WhoAmIAsync((BasicToken) token, cancellationToken: cancellationToken),
			SupportedTokenTypes.Bearer => await this._tokenService.WhoAmIAsync((BearerToken) token, cancellationToken: cancellationToken),
			_ => throw ErtisAuthException.UnsupportedTokenType()
		};
	}
	
	/// <summary>Generate a token</summary>
	/// <remarks>Signs in with <c>username</c> (or email address) and <c>password</c> and returns an access and a refresh token. Without credentials, a Bearer token in the Authorization header and <c>scopes</c> in the body return a scoped token of the same user. The membership is given by the <c>X-Ertis-Alias</c> header (or <c>Membership</c>, <c>MembershipId</c>). The optional <c>X-IpAddress</c> and <c>X-UserAgent</c> headers are stored with the active token.</remarks>
	/// <param name="model">Credentials or scopes</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPost]
	[Route("generate-token")]
	[ProducesResponseType<BearerToken>(StatusCodes.Status201Created)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> GenerateToken([FromBody] GenerateTokenFormModel model, CancellationToken cancellationToken = default)
	{
		var membershipId = this.GetMembershipId();
		if (string.IsNullOrEmpty(membershipId))
		{
			return this.MembershipIdRequired();
		}
		
		var username = model.Username;
		var password = model.Password;
		
		string? ipAddress = null;
		if (this.Request.Headers.TryGetValue("X-IpAddress", out var ipAddressHeader))
		{
			ipAddress = ipAddressHeader.ToString();
		}
		
		string? userAgent = null;
		if (this.Request.Headers.TryGetValue("X-UserAgent", out var userAgentHeader))
		{
			userAgent = userAgentHeader.ToString();
		}
		
		if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
		{
			var token = await this._tokenService.GenerateTokenAsync(
				username, 
				password, 
				membershipId, 
				ipAddress: ipAddress, 
				userAgent: userAgent, 
				cancellationToken: cancellationToken);
			
			return this.Created($"{this.Request.Scheme}://{this.Request.Host}", token);
		}
		else
		{
			var token = this.GetTokenFromHeader(out var tokenTypeStr);
			if (string.IsNullOrEmpty(token))
			{
				throw ErtisAuthException.BearerTokenRequired();
			}
			
			var scopes = model.Scopes?.Select(x => x.Trim()).ToArray();
			if ((scopes == null || scopes.Length == 0) && string.IsNullOrEmpty(token))
			{
				return this.InvalidCredentials();
			}
			
			if (tokenTypeStr == null || !TokenTypeExtensions.TryParseTokenType(tokenTypeStr, out var tokenType))
			{
				throw ErtisAuthException.UnsupportedTokenType();
			}
			else if (tokenType != SupportedTokenTypes.Bearer)
			{
				throw ErtisAuthException.BearerTokenRequired();
			}
			
			var generatedToken = await this._tokenService.GenerateTokenAsync(token, scopes, membershipId, cancellationToken: cancellationToken);
			return this.Created($"{this.Request.Scheme}://{this.Request.Host}", generatedToken);
		}
	}
	
	/// <summary>Verify a token</summary>
	/// <remarks>Verifies the token in the Authorization header (Bearer or Basic). Answers 200 with the validation result, or 401 when the token is not valid.</remarks>
	[HttpGet]
	[Route("verify-token")]
	[ProducesResponseType<ITokenValidationResult>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ITokenValidationResult>(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> VerifyToken()
	{
		var token = this.GetTokenFromHeader(out var tokenTypeStr);
		if (string.IsNullOrEmpty(token))
		{
			return this.AuthorizationHeaderMissing();
		}
		
		if (tokenTypeStr == null || !TokenTypeExtensions.TryParseTokenType(tokenTypeStr, out var tokenType))
		{
			throw ErtisAuthException.UnsupportedTokenType();	
		}
		
		var validationResult = await this._tokenService.VerifyTokenAsync(token, tokenType, false);
		if (validationResult.IsValidated)
		{
			return this.Ok(validationResult);
		}
		else
		{
			return this.Unauthorized(validationResult);
		}
	}
	
	/// <summary>Verify a token (body)</summary>
	/// <remarks>Like the GET endpoint; the token can also be given in the body (with its type, e.g. <c>Bearer ...</c>) instead of the Authorization header.</remarks>
	/// <param name="model">Token to verify</param>
	[HttpPost]
	[Route("verify-token")]
	[ProducesResponseType<ITokenValidationResult>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ITokenValidationResult>(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> VerifyToken([FromBody] VerifyTokenFormModel model)
	{
		var token = this.GetTokenFromHeader(out var tokenTypeStr);
		if (string.IsNullOrEmpty(token))
		{
			token = TokenBase.ExtractToken(model.Token, out tokenTypeStr);
		}
		
		if (tokenTypeStr == null || !TokenTypeExtensions.TryParseTokenType(tokenTypeStr, out var tokenType))
		{
			throw ErtisAuthException.UnsupportedTokenType();	
		}
		
		if (string.IsNullOrEmpty(token))
		{
			return this.AuthorizationHeaderMissing();
		}
		
		var validationResult = await this._tokenService.VerifyTokenAsync(token, tokenType, false);
		if (validationResult.IsValidated)
		{
			return this.Ok(validationResult);
		}
		else
		{
			return this.Unauthorized(validationResult);
		}
	}
	
	/// <summary>Refresh a token</summary>
	/// <remarks>Returns a new token for the refresh token in the Authorization header. The refresh token is revoked (usable once) unless <c>revoke=false</c> is given. **Note:** the previous access token is not revoked; it stays valid until it expires.</remarks>
	[HttpGet]
	[Route("refresh-token")]
	[ProducesResponseType<BearerToken>(StatusCodes.Status201Created)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> RefreshToken()
	{
		var refreshToken = this.GetTokenFromHeader(out _);
		if (string.IsNullOrEmpty(refreshToken))
		{
			return this.AuthorizationHeaderMissing();
		}
		
		var revokeBefore = true;
		if (this.Request.Query.ContainsKey("revoke"))
		{
			revokeBefore = this.Request.Query["revoke"] == "true";
		}
		
		var token = await this._tokenService.RefreshTokenAsync(refreshToken, revokeBefore);
		return this.Created($"{this.Request.Scheme}://{this.Request.Host}", token);
	}
	
	/// <summary>Refresh a token (body)</summary>
	/// <remarks>Like the GET endpoint; the refresh token can also be given in the body instead of the Authorization header.</remarks>
	/// <param name="model">Refresh token</param>
	[HttpPost]
	[Route("refresh-token")]
	[ProducesResponseType<BearerToken>(StatusCodes.Status201Created)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenFormModel model)
	{
		var refreshToken = this.GetTokenFromHeader(out _);
		if (string.IsNullOrEmpty(refreshToken))
		{
			refreshToken = model.Token;
		}
		
		if (string.IsNullOrEmpty(refreshToken))
		{
			throw ErtisAuthException.RefreshTokenRequired();
		}
		
		var revokeBefore = true;
		if (this.Request.Query.ContainsKey("revoke"))
		{
			revokeBefore = this.Request.Query["revoke"] == "true";
		}
		
		var token = await this._tokenService.RefreshTokenAsync(refreshToken, revokeBefore);
		return this.Created($"{this.Request.Scheme}://{this.Request.Host}", token);
	}
	
	/// <summary>Revoke a token</summary>
	/// <remarks>Signs out: revokes the token in the Authorization header. <c>logout-all=true</c> revokes all tokens of the user (every device). Answers 401 when the token could not be revoked.</remarks>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpGet]
	[Route("revoke-token")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> RevokeToken(CancellationToken cancellationToken = default)
	{
		var token = this.GetTokenFromHeader(out _);
		if (string.IsNullOrEmpty(token))
		{
			return this.AuthorizationHeaderMissing();
		}
		
		var logoutFromAllDevices = false;
		if (this.Request.Query.ContainsKey("logout-all"))
		{
			bool.TryParse(this.Request.Query["logout-all"], out logoutFromAllDevices);
		}
		
		if (await this._tokenService.RevokeTokenAsync(token, logoutFromAllDevices, cancellationToken: cancellationToken))
		{
			await this._providerService.LogoutAsync(token, cancellationToken: cancellationToken);
			return this.NoContent();
		}
		else
		{
			return this.Unauthorized();
		}
	}
	
	/// <summary>Revoke a token (body)</summary>
	/// <remarks>Like the GET endpoint; the token can also be given in the body instead of the Authorization header.</remarks>
	/// <param name="model">Token to revoke</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPost]
	[Route("revoke-token")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenFormModel model, CancellationToken cancellationToken = default)
	{
		var token = this.GetTokenFromHeader(out _);
		if (string.IsNullOrEmpty(token))
		{
			token = model.Token;
		}
		
		if (string.IsNullOrEmpty(token))
		{
			throw ErtisAuthException.BearerTokenRequired();
		}
		
		var logoutFromAllDevices = false;
		if (this.Request.Query.ContainsKey("logout-all"))
		{
			bool.TryParse(this.Request.Query["logout-all"], out logoutFromAllDevices);
		}
		
		if (await this._tokenService.RevokeTokenAsync(token, logoutFromAllDevices, cancellationToken: cancellationToken))
		{
			await this._providerService.LogoutAsync(token, cancellationToken: cancellationToken);
			return this.NoContent();
		}
		else
		{
			return this.Unauthorized();
		}
	}
	
	/// <summary>Verify a one time password</summary>
	/// <remarks>Verifies the one time password (<c>password</c>) of the user (<c>username</c> or email address) and returns a reset token, to set a new password with the set password endpoint. The <c>X-Host</c> header must match the OTP host of the membership. The membership is given by the <c>X-Ertis-Alias</c> header (or <c>Membership</c>, <c>MembershipId</c>). **Note:** the returned token is not an access token. Failed attempts are limited; the one time password is deleted when they are used up.</remarks>
	/// <param name="model">Username and one time password</param>
	[HttpPost]
	[Route("verify-otp")]
	[ProducesResponseType<ResetPasswordToken>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> VerifyOneTimePassword([FromBody] GenerateTokenFormModel model)
	{
		var membershipId = this.GetMembershipId();
		if (string.IsNullOrEmpty(membershipId))
		{
			return this.MembershipIdRequired();
		}
		
		var username = model.Username;
		var password = model.Password;
		var host = this.Request.Headers.TryGetValue("X-Host", out var hostStringValue) ? hostStringValue.ToString() : null;
		
		if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
		{
			return this.InvalidCredentials();
		}
		
		var otp = await this._oneTimePasswordService.VerifyOtpAsync(username, password, membershipId, host);
		if (otp != null)
		{
			return this.Ok(otp.Token);
		}
		else
		{
			return this.InvalidCredentials();
		}
	}
	
	#endregion
	
	#region Provider Methods
	
	/// <summary>Sign in with Facebook</summary>
	/// <remarks>Signs in (or signs up) the user with a Facebook login result. <c>limited_flow=true</c> is for Facebook Limited Login. The membership is given by the <c>X-Ertis-Alias</c> header (or <c>Membership</c>, <c>MembershipId</c>). The optional <c>X-IpAddress</c> and <c>X-UserAgent</c> headers are stored with the active token.</remarks>
	/// <param name="request">Facebook login result</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPost]
	[Route("oauth/facebook/login")]
	[ProducesResponseType<BearerToken>(StatusCodes.Status201Created)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status409Conflict)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status501NotImplemented)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status503ServiceUnavailable)]
	public async Task<IActionResult> FacebookLogin([FromBody] FacebookLoginRequest request, CancellationToken cancellationToken = default)
	{
		var membershipId = this.GetMembershipId();
		if (string.IsNullOrEmpty(membershipId))
		{
			return this.MembershipIdRequired();
		}
		
		string? ipAddress = null;
		if (this.Request.Headers.TryGetValue("X-IpAddress", out var ipAddressHeader))
		{
			ipAddress = ipAddressHeader.ToString();
		}
		
		string? userAgent = null;
		if (this.Request.Headers.TryGetValue("X-UserAgent", out var userAgentHeader))
		{
			userAgent = userAgentHeader.ToString();
		}
		
		if (this.Request.Query.ContainsKey("limited_flow") && this.Request.Query["limited_flow"] == "true")
		{
			request.IsLimited = true;
		}
		
		return this.Created(
			$"{this.Request.Scheme}://{this.Request.Host}", 
			await this._providerService.LoginAsync(
				request, 
				membershipId, 
				ipAddress: ipAddress, 
				userAgent: userAgent,
				cancellationToken: cancellationToken
			)
		);
	}
	
	/// <summary>Sign in with Google</summary>
	/// <remarks>Signs in (or signs up) the user with a Google login result. The membership is given by the <c>X-Ertis-Alias</c> header (or <c>Membership</c>, <c>MembershipId</c>). The optional <c>X-IpAddress</c> and <c>X-UserAgent</c> headers are stored with the active token.</remarks>
	/// <param name="request">Google login result</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPost]
	[Route("oauth/google/login")]
	[ProducesResponseType<BearerToken>(StatusCodes.Status201Created)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status409Conflict)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status501NotImplemented)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status503ServiceUnavailable)]
	public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request, CancellationToken cancellationToken = default)
	{
		var membershipId = this.GetMembershipId();
		if (string.IsNullOrEmpty(membershipId))
		{
			return this.MembershipIdRequired();
		}
		
		string? ipAddress = null;
		if (this.Request.Headers.TryGetValue("X-IpAddress", out var ipAddressHeader))
		{
			ipAddress = ipAddressHeader.ToString();
		}
		
		string? userAgent = null;
		if (this.Request.Headers.TryGetValue("X-UserAgent", out var userAgentHeader))
		{
			userAgent = userAgentHeader.ToString();
		}
		
		return this.Created(
			$"{this.Request.Scheme}://{this.Request.Host}", 
			await this._providerService.LoginAsync(
				request, 
				membershipId, 
				ipAddress: ipAddress, 
				userAgent: userAgent,
				cancellationToken: cancellationToken
			)
		);
	}
	
	/// <summary>Sign in with Microsoft</summary>
	/// <remarks>Signs in (or signs up) the user with a Microsoft login result. The membership is given by the <c>X-Ertis-Alias</c> header (or <c>Membership</c>, <c>MembershipId</c>). The optional <c>X-IpAddress</c> and <c>X-UserAgent</c> headers are stored with the active token.</remarks>
	/// <param name="request">Microsoft login result</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPost]
	[Route("oauth/microsoft/login")]
	[ProducesResponseType<BearerToken>(StatusCodes.Status201Created)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status409Conflict)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status501NotImplemented)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status503ServiceUnavailable)]
	public async Task<IActionResult> MicrosoftLogin([FromBody] MicrosoftLoginRequest request, CancellationToken cancellationToken = default)
	{
		var membershipId = this.GetMembershipId();
		if (string.IsNullOrEmpty(membershipId))
		{
			return this.MembershipIdRequired();
		}
		
		string? ipAddress = null;
		if (this.Request.Headers.TryGetValue("X-IpAddress", out var ipAddressHeader))
		{
			ipAddress = ipAddressHeader.ToString();
		}
		
		string? userAgent = null;
		if (this.Request.Headers.TryGetValue("X-UserAgent", out var userAgentHeader))
		{
			userAgent = userAgentHeader.ToString();
		}
		
		return this.Created(
			$"{this.Request.Scheme}://{this.Request.Host}", 
			await this._providerService.LoginAsync(
				request, 
				membershipId, 
				ipAddress: ipAddress, 
				userAgent: userAgent,
				cancellationToken: cancellationToken
			)
		);
	}
	
	/// <summary>Sign in with Apple</summary>
	/// <remarks>Signs in (or signs up) the user with a Sign in with Apple result. The membership is given by the <c>X-Ertis-Alias</c> header (or <c>Membership</c>, <c>MembershipId</c>). The optional <c>X-IpAddress</c> and <c>X-UserAgent</c> headers are stored with the active token.</remarks>
	/// <param name="request">Sign in with Apple result</param>
	/// <param name="platform">Client platform: <c>ios</c>, <c>android</c> or <c>web</c></param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPost]
	[Route("oauth/apple/login")]
	[ProducesResponseType<BearerToken>(StatusCodes.Status201Created)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status409Conflict)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status501NotImplemented)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status503ServiceUnavailable)]
	public async Task<IActionResult> AppleLogin([FromBody] AppleLoginModel request, [FromQuery] string platform, CancellationToken cancellationToken = default)
	{
		var membershipId = this.GetMembershipId();
		if (string.IsNullOrEmpty(membershipId))
		{
			return this.MembershipIdRequired();
		}
		
		var platforms = new []
		{
			"ios", "android", "web"
		};
		
		if (!string.IsNullOrEmpty(platform) && !platforms.Contains(platform))
		{
			return this.UnknownPlatform(platform);
		}
		
		string? ipAddress = null;
		if (this.Request.Headers.TryGetValue("X-IpAddress", out var ipAddressHeader))
		{
			ipAddress = ipAddressHeader.ToString();
		}
		
		string? userAgent = null;
		if (this.Request.Headers.TryGetValue("X-UserAgent", out var userAgentHeader))
		{
			userAgent = userAgentHeader.ToString();
		}
		
		return this.Created(
			$"{this.Request.Scheme}://{this.Request.Host}", 
			await this._providerService.LoginAsync(
				request.ToLoginRequest(platform == "ios"), 
				membershipId, 
				ipAddress: ipAddress, 
				userAgent: userAgent,
				cancellationToken: cancellationToken
			)
		);
	}
	
	#endregion
}