using System.Threading.Tasks;
using Ertis.Core.Models.Response;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
using Microsoft.AspNetCore.Http;
using ErtisAuth.Hub.Models;

namespace ErtisAuth.Hub.Services.Interfaces
{
    public interface ISessionService
    {
        SessionUser GetSessionUser();

        Task<IResponseResult<User>> StartSessionAsync(HttpContext httpContext, BearerToken bearerToken);
    }
}