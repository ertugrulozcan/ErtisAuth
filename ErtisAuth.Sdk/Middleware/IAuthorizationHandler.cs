using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Sdk.Models;
using Microsoft.AspNetCore.Http;

namespace ErtisAuth.Sdk.Middleware;

public interface IAuthorizationHandler<in T> where T : TokenBase
{
	Task<Utilizer> CheckAuthenticationAsync(T token);
	
	Task<AuthorizationResult> CheckAuthorizationAsync(T token, HttpContext context);
}