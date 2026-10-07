using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.Extensions.AspNetCore.Attributes;

/// <summary>
/// Routes a membership bounded controller under <c>memberships/{membershipId}/{template}</c>.
/// Endpoints carrying this attribute are only accessible with a token of the same membership
/// (enforced by ErtisAuthAuthenticationHandler).
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class MembershipRouteAttribute(string template) : RouteAttribute($"memberships/{{{ParameterName}}}/{template}")
{
	#region Constants

	public const string ParameterName = "membershipId";

	#endregion
}
