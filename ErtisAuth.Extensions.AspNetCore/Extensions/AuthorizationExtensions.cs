using ErtisAuth.Extensions.AspNetCore.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;

namespace ErtisAuth.Extensions.AspNetCore.Extensions;

public static class AuthorizationExtensions
{
	#region Methods
	
	public static void AddErtisAuth(this IServiceCollection services)
	{
		// Authentication
		services
			.AddAuthentication()
			.AddScheme<AuthenticationSchemeOptions, ErtisAuthAuthenticationHandler>(Authorization.Scheme.Name, _ => {});
		
		// Authorization
		services.AddAuthorization(options =>
			options.AddPolicy(Authorization.Policy.Name, policy =>
			{
				policy.AddAuthenticationSchemes(Authorization.Scheme.Name);
				policy.AddRequirements(new ErtisAuthAuthorizationRequirement());
			}));
	}
	
	/// <summary>
	/// Every 401 response carries the WWW-Authenticate challenge (RFC 9110 §15.5.2), also the ones written by controllers
	/// (e.g. wrong credentials) and by the exception handler.
	/// </summary>
	public static IApplicationBuilder UseWwwAuthenticateChallenge(this IApplicationBuilder app)
	{
		return app.Use(async (context, next) =>
		{
			context.Response.OnStarting(() =>
			{
				if (context.Response.StatusCode == StatusCodes.Status401Unauthorized && !context.Response.Headers.ContainsKey(HeaderNames.WWWAuthenticate))
				{
					context.Response.Headers.WWWAuthenticate = Authorization.Scheme.WwwAuthenticate;
				}
				
				return Task.CompletedTask;
			});
			
			await next(context);
		});
	}
	
	#endregion
}