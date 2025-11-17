using System.Threading.Tasks;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Extensions.AspNetCore.Models;
using Microsoft.AspNetCore.Http;

namespace ErtisAuth.Extensions.AspNetCore.Services;

public interface IAuthorizationHandler<in T> where T : TokenBase
{
	Task<Utilizer> CheckAuthenticationAsync(T token);
	
	Task<AuthorizationResult> CheckAuthorizationAsync(T token, HttpContext context);
}