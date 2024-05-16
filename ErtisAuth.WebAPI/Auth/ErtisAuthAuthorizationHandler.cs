using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace ErtisAuth.WebAPI.Auth
{
	public class ErtisAuthAuthorizationHandler : AuthorizationHandler<ErtisAuthAuthorizationRequirement>
	{
		protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ErtisAuthAuthorizationRequirement requirement)
		{
			if (context.User.Identities.Any(x => x.NameClaimType == "Utilizer") || context.User.Identities.Any(x => x.NameClaimType == "Public"))
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
}