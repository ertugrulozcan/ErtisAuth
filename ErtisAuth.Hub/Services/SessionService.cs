using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
using Ertis.Core.Models.Response;
using ErtisAuth.Hub.Constants;
using ErtisAuth.Hub.Extensions;
using ErtisAuth.Hub.Models;
using ErtisAuth.Hub.Services.Interfaces;
using ErtisAuth.Sdk.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IAuthenticationService = ErtisAuth.Sdk.Services.Interfaces.IAuthenticationService;
using ErtisAuth.Sdk.Services.Interfaces;
using ErtisAuth.Core.Models.Memberships;

namespace ErtisAuth.Hub.Services
{
	public class SessionService : ISessionService
	{
		#region Services

		private readonly IHttpContextAccessor httpContextAccessor;
		private readonly ILogger<SessionService> logger;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="httpContextAccessor"></param>
		/// <param name="logger"></param>
		public SessionService(IHttpContextAccessor httpContextAccessor, ILogger<SessionService> logger)
		{
			this.httpContextAccessor = httpContextAccessor;
			this.logger = logger;
		}

		#endregion

		#region Methods

		public SessionUser GetSessionUser()
		{
			var userId = this.httpContextAccessor.HttpContext.GetClaim(Claims.UserId);
			var username = this.httpContextAccessor.HttpContext.GetClaim(Claims.Username);
			var email = this.httpContextAccessor.HttpContext.GetClaim(Claims.Email);
			var firstName = this.httpContextAccessor.HttpContext.GetClaim(Claims.FirstName);
			var lastName = this.httpContextAccessor.HttpContext.GetClaim(Claims.LastName);
			var role = this.httpContextAccessor.HttpContext.GetClaim(Claims.Role);
			var membershipId = this.httpContextAccessor.HttpContext.GetClaim(Claims.MembershipId);
			var membershipName = this.httpContextAccessor.HttpContext.GetClaim(Claims.MembershipName);
			var accessToken = this.httpContextAccessor.HttpContext.GetClaim(Claims.AccessToken);
			var refreshToken = this.httpContextAccessor.HttpContext.GetClaim(Claims.RefreshToken);

			return new SessionUser(
				userId,
				username,
				email,
				firstName,
				lastName,
				role,
				membershipId,
				membershipName,
				accessToken,
				refreshToken);
		}

		public async Task<IResponseResult<User>> StartSessionAsync(HttpContext httpContext, string serverUrl, string membershipId, BearerToken bearerToken)
		{
			// Who am i?
			var serviceScopeFactory = httpContext.RequestServices.GetRequiredService<IServiceScopeFactory>();
			using (var scope = serviceScopeFactory.CreateScope())
			{
				var scopedErtisAuthOptions = scope.ServiceProvider.GetRequiredService<IErtisAuthOptions>() as ScopedErtisAuthOptions;
				scopedErtisAuthOptions?.SetTemporary(serverUrl, membershipId);
				var scopedAuthenticationService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();

				var token = BearerToken.CreateTemp(bearerToken.AccessToken);
				var meResult = await scopedAuthenticationService.WhoAmIAsync(token);
	            if (meResult.IsSuccess)
	            {
					Membership membership = null;
					scopedErtisAuthOptions?.SetTemporary(serverUrl, membershipId);
					var scopedMembershipService = scope.ServiceProvider.GetRequiredService<IMembershipService>();
					var getMembershipResult = await scopedMembershipService.GetMembershipAsync(meResult.Data.MembershipId, token);
					if (getMembershipResult.IsSuccess) 
					{
						membership = getMembershipResult.Data;
					}

            		var claims = new List<Claim>
            		{
		                new Claim(Claims.ServerUrl, serverUrl),
		                new Claim(Claims.UserId, meResult.Data.Id),
            			new Claim(Claims.Username, meResult.Data.Username ?? string.Empty),
            			new Claim(Claims.Email, meResult.Data.EmailAddress ?? string.Empty),
            			new Claim(Claims.FirstName, meResult.Data.FirstName ?? string.Empty),
            			new Claim(Claims.LastName, meResult.Data.LastName ?? string.Empty),
            			new Claim(Claims.Role, meResult.Data.Role ?? string.Empty),
						new Claim(Claims.MembershipId, meResult.Data.MembershipId ?? string.Empty),
						new Claim(Claims.MembershipName, membership?.Name ?? string.Empty),
						new Claim(Claims.AccessToken, string.IsNullOrEmpty(bearerToken.AccessToken) ? string.Empty : bearerToken.AccessToken),
            			new Claim(Claims.RefreshToken, string.IsNullOrEmpty(bearerToken.RefreshToken) ? string.Empty : bearerToken.RefreshToken)
            		};

            		var authProperties = new AuthenticationProperties
            		{
            			IsPersistent = true,
            			AllowRefresh = true
            		};

            		var authTokens = new List<AuthenticationToken>();
					if (!string.IsNullOrEmpty(bearerToken.AccessToken))
					{
						authTokens.Add(new AuthenticationToken
						{
							Name = "AccessToken",
							Value = bearerToken.AccessToken
						});
					}
					
					if (!string.IsNullOrEmpty(bearerToken.RefreshToken))
					{
						authTokens.Add(new AuthenticationToken
						{
							Name = "RefreshToken",
							Value = bearerToken.RefreshToken
						});
					}

            		authProperties.StoreTokens(authTokens);

            		await httpContext.SignInAsync(
            			CookieAuthenticationDefaults.AuthenticationScheme, 
            			new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies", Claims.UserId, Claims.Role)), 
            			authProperties);

					this.logger.LogInformation("{Username} ({EmailAddress}) logged-in", meResult.Data.Username, meResult.Data.EmailAddress);
				}

				return meResult;
			}
		}

		#endregion
	}
}