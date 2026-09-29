using Ertis.Core.Models.Resources;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Infrastructure.Helpers;

namespace ErtisAuth.Infrastructure.Tests.Helpers;

/// <summary>
/// The sys info rule: UTC times, the utilizer by its name ("system" for the system), creation info kept on update.
/// </summary>
public class SysInfoHelperTests
{
	#region Helpers
	
	private static Utilizer User(string username) => new()
	{
		Id = "user-id",
		Username = username,
		Role = "admin",
		Type = Utilizer.UtilizerType.User,
		MembershipId = "membership-id"
	};
	
	#endregion
	
	#region Tests
	
	[Fact]
	public void Created_RecordsTheUtilizerAndTheUtcTime()
	{
		var before = DateTime.UtcNow;
		
		var sys = SysInfoHelper.Created(User("jane"));
		
		Assert.Equal("jane", sys.CreatedBy);
		Assert.Equal(DateTimeKind.Utc, sys.CreatedAt!.Value.Kind);
		Assert.InRange(sys.CreatedAt.Value, before, DateTime.UtcNow);
		Assert.Null(sys.ModifiedAt);
		Assert.Null(sys.ModifiedBy);
	}
	
	[Fact]
	public void SystemUtilizer_IsRecordedAsSystem()
	{
		var sys = SysInfoHelper.Created(Utilizer.GetSystemUtilizer("membership-id"));
		
		Assert.Equal(SysInfoHelper.SystemUtilizerName, sys.CreatedBy);
	}
	
	[Fact]
	public void Modified_KeepsTheCreationInfo()
	{
		var createdAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
		var current = new SysModel { CreatedAt = createdAt, CreatedBy = "jane", ModifiedAt = createdAt, ModifiedBy = "jane" };
		
		var sys = SysInfoHelper.Modified(current, User("john"));
		
		Assert.Equal(createdAt, sys.CreatedAt);
		Assert.Equal("jane", sys.CreatedBy);
		Assert.Equal("john", sys.ModifiedBy);
		Assert.True(sys.ModifiedAt > createdAt);
	}
	
	/// <summary>
	/// As on master: a stored version without sys info gets the update as its creation.
	/// </summary>
	[Fact]
	public void Modified_WithoutCurrentSys_UsesTheUpdateAsTheCreation()
	{
		var sys = SysInfoHelper.Modified(null, User("john"));
		
		Assert.Equal("john", sys.CreatedBy);
		Assert.Equal("john", sys.ModifiedBy);
		Assert.Equal(sys.ModifiedAt, sys.CreatedAt);
	}
	
	[Fact]
	public void SetCreated_ReplacesTheSysInfoSentByTheCaller()
	{
		var role = new Role { Name = "Editor", MembershipId = "membership-id", Sys = new SysModel { CreatedBy = "forged", CreatedAt = DateTime.MinValue } };
		
		SysInfoHelper.SetCreated(role, User("jane"));
		
		Assert.Equal("jane", role.Sys!.CreatedBy);
		Assert.NotEqual(DateTime.MinValue, role.Sys.CreatedAt);
	}
	
	[Fact]
	public void SetModified_TakesTheCreationInfoFromTheStoredVersion()
	{
		var createdAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
		var stored = new Role { Name = "Editor", MembershipId = "membership-id", Sys = new SysModel { CreatedAt = createdAt, CreatedBy = "jane" } };
		var model = new Role { Name = "Editor", MembershipId = "membership-id", Sys = new SysModel { CreatedBy = "forged", CreatedAt = DateTime.MinValue } };
		
		SysInfoHelper.SetModified(model, stored, User("john"));
		
		Assert.Equal(createdAt, model.Sys!.CreatedAt);
		Assert.Equal("jane", model.Sys.CreatedBy);
		Assert.Equal("john", model.Sys.ModifiedBy);
	}
	
	[Fact]
	public void ModelWithoutSysInfo_IsLeftAsItIs()
	{
		var model = new object();
		
		SysInfoHelper.SetCreated(model, User("jane"));
		SysInfoHelper.SetModified(model, model, User("jane"));
	}
	
	#endregion
}
