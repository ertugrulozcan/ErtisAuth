using System.Text.Json;
using Ertis.Core.Models;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Integrations.OAuth.Apple;
using ErtisAuth.Integrations.OAuth.Core;
using ErtisAuth.Integrations.OAuth.Facebook;
using ErtisAuth.Integrations.OAuth.Google;
using ErtisAuth.Integrations.OAuth.Microsoft;
using ErtisAuth.Extensions.AspNetCore.Extensions;
using ErtisAuth.WebAPI.Models.Tokens;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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
	private readonly JsonSerializerOptions _jsonSerializerOptions;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="tokenService"></param>
	/// <param name="userService"></param>
	/// <param name="providerService"></param>
	/// <param name="oneTimePasswordService"></param>
	/// <param name="jsonOptions"></param>
	public TokensController(
		ITokenService tokenService, 
		IUserService userService, 
		IProviderService providerService, 
		IOneTimePasswordService oneTimePasswordService,
		IOptions<JsonOptions> jsonOptions)
	{
		this._tokenService = tokenService;
		this._userService = userService;
		this._providerService = providerService;
		this._oneTimePasswordService = oneTimePasswordService;
		this._jsonSerializerOptions = jsonOptions.Value.JsonSerializerOptions;
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
	public async Task<IActionResult> Me(CancellationToken cancellationToken = default)
	{
		var token = this.GetToken();
		var utilizer = await this.GetTokenOwnerUtilizerAsync(token, cancellationToken: cancellationToken);
		if (utilizer != null)
		{
			if (token.TokenType == SupportedTokenTypes.Bearer)
			{
				var user = await this._userService.GetAsync(utilizer.Id, utilizer.MembershipId, cancellationToken: cancellationToken);
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
	public async Task<IActionResult> WhoAmI(CancellationToken cancellationToken = default)
	{
		var token = this.GetToken();
		var utilizer = await this.GetTokenOwnerUtilizerAsync(token, cancellationToken: cancellationToken);
		if (utilizer != null)
		{
			if (token.TokenType == SupportedTokenTypes.Bearer)
			{
				var user = await this._userService.GetAsync(utilizer.Id, utilizer.MembershipId, cancellationToken: cancellationToken);
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
	
	[NonAction]
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
				throw ErtisAuthException.InvalidCredentialsOrMissingToken();
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
	public async Task<IActionResult> VerifyToken(CancellationToken cancellationToken = default)
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
		
		var validationResult = await this._tokenService.VerifyTokenAsync(token, tokenType, false, cancellationToken: cancellationToken);
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
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPost]
	[Route("verify-token")]
	[ProducesResponseType<ITokenValidationResult>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ITokenValidationResult>(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> VerifyToken([FromBody] VerifyTokenFormModel model, CancellationToken cancellationToken = default)
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
		
		var validationResult = await this._tokenService.VerifyTokenAsync(token, tokenType, false, cancellationToken: cancellationToken);
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
	public async Task<IActionResult> RefreshToken(CancellationToken cancellationToken = default)
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
		
		var token = await this._tokenService.RefreshTokenAsync(refreshToken, revokeBefore, cancellationToken: cancellationToken);
		return this.Created($"{this.Request.Scheme}://{this.Request.Host}", token);
	}
	
	/// <summary>Refresh a token (body)</summary>
	/// <remarks>Like the GET endpoint; the refresh token can also be given in the body instead of the Authorization header.</remarks>
	/// <param name="model">Refresh token</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPost]
	[Route("refresh-token")]
	[ProducesResponseType<BearerToken>(StatusCodes.Status201Created)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenFormModel model, CancellationToken cancellationToken = default)
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
		
		var token = await this._tokenService.RefreshTokenAsync(refreshToken, revokeBefore, cancellationToken: cancellationToken);
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
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPost]
	[Route("verify-otp")]
	[ProducesResponseType<ResetPasswordToken>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> VerifyOneTimePassword([FromBody] GenerateTokenFormModel model, CancellationToken cancellationToken = default)
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
		
		var otp = await this._oneTimePasswordService.VerifyOtpAsync(username, password, membershipId, host, cancellationToken: cancellationToken);
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
	
	/// <summary>Sign in with a provider</summary>
	/// <remarks>
	/// Signs in (or signs up) the user with the login result of the provider whose slug is given in the url (several
	/// providers of the same type can be used, e.g. for different apps). The body is the login result of the provider's type:
	/// 
	/// - **Facebook:** <c>{ "appId", "user": { "id", "first_name", "last_name", "email", "accessToken", ... } }</c>; <c>limited_flow=true</c> is for Facebook Limited Login.
	/// - **Google:** <c>{ "clientId", "token": { "idToken", "clientId", ... } }</c>
	/// - **Microsoft:** <c>{ "clientId", "token": { "accessToken", ... } }</c>
	/// - **Apple, AppleNative:** <c>{ "user": { "name": { "firstName", "lastName" }, "email" }, "authorization": { "code", "id_token" } }</c>
	/// 
	/// The membership is given by the <c>X-Ertis-Alias</c> header (or <c>Membership</c>, <c>MembershipId</c>). The optional <c>X-IpAddress</c> and <c>X-UserAgent</c> headers are stored with the active token.
	/// **Note:** an unknown slug answers 403 (<c>ProviderNotConfigured</c>), like a provider that was never set up.
	/// </remarks>
	/// <param name="slug">Provider slug</param>
	/// <param name="body">Login result of the provider</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPost]
	[Route("oauth/{slug}/login")]
	[ProducesResponseType<BearerToken>(StatusCodes.Status201Created)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status409Conflict)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status501NotImplemented)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status503ServiceUnavailable)]
	public async Task<IActionResult> ProviderLogin([FromRoute] string slug, [FromBody] JsonElement body, CancellationToken cancellationToken = default)
	{
		var membershipId = this.GetMembershipId();
		if (string.IsNullOrEmpty(membershipId))
		{
			return this.MembershipIdRequired();
		}
		
		var provider = await this._providerService.GetBySlugAsync(slug, membershipId, cancellationToken: cancellationToken);
		if (provider == null)
		{
			throw ErtisAuthException.ProviderNotConfigured();
		}
		
		var request = this.ReadLoginRequest(provider, body);
		
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
				provider, 
				request, 
				ipAddress: ipAddress, 
				userAgent: userAgent,
				cancellationToken: cancellationToken
			)
		);
	}
	
	/// <summary>
	/// The body is bound by the provider's type, which is known only after the provider is read by its slug.
	/// </summary>
	private IProviderLoginRequest ReadLoginRequest(Provider provider, JsonElement body)
	{
		try
		{
			switch (provider)
			{
				case FacebookProvider:
					var facebookLoginRequest = body.Deserialize<FacebookLoginRequest>(this._jsonSerializerOptions) ?? throw ErtisAuthException.InvalidProviderLoginRequest(provider.Type.ToString());
					facebookLoginRequest.IsLimited = this.Request.Query.TryGetValue("limited_flow", out var limitedFlow) && limitedFlow == "true";
					return facebookLoginRequest;
				case GoogleProvider:
					return body.Deserialize<GoogleLoginRequest>(this._jsonSerializerOptions) ?? throw ErtisAuthException.InvalidProviderLoginRequest(provider.Type.ToString());
				case MicrosoftProvider:
					return body.Deserialize<MicrosoftLoginRequest>(this._jsonSerializerOptions) ?? throw ErtisAuthException.InvalidProviderLoginRequest(provider.Type.ToString());
				case BaseAppleProvider:
					var appleLoginModel = body.Deserialize<AppleLoginModel>(this._jsonSerializerOptions) ?? throw ErtisAuthException.InvalidProviderLoginRequest(provider.Type.ToString());
					return appleLoginModel.ToLoginRequest(provider is AppleNativeProvider);
				default:
					throw ErtisAuthException.UnsupportedProvider();
			}
		}
		catch (JsonException ex)
		{
			throw ErtisAuthException.InvalidProviderLoginRequest(provider.Type.ToString(), ex.Message);
		}
		catch (ArgumentException ex)
		{
			// Apple: an id_token that can't be read
			throw ErtisAuthException.InvalidProviderLoginRequest(provider.Type.ToString(), ex.Message);
		}
	}
	
	#endregion
}