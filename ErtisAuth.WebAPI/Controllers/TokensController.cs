using System.Threading.Tasks;
using ErtisAuth.Abstractions.Services.Interfaces;
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

			var token = await this.tokenService.GenerateTokenAsync(username, password, membershipId);
			if (token != null)
			{
				return this.Created($"{this.Request.Scheme}://{this.Request.Host}", token);
			}
			else
			{
				return this.Unauthorized(username, password);
			}
		}
		
		[HttpGet]
		[Route("verify-token")]
		public async Task<IActionResult> VerifyToken()
		{
			string token = this.GetTokenFromHeader(out string _);
			if (string.IsNullOrEmpty(token))
			{
				return this.AuthorizationHeaderMissing();
			}
			
			var validationResult = await this.tokenService.VerifyTokenAsync(token);
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
			string token = this.GetTokenFromHeader(out string _);
			if (string.IsNullOrEmpty(token))
			{
				token = model.Token;
			}

			var validationResult = await this.tokenService.VerifyTokenAsync(token);
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

			if (await this.tokenService.RevokeTokenAsync(token))
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

			if (await this.tokenService.RevokeTokenAsync(token))
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