using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Sdk.AspNetCore.Models;
using Microsoft.AspNetCore.Http;

namespace ErtisAuth.Sdk.AspNetCore.Middleware;

public interface IAuthorizationHandler<in T> where T : TokenBase
{
	Task<Utilizer> CheckAuthenticationAsync(T token);
	
	Task<AuthorizationResult> CheckAuthorizationAsync(T token, HttpContext context);
}