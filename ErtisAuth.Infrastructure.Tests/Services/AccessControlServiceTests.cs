using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Infrastructure.Services;
using NSubstitute;

namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// Authorization rules (RBAC, UBAC, own-update rule and token scopes) as evaluated by the authentication handler
/// through AccessControlService.HasPermission(Role, Rbac, Utilizer).
/// </summary>
public class AccessControlServiceTests
{
	#region Constants
	
	private const string UtilizerId = "user-1";
	
	private const string OtherUserId = "user-2";
	
	#endregion
	
	#region Helpers
	
	private static AccessControlService CreateService()
	{
		return new AccessControlService(Substitute.For<IRoleService>());
	}
	
	private static Role CreateRole(string[]? permissions = null, string[]? forbidden = null)
	{
		return new Role
		{
			Id = "role-id",
			Name = "test-role",
			MembershipId = "membership-id",
			Permissions = permissions,
			Forbidden = forbidden
		};
	}
	
	private static Utilizer CreateUtilizer(
		Utilizer.UtilizerType type = Utilizer.UtilizerType.User,
		string[]? permissions = null,
		string[]? forbidden = null,
		string[]? scopes = null)
	{
		return new Utilizer
		{
			Id = UtilizerId,
			Username = "john.doe",
			MembershipId = "membership-id",
			Role = "test-role",
			Type = type,
			Permissions = permissions,
			Forbidden = forbidden,
			Scopes = scopes
		};
	}
	
	/// <summary>
	/// The rbac the authentication handler builds for a request: [utilizer].[resource].[action].[route object or *]
	/// </summary>
	private static Rbac Request(string resource, string action, string? obj = null)
	{
		return new Rbac(
			new RbacSegment(UtilizerId),
			new RbacSegment(resource),
			new RbacSegment(action),
			obj == null ? RbacSegment.All : new RbacSegment(obj));
	}
	
	private static bool HasPermission(Role role, Rbac rbac, Utilizer? utilizer = null)
	{
		return CreateService().HasPermission(role, rbac, utilizer ?? CreateUtilizer());
	}
	
	#endregion
	
	#region Role Permissions (RBAC)
	
	[Theory]
	[InlineData("users.read")]
	[InlineData("users.*")]
	[InlineData("*.read")]
	[InlineData("*")]
	[InlineData("users.read.*")]
	[InlineData("users.read.user-2")]
	[InlineData("*.users.read.*")]
	[InlineData("user-1.users.read.*")]
	public void HasPermission_WithMatchingRolePermission_ReturnsTrue(string permission)
	{
		Assert.True(HasPermission(CreateRole([permission]), Request("users", "read", OtherUserId)));
	}
	
	[Theory]
	[InlineData("users.create")]
	[InlineData("roles.read")]
	[InlineData("users.read.user-3")]
	[InlineData("someone-else.users.read.*")]
	[InlineData("not a valid ... permission")]
	public void HasPermission_WithoutMatchingRolePermission_ReturnsFalse(string permission)
	{
		Assert.False(HasPermission(CreateRole([permission]), Request("users", "read", OtherUserId)));
	}
	
	[Fact]
	public void HasPermission_WithoutAnyRolePermission_ReturnsFalse()
	{
		Assert.False(HasPermission(CreateRole(), Request("users", "read")));
	}
	
	[Theory]
	[InlineData("users.read")]
	[InlineData("users.*")]
	[InlineData("*")]
	[InlineData("users.read.user-2")]
	public void HasPermission_WhenRoleForbidsWhatItPermits_ForbiddenWins(string forbidden)
	{
		Assert.False(HasPermission(CreateRole(["*"], [forbidden]), Request("users", "read", OtherUserId)));
	}
	
	[Fact]
	public void HasPermission_WithForbiddenForAnotherObject_DoesNotAffectThisObject()
	{
		Assert.True(HasPermission(CreateRole(["users.*"], ["users.read.user-3"]), Request("users", "read", OtherUserId)));
	}
	
	[Fact]
	public void HasPermission_ResourceAndActionMatching_IsCaseInsensitive()
	{
		Assert.True(HasPermission(CreateRole(["USERS.READ"]), Request("users", "read")));
	}
	
	[Fact]
	public void HasPermission_ObjectMatching_IsCaseSensitive()
	{
		Assert.False(HasPermission(CreateRole(["users.read.USER-2"]), Request("users", "read", OtherUserId)));
	}
	
	#endregion
	
	#region User Permissions (UBAC)
	
	[Fact]
	public void HasPermission_WithUbacPermission_GrantsEvenWhenRoleDoesNot()
	{
		var utilizer = CreateUtilizer(permissions: ["users.read"]);
		
		Assert.True(HasPermission(CreateRole(), Request("users", "read"), utilizer));
	}
	
	[Fact]
	public void HasPermission_WithUbacForbidden_DeniesEvenWhenRolePermits()
	{
		var utilizer = CreateUtilizer(forbidden: ["users.delete"]);
		
		Assert.False(HasPermission(CreateRole(["*"]), Request("users", "delete"), utilizer));
	}
	
	[Fact]
	public void HasPermission_WithUbacPermission_OverridesRoleForbidden()
	{
		// UBAC is evaluated first and, when it matches, its decision is final (the role is not consulted).
		var utilizer = CreateUtilizer(permissions: ["users.delete"]);
		
		Assert.True(HasPermission(CreateRole(["*"], ["users.delete"]), Request("users", "delete"), utilizer));
	}
	
	[Fact]
	public void HasPermission_WithNonMatchingUbac_FallsBackToRole()
	{
		var utilizer = CreateUtilizer(permissions: ["roles.read"], forbidden: ["roles.delete"]);
		
		Assert.True(HasPermission(CreateRole(["users.read"]), Request("users", "read"), utilizer));
		Assert.False(HasPermission(CreateRole(["users.read"]), Request("users", "delete"), utilizer));
	}
	
	#endregion
	
	#region Own Update Rule
	
	[Theory]
	[InlineData(Utilizer.UtilizerType.User, "users")]
	[InlineData(Utilizer.UtilizerType.Application, "applications")]
	public void HasPermission_UpdatingItself_IsAllowedWithoutRolePermission(Utilizer.UtilizerType type, string resource)
	{
		var utilizer = CreateUtilizer(type);
		
		Assert.True(HasPermission(CreateRole(), Request(resource, "update", UtilizerId), utilizer));
	}
	
	[Theory]
	[InlineData("users", "update", OtherUserId)]
	[InlineData("users", "delete", UtilizerId)]
	[InlineData("users", "read", UtilizerId)]
	[InlineData("applications", "update", UtilizerId)]
	public void HasPermission_OwnUpdateRule_DoesNotCoverOtherObjectsActionsOrResources(string resource, string action, string obj)
	{
		Assert.False(HasPermission(CreateRole(), Request(resource, action, obj), CreateUtilizer()));
	}
	
	[Theory]
	[InlineData("users.update")]
	[InlineData("users.*")]
	[InlineData("*")]
	public void HasPermission_UpdatingItself_IsDeniedWhenRoleForbidsUpdate(string forbidden)
	{
		var role = CreateRole(forbidden: [forbidden]);
		
		Assert.False(HasPermission(role, Request("users", "update", UtilizerId), CreateUtilizer()));
	}
	
	#endregion
	
	#region Token Scopes
	
	[Fact]
	public void HasPermission_WithScopeCoveringRequest_ReturnsTrue()
	{
		var utilizer = CreateUtilizer(scopes: ["users.read"]);
		
		Assert.True(HasPermission(CreateRole(["*"]), Request("users", "read"), utilizer));
	}
	
	[Fact]
	public void HasPermission_WithScopeNotCoveringRequest_ReturnsFalseEvenWhenRolePermits()
	{
		var utilizer = CreateUtilizer(scopes: ["users.read"]);
		
		Assert.False(HasPermission(CreateRole(["*"]), Request("users", "delete"), utilizer));
	}
	
	[Fact]
	public void HasPermission_WithScopedTokenAndUbacPermission_IsStillLimitedByScopes()
	{
		var utilizer = CreateUtilizer(permissions: ["users.delete"], scopes: ["users.read"]);
		
		Assert.False(HasPermission(CreateRole(), Request("users", "delete"), utilizer));
	}
	
	[Fact]
	public void HasPermission_WithScopedTokenUpdatingItself_IsStillLimitedByScopes()
	{
		var utilizer = CreateUtilizer(scopes: ["users.read"]);
		
		Assert.False(HasPermission(CreateRole(), Request("users", "update", UtilizerId), utilizer));
	}
	
	
	[Theory]
	[InlineData("users.read")]
	[InlineData("users.read.*")]
	[InlineData("*.users.read.*")]
	[InlineData("user-1.users.read.*")]
	[InlineData("users.*")]
	public void HasPermission_ScopesUseTheRolePermissionFormat(string scope)
	{
		// Scopes are matched like role permissions (1 to 4 segments), so 4-segment scopes work as well.
		var utilizer = CreateUtilizer(scopes: [scope]);
		
		Assert.True(HasPermission(CreateRole(["*"]), Request("users", "read", OtherUserId), utilizer));
	}
	
	[Fact]
	public void HasPermission_ScopeWithSubject_OnlyCoversThatSubject()
	{
		var utilizer = CreateUtilizer(scopes: ["someone-else.users.read.*"]);
		
		Assert.False(HasPermission(CreateRole(["*"]), Request("users", "read"), utilizer));
	}
	
	[Fact]
	public void HasPermission_ScopeWithObject_OnlyCoversThatObject()
	{
		var utilizer = CreateUtilizer(scopes: ["users.read.user-2"]);
		
		Assert.True(HasPermission(CreateRole(["*"]), Request("users", "read", OtherUserId), utilizer));
		Assert.False(HasPermission(CreateRole(["*"]), Request("users", "read", "user-3"), utilizer));
	}
	
	#endregion
	
	#region Granted Permission (without own-update exception)
	
	[Fact]
	public void HasGrantedPermission_UpdatingItselfWithoutRolePermission_ReturnsFalse()
	{
		Assert.False(CreateService().HasGrantedPermission(CreateRole(), Request("users", "update", UtilizerId), CreateUtilizer()));
	}
	
	[Fact]
	public void HasGrantedPermission_WithRolePermission_ReturnsTrue()
	{
		Assert.True(CreateService().HasGrantedPermission(CreateRole(["users.update"]), Request("users", "update", OtherUserId), CreateUtilizer()));
	}
	
	[Fact]
	public void HasGrantedPermission_WithUbacPermissionAndNoRole_ReturnsTrue()
	{
		Assert.True(CreateService().HasGrantedPermission(null, Request("users", "update", OtherUserId), CreateUtilizer(permissions: ["users.update"])));
	}
	
	[Fact]
	public void HasGrantedPermission_WithoutRoleAndUbac_ReturnsFalse()
	{
		Assert.False(CreateService().HasGrantedPermission(null, Request("users", "update", OtherUserId), CreateUtilizer()));
	}
	
	[Fact]
	public void HasGrantedPermission_IsLimitedByScopes()
	{
		Assert.False(CreateService().HasGrantedPermission(CreateRole(["*"]), Request("users", "update", OtherUserId), CreateUtilizer(scopes: ["users.read"])));
	}
	
	#endregion
	
	#region Design Contract
	
	// The role is the basis of authorization: only what the role explicitly permits is permitted.
	[Fact]
	public void Contract_RoleDecides_AbsenceFromPermissionsIsNotAPermission()
	{
		Assert.False(HasPermission(CreateRole(["users.read"]), Request("applications", "update", "123")));
	}
	
	// "*.applications.update.*" permits updating every application;
	// adding "*.applications.update.123" to forbidden narrows it to "every application except 123".
	[Fact]
	public void Contract_ForbiddenNarrowsRolePermission()
	{
		var role = CreateRole(["*.applications.update.*"], ["*.applications.update.123"]);
		
		Assert.False(HasPermission(role, Request("applications", "update", "123")));
		Assert.True(HasPermission(role, Request("applications", "update", "456")));
	}
	
	// UBAC gives exceptional permissions on top of the role: permitted by UBAC means permitted, even if the role forbids it.
	[Theory]
	[InlineData(Utilizer.UtilizerType.User)]
	[InlineData(Utilizer.UtilizerType.Application)]
	public void Contract_UbacPermissionOverridesRoleForbidden(Utilizer.UtilizerType type)
	{
		var role = CreateRole(["*.applications.update.*"], ["*.applications.update.123"]);
		var utilizer = CreateUtilizer(type, permissions: ["applications.update.123"]);
		
		Assert.True(HasPermission(role, Request("applications", "update", "123"), utilizer));
	}
	
	// UBAC gives exceptional prohibitions on top of the role: forbidden by UBAC means forbidden, even if the role permits it.
	[Theory]
	[InlineData(Utilizer.UtilizerType.User)]
	[InlineData(Utilizer.UtilizerType.Application)]
	public void Contract_UbacForbiddenOverridesRolePermission(Utilizer.UtilizerType type)
	{
		var role = CreateRole(["*.applications.update.*"]);
		var utilizer = CreateUtilizer(type, forbidden: ["applications.update.456"]);
		
		Assert.False(HasPermission(role, Request("applications", "update", "456"), utilizer));
		Assert.True(HasPermission(role, Request("applications", "update", "789"), utilizer));
	}
	
	// UBAC only affects what it matches; everything else is still decided by the role.
	[Fact]
	public void Contract_UbacDoesNotAffectUnmatchedRequests()
	{
		var role = CreateRole(["*.applications.update.*"], ["*.applications.update.123"]);
		var utilizer = CreateUtilizer(permissions: ["users.read"], forbidden: ["roles.read"]);
		
		Assert.False(HasPermission(role, Request("applications", "update", "123"), utilizer));
		Assert.True(HasPermission(role, Request("applications", "update", "456"), utilizer));
	}
	
	#endregion
}
