using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace ErtisAuth.Extensions.AspNetCore
{
	public class ErtisAuthAuthorizationHandler : AuthorizationHandler<ErtisAuthAuthorizationRequirement>
	{
		protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ErtisAuthAuthorizationRequirement requirement)
		{
			if (context.User.Identities.Any(x => x.NameClaimType == "Utilizer"))
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