using ErtisAuth.WebAPI.Constants;
using Microsoft.AspNetCore.Authorization;

namespace ErtisAuth.WebAPI.Annotations
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