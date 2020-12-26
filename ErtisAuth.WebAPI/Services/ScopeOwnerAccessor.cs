using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Identity.Jwt.Services.Interfaces;
using ErtisAuth.WebAPI.Extensions;
using Microsoft.AspNetCore.Http;

namespace ErtisAuth.WebAPI.Services
{
	public class ScopeOwnerAccessor : IScopeOwnerAccessor
	{
		#region Services

		private readonly IHttpContextAccessor httpContextAccessor;
		private readonly IJwtService jwtService;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="httpContextAccessor"></param>
		/// <param name="jwtService"></param>
		public ScopeOwnerAccessor(IHttpContextAccessor httpContextAccessor, IJwtService jwtService)
		{
			this.httpContextAccessor = httpContextAccessor;
			this.jwtService = jwtService;
		}

		#endregion

		#region Methods

		public string GetRequestOwner()
		{
			if (this.TryExtractAccessToken(out var accessToken))
			{
				if (this.jwtService.TryDecodeToken(accessToken, out var securityToken))
				{
					var emailClaim = securityToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Email);
					if (emailClaim != null)
					{
						return emailClaim.Value;
					}
					else
					{
						return securityToken.Subject;
					}
				}
			}

			return null;
		}
		
		private bool TryExtractAccessToken(out string accessToken)
		{
			try
			{
				accessToken = this.httpContextAccessor.HttpContext.Request.GetTokenFromHeader(out string _);
				return true;
			}
			catch
			{
				accessToken = null;
				return false;
			}
		}

		#endregion
	}
}