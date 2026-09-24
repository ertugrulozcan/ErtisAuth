using Microsoft.AspNetCore.Authorization;

namespace ErtisAuth.Extensions.Authorization.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class SelfAuthorizedAttribute() : AuthorizeAttribute(Authorization.Policy.Name);