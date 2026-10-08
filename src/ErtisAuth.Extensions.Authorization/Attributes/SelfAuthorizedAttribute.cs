using Microsoft.AspNetCore.Authorization;

namespace ErtisAuth.Extensions.Authorization.Attributes;

/// <summary>
/// The endpoint requires authorization, but checks the permission itself (e.g. with an rbac it can only build from the
/// requested data): the authentication handler only authenticates the token. On an action it overrides the
/// controller's [Authorized] or [Unauthorized].
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
public class SelfAuthorizedAttribute() : AuthorizeAttribute(Authorization.Policy.Name);