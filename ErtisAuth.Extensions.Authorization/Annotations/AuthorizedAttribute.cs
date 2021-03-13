using ErtisAuth.Extensions.Authorization.Constants;
using Microsoft.AspNetCore.Authorization;

namespace ErtisAuth.Extensions.Authorization.Annotations
{
	[System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
	public class AuthorizedAttribute : AuthorizeAttribute
	{
		#region Constructors

		public AuthorizedAttribute() : base(Policies.ErtisAuthAuthorizationPolicyName)
		{
			
		}

		#endregion
	}
}