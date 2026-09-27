using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;

namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// Deleted roles must not keep granting permissions from the cached role list.
/// </summary>
public class RoleServiceCacheTests
{
	#region Fields
	
	private readonly IMembershipService _membershipService = Substitute.For<IMembershipService>();
	
	private readonly IRoleRepository _repository = Substitute.For<IRoleRepository>();
	
	private readonly Membership _membership;
	
	private readonly Utilizer _utilizer;
	
	#endregion
	
	#region Constructors
	
	public RoleServiceCacheTests()
	{
		this._membership = TestServiceFactory.CreateMembership("SHA2-256");
		this._membershipService.Get(this._membership.Id).Returns(this._membership);
		this._membershipService.GetAsync(this._membership.Id, Arg.Any<CancellationToken>()).Returns(this._membership);
		this._utilizer = Utilizer.GetSystemUtilizer(this._membership.Id);
		InMemoryRepository.Setup(this._repository);
	}
	
	#endregion
	
	#region Helpers
	
	private RoleService CreateRoleService()
	{
		return new RoleService(this._membershipService, Substitute.For<IEventService>(), new MemoryCache(new MemoryCacheOptions()), this._repository);
	}
	
	private Role NewRole(string id, string name)
	{
		return new Role
		{
			Id = id,
			Name = name,
			MembershipId = this._membership.Id,
			Permissions = ["*.users.read.*"]
		};
	}
	
	/// <summary>
	/// Creating roles refreshes the cached role list of the membership.
	/// </summary>
	private async Task<RoleService> CreateRoleServiceWithCachedRolesAsync()
	{
		var roleService = this.CreateRoleService();
		await roleService.CreateAsync(this._utilizer, this._membership.Id, this.NewRole("role-1", "Editor"), TestContext.Current.CancellationToken);
		await roleService.CreateAsync(this._utilizer, this._membership.Id, this.NewRole("role-2", "Viewer"), TestContext.Current.CancellationToken);
		return roleService;
	}
	
	#endregion
	
	#region Bulk Delete
	
	[Fact]
	public async Task BulkDeleteAsync_RemovesDeletedRolesFromCache()
	{
		var roleService = await this.CreateRoleServiceWithCachedRolesAsync();
		Assert.NotNull(await roleService.GetBySlugAsync("editor", this._membership.Id, TestContext.Current.CancellationToken));
		
		await roleService.BulkDeleteAsync(this._utilizer, this._membership.Id, ["role-1"], TestContext.Current.CancellationToken);
		
		Assert.Null(await roleService.GetBySlugAsync("editor", this._membership.Id, TestContext.Current.CancellationToken));
		Assert.Null(await roleService.GetAsync(this._membership.Id, "role-1", TestContext.Current.CancellationToken));
		Assert.NotNull(await roleService.GetBySlugAsync("viewer", this._membership.Id, TestContext.Current.CancellationToken));
	}
	
	[Fact]
	public async Task BulkDelete_RemovesDeletedRolesFromCache()
	{
		var roleService = await this.CreateRoleServiceWithCachedRolesAsync();
		Assert.NotNull(roleService.GetBySlug("editor", this._membership.Id));
		
		roleService.BulkDelete(this._utilizer, this._membership.Id, ["role-1"]);
		
		Assert.Null(roleService.GetBySlug("editor", this._membership.Id));
		Assert.Null(roleService.Get(this._membership.Id, "role-1"));
		Assert.NotNull(roleService.GetBySlug("viewer", this._membership.Id));
	}
	
	[Fact]
	public async Task DeleteAsync_RemovesDeletedRoleFromCache()
	{
		var roleService = await this.CreateRoleServiceWithCachedRolesAsync();
		
		await roleService.DeleteAsync(this._utilizer, this._membership.Id, "role-1", TestContext.Current.CancellationToken);
		
		Assert.Null(await roleService.GetBySlugAsync("editor", this._membership.Id, TestContext.Current.CancellationToken));
	}
	
	#endregion
}
