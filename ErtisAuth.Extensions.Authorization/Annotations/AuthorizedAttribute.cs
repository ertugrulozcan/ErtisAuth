using ErtisAuth.Extensions.Authorization.Constants;
using Microsoft.AspNetCore.Authorization;

namespace ErtisAuth.Extensions.Authorization.Annotations;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class AuthorizedAttribute() : AuthorizeAttribute(Policies.ErtisAuthAuthorizationPolicyName);