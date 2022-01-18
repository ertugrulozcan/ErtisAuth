using System.Threading.Tasks;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Exceptions;
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

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="tokenService"></param>
		public TokensController(ITokenService tokenService)
		{
			this.tokenService = tokenService;
		}

		#endregion
		
		#region Methods

		[HttpGet]
		[Route("me")]
		public async Task<IActionResult> Me()
		{
			return await this.WhoAmI();
		}
		
		[HttpGet]
		[Route("whoami")]
		public async Task<IActionResult> WhoAmI()
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

			switch (tokenType)
			{
				case SupportedTokenTypes.None:
					throw ErtisAuthException.UnsupportedTokenType();
				case SupportedTokenTypes.Basic:
					var application = await this.tokenService.WhoAmIAsync(new BasicToken(token));
					if (application != null)
					{
						return this.Ok(application);
					}
					else
					{
						return this.InvalidToken();
					}
				case SupportedTokenTypes.Bearer:
					var user = await this.tokenService.WhoAmIAsync(BearerToken.CreateTemp(token));
					if (user != null)
					{
						return this.Ok(user);
					}
					else
					{
						return this.InvalidToken();
					}
				default:
					throw ErtisAuthException.UnsupportedTokenType();
			}
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
				return this.NoContent();
			}
			else
			{
				return this.Unauthorized();
			}
		}

		#endregion
	}
}