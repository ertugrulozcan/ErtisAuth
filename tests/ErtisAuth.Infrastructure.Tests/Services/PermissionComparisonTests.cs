using System.Globalization;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Infrastructure.Extensions;

namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// Resource and action segments are case-insensitive names ("users.read" == "USERS.read"), independent of the current culture;
/// subject and object segments are ids and compared ordinally. Permission matching and Rbac/Ubac equality follow the same rules.
/// </summary>
public class PermissionComparisonTests
{
	#region Constants
	
	private const string UtilizerId = "user-1";
	
	#endregion
	
	#region Helpers
	
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
	
	private static Utilizer CreateUtilizer(string[]? permissions = null, string[]? forbidden = null)
	{
		return new Utilizer
		{
			Id = UtilizerId,
			Username = "john.doe",
			MembershipId = "membership-id",
			Role = "test-role",
			Type = Utilizer.UtilizerType.User,
			Permissions = permissions,
			Forbidden = forbidden
		};
	}
	
	private static Rbac Request(string resource, string action, string? obj = null)
	{
		return new Rbac(
			new RbacSegment(UtilizerId),
			new RbacSegment(resource),
			new RbacSegment(action),
			obj == null ? RbacSegment.All : new RbacSegment(obj));
	}
	
	private static T InCulture<T>(string cultureName, Func<T> func)
	{
		var previousCulture = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = new CultureInfo(cultureName);
			return func();
		}
		finally
		{
			CultureInfo.CurrentCulture = previousCulture;
		}
	}
	
	#endregion
	
	#region Matching
	
	// "MEMBERSHIPS" contains an 'I', whose lowercase in tr-TR is the dotless 'ı' (not 'i')
	
	[Theory]
	[InlineData("en-US")]
	[InlineData("tr-TR")]
	[InlineData("")]
	public void RolePermission_ResourceAndActionInDifferentCase_Matches(string cultureName)
	{
		var role = CreateRole(permissions: ["MEMBERSHIPS.READ"]);
		
		Assert.True(InCulture(cultureName, () => role.HasPermission(Request("memberships", "read"))));
	}
	
	[Theory]
	[InlineData("en-US")]
	[InlineData("tr-TR")]
	[InlineData("")]
	public void RoleForbidden_ResourceAndActionInDifferentCase_Matches(string cultureName)
	{
		var role = CreateRole(permissions: ["*"], forbidden: ["MEMBERSHIPS.DELETE"]);
		
		Assert.False(InCulture(cultureName, () => role.HasPermission(Request("memberships", "delete"))));
	}
	
	[Theory]
	[InlineData("en-US")]
	[InlineData("tr-TR")]
	[InlineData("")]
	public void UbacPermission_ResourceAndActionInDifferentCase_Matches(string cultureName)
	{
		var utilizer = CreateUtilizer(permissions: ["MEMBERSHIPS.READ"]);
		
		Assert.True(InCulture(cultureName, () => utilizer.HasPermission(Request("memberships", "read"))));
	}
	
	[Theory]
	[InlineData("en-US")]
	[InlineData("tr-TR")]
	[InlineData("")]
	public void UbacForbidden_ResourceAndActionInDifferentCase_Matches(string cultureName)
	{
		var utilizer = CreateUtilizer(forbidden: ["MEMBERSHIPS.READ"]);
		
		Assert.False(InCulture(cultureName, () => utilizer.HasPermission(Request("memberships", "read"))));
	}
	
	[Fact]
	public void RolePermission_ObjectInDifferentCase_DoesNotMatch()
	{
		var role = CreateRole(permissions: ["users.read.abc"]);
		
		Assert.False(role.HasPermission(Request("users", "read", "ABC")));
	}
	
	[Fact]
	public void RolePermission_SubjectInDifferentCase_DoesNotMatch()
	{
		var role = CreateRole(permissions: ["USER-1.users.read.*"]);
		
		Assert.False(role.HasPermission(Request("users", "read")));
	}
	
	#endregion
	
	#region Rbac Equality
	
	[Theory]
	[InlineData("users.read", "USERS.read")]
	[InlineData("users.read", "users.READ")]
	[InlineData("user-1.users.read.abc", "user-1.Users.Read.abc")]
	public void Rbac_ResourceAndActionInDifferentCase_AreEqualWithSameHashCode(string first, string second)
	{
		var rbac1 = Rbac.Parse(first);
		var rbac2 = Rbac.Parse(second);
		
		Assert.True(rbac1 == rbac2);
		Assert.True(rbac1.Equals(rbac2));
		Assert.Equal(rbac1.GetHashCode(), rbac2.GetHashCode());
	}
	
	[Theory]
	[InlineData("users.read", "users.update")]
	[InlineData("users.read", "roles.read")]
	[InlineData("users.read.abc", "users.read.ABC")]
	[InlineData("user-1.users.read.*", "USER-1.users.read.*")]
	[InlineData("users.read", "users.read.abc")]
	public void Rbac_DifferentPermissions_AreNotEqual(string first, string second)
	{
		Assert.False(Rbac.Parse(first) == Rbac.Parse(second));
	}
	
	[Fact]
	public void Rbac_EncodedAndPlainDotInSegment_AreEqualWithSameHashCode()
	{
		var encoded = new Rbac(new RbacSegment("user-1"), new RbacSegment("users"), new RbacSegment("read"), new RbacSegment("a%2Eb"));
		var plain = new Rbac(new RbacSegment("user-1"), new RbacSegment("users"), new RbacSegment("read"), new RbacSegment("a.b"));
		
		Assert.True(encoded == plain);
		Assert.Equal(encoded.GetHashCode(), plain.GetHashCode());
	}
	
	[Fact]
	public void Rbac_EqualPermissions_AreDeduplicatedInHashSet()
	{
		var rbacs = new HashSet<Rbac> { Rbac.Parse("users.read"), Rbac.Parse("USERS.READ"), Rbac.Parse("users.update") };
		
		Assert.Equal(2, rbacs.Count);
	}
	
	#endregion
	
	#region Ubac Equality
	
	[Fact]
	public void Ubac_ResourceAndActionInDifferentCase_AreEqualWithSameHashCode()
	{
		var ubac1 = Ubac.Parse("users.read.abc");
		var ubac2 = Ubac.Parse("USERS.Read.abc");
		
		Assert.True(ubac1 == ubac2);
		Assert.Equal(ubac1.GetHashCode(), ubac2.GetHashCode());
	}
	
	[Theory]
	[InlineData("users.read", "users.update")]
	[InlineData("users.read.abc", "users.read.ABC")]
	public void Ubac_DifferentPermissions_AreNotEqual(string first, string second)
	{
		Assert.False(Ubac.Parse(first) == Ubac.Parse(second));
	}
	
	#endregion
}
