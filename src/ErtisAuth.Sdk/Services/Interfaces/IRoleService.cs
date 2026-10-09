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
	Task<bool> CheckPermissionAsync(string rbac, TokenBase token, CancellationToken cancellationToken = default);
	
	Task<bool> CheckPermissionByRoleAsync(string roleId, string rbac, TokenBase token, CancellationToken cancellationToken = default);
}