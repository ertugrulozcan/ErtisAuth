using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Identity.Jwt.Services.Interfaces;
using ErtisAuth.WebAPI.Extensions;
using Microsoft.AspNetCore.Http;

namespace ErtisAuth.WebAPI.Adapters
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
					var usernameClaim = securityToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.UniqueName);
					if (usernameClaim != null)
					{
						return usernameClaim.Value;
					}
					else
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
			}

			if (this.httpContextAccessor.HttpContext != null && this.httpContextAccessor.HttpContext.Items.TryGetValue("SysUtilizer", out var utilizer))
			{
				return utilizer?.ToString();
			}
			
			return null;
		}
		
		private bool TryExtractAccessToken(out string accessToken)
		{
			try
			{
				accessToken = this.httpContextAccessor.HttpContext?.Request.GetTokenFromHeader(out string _);
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