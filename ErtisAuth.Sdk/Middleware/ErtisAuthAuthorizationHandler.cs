using ErtisAuth.Extensions.Authorization.Extensions;
using Microsoft.AspNetCore.Authorization;

namespace ErtisAuth.Sdk.Middleware;

public class ErtisAuthAuthorizationRequirement : IAuthorizationRequirement;

public class ErtisAuthAuthorizationHandler : AuthorizationHandler<ErtisAuthAuthorizationRequirement>
{
	protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ErtisAuthAuthorizationRequirement requirement)
	{
		var allowedClaims = new[]
		{
			ClaimExtensions.UtilizerClaimName,
			ClaimExtensions.PublicClaimName
		};
		
		if (context.User.Identities.Any(x => allowedClaims.Contains(x.NameClaimType)))
		{
			context.Succeed(requirement);
		}
		else
		{
			context.Fail();
		}
		
		return Task.CompletedTask;
	}
}