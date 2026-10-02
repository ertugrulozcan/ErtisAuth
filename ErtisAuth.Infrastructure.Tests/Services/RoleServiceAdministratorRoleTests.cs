using Ertis.Core.Collections;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Helpers;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// The administrator role of a membership is ensured explicitly (at startup and on migration), not in the constructor.
/// </summary>
public class RoleServiceAdministratorRoleTests
{
	#region Fields
	
	private readonly IMembershipService _membershipService = Substitute.For<IMembershipService>();
	
	private readonly IRoleRepository _repository = Substitute.For<IRoleRepository>();
	
	private readonly List<Role> _roles;
	
	private readonly List<Membership> _memberships = [];
	
	#endregion
	
	#region Constructors
	
	public RoleServiceAdministratorRoleTests()
	{
		// Inserted roles get their ids from the database
		this._roles = InMemoryRepository.Setup(this._repository, role => role.Id = Guid.NewGuid().ToString("N")[..24]);
		
		this.AddMembership("membership-1");
		this.AddMembership("membership-2");
		
		this._membershipService
			.GetAsync()
			.ReturnsForAnyArgs(_ => new PaginationCollection<Membership> { Count = this._memberships.Count, Items = this._memberships });
	}
	
	#endregion
	
	#region Helpers
	
	private void AddMembership(string id)
	{
		var membership = TestServiceFactory.CreateMembership("SHA2-256");
		membership.Id = id;
		this._memberships.Add(membership);
		this._membershipService.GetAsync(id, Arg.Any<CancellationToken>()).Returns(membership);
	}
	
	private RoleService CreateRoleService()
	{
		return new RoleService(this._membershipService, Substitute.For<IEventService>(), new MemoryCache(new MemoryCacheOptions()), this._repository, NullLogger<RoleService>.Instance);
	}
	
	private Role[] AdministratorRoles(string membershipId)
	{
		return this._roles.Where(x => x.MembershipId == membershipId && x.Slug == ReservedRoles.Administrator).ToArray();
	}
	
	#endregion
	
	#region Tests
	
	[Fact]
	public void Constructor_DoesNotAccessDatabase()
	{
		this._membershipService.ClearReceivedCalls();
		this._repository.ClearReceivedCalls();
		
		this.CreateRoleService();
		
		// Registering the service to the membership service is the only expected interaction
		Assert.All(this._membershipService.ReceivedCalls(), x => Assert.Equal(nameof(IMembershipService.RegisterService), x.GetMethodInfo().Name));
		Assert.Empty(this._repository.ReceivedCalls());
	}
	
	[Fact]
	public async Task EnsureAdministratorRoleAsync_WhenMissing_CreatesAdministratorRole()
	{
		var membership = this._memberships[0];
		
		var role = await this.CreateRoleService().EnsureAdministratorRoleAsync(membership, TestContext.Current.CancellationToken);
		
		Assert.Equal(ReservedRoles.Administrator, role.Slug);
		Assert.Single(this.AdministratorRoles(membership.Id));
		Assert.Contains("*.users.create.*", role.Permissions!);
		Assert.Contains("*.memberships.delete.*", role.Permissions!);
		Assert.Contains("*.code-policies.update.*", role.Permissions!);
		Assert.Contains("*.otp.create.*", role.Permissions!);
	}
	
	[Fact]
	public async Task EnsureAdministratorRoleAsync_WhenExists_ReturnsExistingRole()
	{
		var membership = this._memberships[0];
		var roleService = this.CreateRoleService();
		var created = await roleService.EnsureAdministratorRoleAsync(membership, TestContext.Current.CancellationToken);
		
		var ensured = await roleService.EnsureAdministratorRoleAsync(membership, TestContext.Current.CancellationToken);
		
		Assert.Equal(created.Id, ensured.Id);
		Assert.Single(this.AdministratorRoles(membership.Id));
	}
	
	[Fact]
	public async Task EnsureAdministratorRolesAsync_EnsuresRoleOfEveryMembership()
	{
		var roleService = this.CreateRoleService();
		await roleService.EnsureAdministratorRoleAsync(this._memberships[0], TestContext.Current.CancellationToken);
		
		await roleService.EnsureAdministratorRolesAsync(TestContext.Current.CancellationToken);
		
		Assert.Single(this.AdministratorRoles("membership-1"));
		Assert.Single(this.AdministratorRoles("membership-2"));
	}
	
	#endregion
}
