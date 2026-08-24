using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Sdk.Attributes;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global
namespace ErtisAuth.Sdk.Services.Interfaces;

[ServiceLifetime(ServiceLifetime.Singleton)]
public interface IRoleService : IMembershipBoundedService<Role>
{
	bool CheckPermission(string rbac, TokenBase token);
	
	Task<bool> CheckPermissionAsync(string rbac, TokenBase token, CancellationToken cancellationToken = default);
	
	bool CheckPermissionByRole(string roleId, string rbac, TokenBase token);
	
	Task<bool> CheckPermissionByRoleAsync(string roleId, string rbac, TokenBase token, CancellationToken cancellationToken = default);
}