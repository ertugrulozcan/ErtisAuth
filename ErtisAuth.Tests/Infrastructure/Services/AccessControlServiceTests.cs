using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.Tests.Mocks.Services;
using NUnit.Framework;

namespace ErtisAuth.Tests.Infrastructure.Services
{
	[TestFixture]
	public class AccessControlServiceTests
	{
		#region Services

		private IRoleService roleService;
		private IAccessControlService accessControlService;

		#endregion
		
		#region Setup

		[SetUp]
		public void Setup()
		{
			this.roleService = new MockRoleService();
			this.accessControlService = new AccessControlService(this.roleService);
		}

		#endregion

		#region Test Methods

		[Test]
		public void Permission_Check_All_Users_Read_All_Return_True_Test()
		{
			var role = this.roleService.GetByName("admin", "test_membership");
			const string rbac = "*.users.read.*";
			var hasPermission = this.accessControlService.HasPermission(role, rbac);
			Assert.IsTrue(hasPermission);
		}
		
		[Test]
		public void Permission_Check_All_Users_Create_All_Return_True_Test()
		{
			var role = this.roleService.GetByName("admin", "test_membership");
			const string rbac = "*.users.create.*";
			var hasPermission = this.accessControlService.HasPermission(role, rbac);
			Assert.IsTrue(hasPermission);
		}
		
		[Test]
		public void Permission_Check_All_Users_Update_All_Return_True_Test()
		{
			var role = this.roleService.GetByName("admin", "test_membership");
			const string rbac = "*.users.update.*";
			var hasPermission = this.accessControlService.HasPermission(role, rbac);
			Assert.IsTrue(hasPermission);
		}
		
		[Test]
		public void Permission_Check_All_Users_Delete_All_Return_True_Test()
		{
			var role = this.roleService.GetByName("admin", "test_membership");
			const string rbac = "*.users.delete.*";
			var hasPermission = this.accessControlService.HasPermission(role, rbac);
			Assert.IsTrue(hasPermission);
		}
		
		[Test]
		public void Permission_Check_All_Users_Read_All_With_Permit_User_Return_True_Test()
		{
			var role = this.roleService.GetByName("admin", "test_membership");
			const string rbac = "*.users.read.test_utilizer";
			var hasPermission = this.accessControlService.HasPermission(role, rbac);
			Assert.IsTrue(hasPermission);
		}
		
		[Test]
		public void Permission_Check_All_Users_Create_All_With_Permit_User_Return_True_Test()
		{
			var role = this.roleService.GetByName("admin", "test_membership");
			const string rbac = "*.users.create.test_utilizer";
			var hasPermission = this.accessControlService.HasPermission(role, rbac);
			Assert.IsTrue(hasPermission);
		}
		
		[Test]
		public void Permission_Check_All_Users_Update_All_With_Permit_User_Return_True_Test()
		{
			var role = this.roleService.GetByName("admin", "test_membership");
			const string rbac = "*.users.update.test_utilizer";
			var hasPermission = this.accessControlService.HasPermission(role, rbac);
			Assert.IsTrue(hasPermission);
		}
		
		[Test]
		public void Permission_Check_All_Users_Delete_All_With_Permit_User_Return_True_Test()
		{
			var role = this.roleService.GetByName("admin", "test_membership");
			const string rbac = "*.users.delete.test_utilizer";
			var hasPermission = this.accessControlService.HasPermission(role, rbac);
			Assert.IsTrue(hasPermission);
		}
		
		[Test]
		public void Permission_Check_All_Users_Read_All_With_Forbid_User_Return_True_Test()
		{
			var role = this.roleService.GetByName("readonly", "test_membership");
			const string rbac = "*.users.read.forbid_user_id";
			var hasPermission = this.accessControlService.HasPermission(role, rbac);
			Assert.IsTrue(hasPermission);
		}
		
		[Test]
		public void Permission_Check_All_Users_Create_All_With_Forbid_User_Return_False_Test()
		{
			var role = this.roleService.GetByName("readonly", "test_membership");
			const string rbac = "*.users.create.forbid_user_id";
			var hasPermission = this.accessControlService.HasPermission(role, rbac);
			Assert.IsFalse(hasPermission);
		}
		
		[Test]
		public void Permission_Check_All_Users_Update_All_With_Forbid_User_Return_False_Test()
		{
			var role = this.roleService.GetByName("readonly", "test_membership");
			const string rbac = "*.users.update.forbid_user_id";
			var hasPermission = this.accessControlService.HasPermission(role, rbac);
			Assert.IsFalse(hasPermission);
		}
		
		[Test]
		public void Permission_Check_All_Users_Delete_All_With_Forbid_User_Return_False_Test()
		{
			var role = this.roleService.GetByName("readonly", "test_membership");
			const string rbac = "*.users.delete.forbid_user_id";
			var hasPermission = this.accessControlService.HasPermission(role, rbac);
			Assert.IsFalse(hasPermission);
		}

		#endregion
	}
}