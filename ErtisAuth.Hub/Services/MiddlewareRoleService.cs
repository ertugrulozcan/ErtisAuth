using System.Threading.Tasks;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Sdk.Services.Interfaces;
using ErtisAuth.Hub.Services.Interfaces;

namespace ErtisAuth.Hub.Services
{
    public class MiddlewareRoleService : IMiddlewareRoleService
    {
        #region Services

        private readonly IRoleService roleService;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="roleService"></param>
        public MiddlewareRoleService(IRoleService roleService)
        {
            this.roleService = roleService;
        }

        #endregion

        #region Methods

        public bool CheckPermission(Rbac rbac, TokenBase token)
        {
            return this.roleService.CheckPermission(rbac.ToString(), token);
        }

        public async Task<bool> CheckPermissionAsync(Rbac rbac, TokenBase token)
        {
            return await this.roleService.CheckPermissionAsync(rbac.ToString(), token);
        }

        #endregion
    }
}