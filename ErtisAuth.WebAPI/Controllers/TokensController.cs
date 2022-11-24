using System.Threading.Tasks;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Integrations.OAuth.Facebook;
using ErtisAuth.Integrations.OAuth.Google;
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

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="tokenService"></param>
		/// <param name="userService"></param>
		/// <param name="providerService"></param>
		public TokensController(ITokenService tokenService, IUserService userService, IProviderService providerService)
		{
			this.tokenService = tokenService;
			this.userService = userService;
			this.providerService = providerService;
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

		private async Task<IUtilizer> GetTokenOwnerUtilizerAsync(TokenBase token)
		{
			return token.TokenType switch
			{
				SupportedTokenTypes.None => throw ErtisAuthException.UnsupportedTokenType(),
				SupportedTokenTypes.Basic => await this.tokenService.WhoAmIAsync(token as BasicToken),
				SupportedTokenTypes.Bearer => await this.tokenService.WhoAmIAsync(token as BearerToken),
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
			if (this.Request.Headers.ContainsKey("X-IpAddress"))
			{
				ipAddress = this.Request.Headers["X-IpAddress"].ToString();
			}
			
			string userAgent = null;
			if (this.Request.Headers.ContainsKey("X-UserAgent"))
			{
				userAgent = this.Request.Headers["X-UserAgent"].ToString();
			}
			
			var token = await this.tokenService.GenerateTokenAsync(username, password, membershipId, ipAddress: ipAddress, userAgent: userAgent);
			if (token != null)
			{
				return this.Created($"{this.Request.Scheme}://{this.Request.Host}", token);
			}
			else
			{
				return this.UsernameOrPasswordIsWrong();
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
		public async Task<IActionResult> RevokeToken()
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
			
			if (await this.tokenService.RevokeTokenAsync(token, logoutFromAllDevices))
			{
				await this.providerService.LogoutAsync(token);
				return this.NoContent();
			}
			else
			{
				return this.Unauthorized();
			}
		}
		
		[HttpPost]
		[Route("revoke-token")]
		public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenFormModel model)
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
			
			if (await this.tokenService.RevokeTokenAsync(token, logoutFromAllDevices))
			{
				await this.providerService.LogoutAsync(token);
				return this.NoContent();
			}
			else
			{
				return this.Unauthorized();
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
			if (this.Request.Headers.ContainsKey("X-IpAddress"))
			{
				ipAddress = this.Request.Headers["X-IpAddress"].ToString();
			}
			
			string userAgent = null;
			if (this.Request.Headers.ContainsKey("X-UserAgent"))
			{
				userAgent = this.Request.Headers["X-UserAgent"].ToString();
			}
			
			return this.Created($"{this.Request.Scheme}://{this.Request.Host}", await this.providerService.LoginAsync(request, membershipId, ipAddress: ipAddress, userAgent: userAgent));
		}
		
		[HttpPost]
		[Route("oauth/google/login")]
		public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
		{
			var membershipId = this.GetXErtisAlias();
			
			string ipAddress = null;
			if (this.Request.Headers.ContainsKey("X-IpAddress"))
			{
				ipAddress = this.Request.Headers["X-IpAddress"].ToString();
			}
			
			string userAgent = null;
			if (this.Request.Headers.ContainsKey("X-UserAgent"))
			{
				userAgent = this.Request.Headers["X-UserAgent"].ToString();
			}
			
			return this.Created($"{this.Request.Scheme}://{this.Request.Host}", await this.providerService.LoginAsync(request, membershipId, ipAddress: ipAddress, userAgent: userAgent));
		}

		#endregion
	}
}