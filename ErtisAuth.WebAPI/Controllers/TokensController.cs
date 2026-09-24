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
	
	[HttpGet]
	[Route("me")]
	public async Task<IActionResult> Me()
	{
		var token = this.GetToken();
		var utilizer = await this.GetTokenOwnerUtilizerAsync(token);
		if (utilizer != null)
		{
			if (token.TokenType == SupportedTokenTypes.Bearer)
			{
				var user = await this._userService.GetAsync(utilizer.MembershipId, utilizer.Id);
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
	
	[HttpGet]
	[Route("whoami")]
	public async Task<IActionResult> WhoAmI()
	{
		var token = this.GetToken();
		var utilizer = await this.GetTokenOwnerUtilizerAsync(token);
		if (utilizer != null)
		{
			if (token.TokenType == SupportedTokenTypes.Bearer)
			{
				var user = await this._userService.GetAsync(utilizer.MembershipId, utilizer.Id);
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
	
	[HttpPost]
	[Route("generate-token")]
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
	
	[HttpGet]
	[Route("verify-token")]
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
	
	[HttpPost]
	[Route("verify-token")]
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
	
	[HttpGet]
	[Route("refresh-token")]
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
	
	[HttpPost]
	[Route("refresh-token")]
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
	
	[HttpGet]
	[Route("revoke-token")]
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
	
	[HttpPost]
	[Route("revoke-token")]
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
	
	[HttpPost]
	[Route("verify-otp")]
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
	
	[HttpPost]
	[Route("oauth/facebook/login")]
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
	
	[HttpPost]
	[Route("oauth/google/login")]
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
	
	[HttpPost]
	[Route("oauth/microsoft/login")]
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
	
	[HttpPost]
	[Route("oauth/apple/login")]
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