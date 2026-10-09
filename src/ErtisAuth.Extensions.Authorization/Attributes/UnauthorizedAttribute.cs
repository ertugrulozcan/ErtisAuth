using Microsoft.AspNetCore.Authorization;

namespace ErtisAuth.Extensions.Authorization.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
public class UnauthorizedAttribute() : AuthorizeAttribute(Authorization.Policy.Name);