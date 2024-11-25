using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Integrations.OAuth.Apple;
using ErtisAuth.Integrations.OAuth.Facebook;
using ErtisAuth.Integrations.OAuth.Google;
using ErtisAuth.Integrations.OAuth.Microsoft;
using ErtisAuth.WebAPI.Extensions;
using ErtisAuth.WebAPI.Models.Request.Tokens;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers
{
	[ApiController]
	[Route("api/v{v:apiVersion}")]
	public class TokensController : ControllerBase
	{
		#region Services

		private readonly ITokenService tokenService;
		private readonly IUserService userService;
		private readonly IProviderService providerService;
		private readonly IOneTimePasswordService oneTimePasswordService;

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
			this.tokenService = tokenService;
			this.userService = userService;
			this.providerService = providerService;
			this.oneTimePasswordService = oneTimePasswordService;
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
					var user = await this.userService.GetAsync(utilizer.MembershipId, utilizer.Id);
					return this.Ok(user);
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
				return this.Ok(utilizer);
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

		private async Task<IUtilizer> GetTokenOwnerUtilizerAsync(TokenBase token, CancellationToken cancellationToken = default)
		{
			return token.TokenType switch
			{
				SupportedTokenTypes.None => throw ErtisAuthException.UnsupportedTokenType(),
				SupportedTokenTypes.Basic => await this.tokenService.WhoAmIAsync(token as BasicToken, cancellationToken: cancellationToken),
				SupportedTokenTypes.Bearer => await this.tokenService.WhoAmIAsync(token as BearerToken, cancellationToken: cancellationToken),
				_ => throw ErtisAuthException.UnsupportedTokenType()
			};
		}
		
		[HttpPost]
		[Route("generate-token")]
		public async Task<IActionResult> GenerateToken([FromBody] GenerateTokenFormModel model)
		{
			var membershipId = this.GetXErtisAlias();
			if (string.IsNullOrEmpty(membershipId))
			{
				return this.XErtisAliasMissing();
			}

			string username = model.Username;
			string password = model.Password;

			string ipAddress = null;
			if (this.Request.Headers.TryGetValue("X-IpAddress", out var ipAddressHeader))
			{
				ipAddress = ipAddressHeader.ToString();
			}
			
			string userAgent = null;
			if (this.Request.Headers.TryGetValue("X-UserAgent", out var userAgentHeader))
			{
				userAgent = userAgentHeader.ToString();
			}
			
			var token = await this.tokenService.GenerateTokenAsync(username, password, membershipId, ipAddress: ipAddress, userAgent: userAgent);
			if (token != null)
			{
				return this.Created($"{this.Request.Scheme}://{this.Request.Host}", token);
			}
			else
			{
				return this.InvalidCredentials();
			}
		}
		
		[HttpGet]
		[Route("verify-token")]
		public async Task<IActionResult> VerifyToken()
		{
			string token = this.GetTokenFromHeader(out string tokenTypeStr);
			if (string.IsNullOrEmpty(token))
			{
				return this.AuthorizationHeaderMissing();
			}
			
			if (!TokenTypeExtensions.TryParseTokenType(tokenTypeStr, out var tokenType))
			{
				throw ErtisAuthException.UnsupportedTokenType();	
			}

			var validationResult = await this.tokenService.VerifyTokenAsync(token, tokenType, false);
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
			string token = this.GetTokenFromHeader(out string tokenTypeStr);
			if (string.IsNullOrEmpty(token))
			{
				token = model.Token;
			}
			
			if (!TokenTypeExtensions.TryParseTokenType(tokenTypeStr, out var tokenType))
			{
				throw ErtisAuthException.UnsupportedTokenType();	
			}

			if (string.IsNullOrEmpty(token))
			{
				return this.AuthorizationHeaderMissing();
			}

			var validationResult = await this.tokenService.VerifyTokenAsync(token, tokenType, false);
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
			string refreshToken = this.GetTokenFromHeader(out string _);
			if (string.IsNullOrEmpty(refreshToken))
			{
				return this.AuthorizationHeaderMissing();
			}

			bool revokeBefore = true;
			if (this.Request.Query.ContainsKey("revoke"))
			{
				revokeBefore = this.Request.Query["revoke"] == "true";
			}

			var token = await this.tokenService.RefreshTokenAsync(refreshToken, revokeBefore);
			return this.Created($"{this.Request.Scheme}://{this.Request.Host}", token);
		}
		
		[HttpPost]
		[Route("refresh-token")]
		public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenFormModel model)
		{
			string refreshToken = this.GetTokenFromHeader(out string _);
			if (string.IsNullOrEmpty(refreshToken))
			{
				refreshToken = model.Token;
			}

			bool revokeBefore = true;
			if (this.Request.Query.ContainsKey("revoke"))
			{
				revokeBefore = this.Request.Query["revoke"] == "true";
			}

			var token = await this.tokenService.RefreshTokenAsync(refreshToken, revokeBefore);
			return this.Created($"{this.Request.Scheme}://{this.Request.Host}", token);
		}
		
		[HttpGet]
		[Route("revoke-token")]
		public async Task<IActionResult> RevokeToken(CancellationToken cancellationToken = default)
		{
			string token = this.GetTokenFromHeader(out string _);
			if (string.IsNullOrEmpty(token))
			{
				return this.AuthorizationHeaderMissing();
			}

			bool logoutFromAllDevices = false;
			if (this.Request.Query.ContainsKey("logout-all"))
			{
				bool.TryParse(this.Request.Query["logout-all"], out logoutFromAllDevices);
			}
			
			if (await this.tokenService.RevokeTokenAsync(token, logoutFromAllDevices, cancellationToken: cancellationToken))
			{
				await this.providerService.LogoutAsync(token, cancellationToken: cancellationToken);
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
			string token = this.GetTokenFromHeader(out string _);
			if (string.IsNullOrEmpty(token))
			{
				token = model.Token;
			}

			bool logoutFromAllDevices = false;
			if (this.Request.Query.ContainsKey("logout-all"))
			{
				bool.TryParse(this.Request.Query["logout-all"], out logoutFromAllDevices);
			}
			
			if (await this.tokenService.RevokeTokenAsync(token, logoutFromAllDevices, cancellationToken: cancellationToken))
			{
				await this.providerService.LogoutAsync(token, cancellationToken: cancellationToken);
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
			var membershipId = this.GetXErtisAlias();
			if (string.IsNullOrEmpty(membershipId))
			{
				return this.XErtisAliasMissing();
			}

			var username = model.Username;
			var password = model.Password;
			var host = this.Request.Headers.TryGetValue("X-Host", out var hostStringValue) ? hostStringValue.ToString() : null;
			
			var otp = await this.oneTimePasswordService.VerifyOtpAsync(username, password, membershipId, host);
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
		public async Task<IActionResult> FacebookLogin([FromBody] FacebookLoginRequest request)
		{
			var membershipId = this.GetXErtisAlias();
			
			string ipAddress = null;
			if (this.Request.Headers.TryGetValue("X-IpAddress", out var ipAddressHeader))
			{
				ipAddress = ipAddressHeader.ToString();
			}
			
			string userAgent = null;
			if (this.Request.Headers.TryGetValue("X-UserAgent", out var userAgentHeader))
			{
				userAgent = userAgentHeader.ToString();
			}
			
			return this.Created($"{this.Request.Scheme}://{this.Request.Host}", await this.providerService.LoginAsync(request, membershipId, ipAddress: ipAddress, userAgent: userAgent));
		}
		
		[HttpPost]
		[Route("oauth/google/login")]
		public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
		{
			var membershipId = this.GetXErtisAlias();
			
			string ipAddress = null;
			if (this.Request.Headers.TryGetValue("X-IpAddress", out var ipAddressHeader))
			{
				ipAddress = ipAddressHeader.ToString();
			}
			
			string userAgent = null;
			if (this.Request.Headers.TryGetValue("X-UserAgent", out var userAgentHeader))
			{
				userAgent = userAgentHeader.ToString();
			}
			
			return this.Created($"{this.Request.Scheme}://{this.Request.Host}", await this.providerService.LoginAsync(request, membershipId, ipAddress: ipAddress, userAgent: userAgent));
		}
		
		[HttpPost]
		[Route("oauth/microsoft/login")]
		public async Task<IActionResult> MicrosoftLogin([FromBody] MicrosoftLoginRequest request)
		{
			var membershipId = this.GetXErtisAlias();
			
			string ipAddress = null;
			if (this.Request.Headers.TryGetValue("X-IpAddress", out var ipAddressHeader))
			{
				ipAddress = ipAddressHeader.ToString();
			}
			
			string userAgent = null;
			if (this.Request.Headers.TryGetValue("X-UserAgent", out var userAgentHeader))
			{
				userAgent = userAgentHeader.ToString();
			}
			
			return this.Created($"{this.Request.Scheme}://{this.Request.Host}", await this.providerService.LoginAsync(request, membershipId, ipAddress: ipAddress, userAgent: userAgent));
		}
		
		[HttpPost]
		[Route("oauth/apple/login")]
		public async Task<IActionResult> AppleLogin([FromBody] AppleLoginModel request)
		{
			var membershipId = this.GetXErtisAlias();
			
			string ipAddress = null;
			if (this.Request.Headers.TryGetValue("X-IpAddress", out var ipAddressHeader))
			{
				ipAddress = ipAddressHeader.ToString();
			}
			
			string userAgent = null;
			if (this.Request.Headers.TryGetValue("X-UserAgent", out var userAgentHeader))
			{
				userAgent = userAgentHeader.ToString();
			}
			
			return this.Created($"{this.Request.Scheme}://{this.Request.Host}", await this.providerService.LoginAsync(request.ToLoginRequest(), membershipId, ipAddress: ipAddress, userAgent: userAgent));
		}

		#endregion
	}
}