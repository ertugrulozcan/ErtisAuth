using System.Threading.Tasks;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;

namespace ErtisAuth.Hub.Services.Interfaces
{
    public interface IMiddlewareRoleService
    {
        bool CheckPermission(Rbac rbac, TokenBase token);

        Task<bool> CheckPermissionAsync(Rbac rbac, TokenBase token);
    }
}