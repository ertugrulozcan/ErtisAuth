using ErtisAuth.Extensions.Authorization.Constants;
using Microsoft.AspNetCore.Authorization;

namespace ErtisAuth.Extensions.Authorization.Annotations;

[System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct | System.AttributeTargets.Method)]
public class UnauthorizedAttribute() : AuthorizeAttribute(Policies.ErtisAuthAuthorizationPolicyName);