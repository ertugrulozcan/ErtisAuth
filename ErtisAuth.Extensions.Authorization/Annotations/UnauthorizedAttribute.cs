using ErtisAuth.Extensions.Authorization.Constants;
using Microsoft.AspNetCore.Authorization;

namespace ErtisAuth.Extensions.Authorization.Annotations;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
public class UnauthorizedAttribute() : AuthorizeAttribute(Policies.ErtisAuthAuthorizationPolicyName);