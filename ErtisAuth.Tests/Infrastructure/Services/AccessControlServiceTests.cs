using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Users;
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
		private IUserService userService;
		private IAccessControlService accessControlService;

		#endregion
		
		#region Setup

		[SetUp]
		public void Setup()
		{
			this.roleService = new MockRoleService();
			this.userService = new MockUserService();
			this.userService.OnCreated += (_, _) => { };
			this.userService.OnUpdated += (_, _) => { };
			this.userService.OnDeleted += (_, _) => { };
			
			this.accessControlService = new AccessControlService(this.roleService);
		}

		#endregion

		#region Test Methods

		[Test]
		public void Permission_Check_All_Users_Read_All_Return_True_Test()
		{
			var role = this.roleService.GetBySlug("admin", "test_membership");
			const string rbac = "*.users.read.*";
			var hasPermission = this.accessControlService.HasPermission(role, rbac);
			Assert.That(hasPermission);
		}
		
		[Test]
		public void Permission_Check_All_Users_Create_All_Return_True_Test()
		{
			var role = this.roleService.GetBySlug("admin", "test_membership");
			const string rbac = "*.users.create.*";
			var hasPermission = this.accessControlService.HasPermission(role, rbac);
			Assert.That(hasPermission);
		}
		
		[Test]
		public void Permission_Check_All_Users_Update_All_Return_True_Test()
		{
			var role = this.roleService.GetBySlug("admin", "test_membership");
			const string rbac = "*.users.update.*";
			var hasPermission = this.accessControlService.HasPermission(role, rbac);
			Assert.That(hasPermission);
		}
		
		[Test]
		public void Permission_Check_All_Users_Delete_All_Return_True_Test()
		{
			var role = this.roleService.GetBySlug("admin", "test_membership");
			const string rbac = "*.users.delete.*";
			var hasPermission = this.accessControlService.HasPermission(role, rbac);
			Assert.That(hasPermission);
		}
		
		[Test]
		public void Permission_Check_All_Users_Read_All_With_Permit_User_Return_True_Test()
		{
			var role = this.roleService.GetBySlug("admin", "test_membership");
			const string rbac = "*.users.read.user_1";
			var hasPermission = this.accessControlService.HasPermission(role, rbac);
			Assert.That(hasPermission);
		}
		
		[Test]
		public void Permission_Check_All_Users_Create_All_With_Permit_User_Return_True_Test()
		{
			var role = this.roleService.GetBySlug("admin", "test_membership");
			const string rbac = "*.users.create.user_1";
			var hasPermission = this.accessControlService.HasPermission(role, rbac);
			Assert.That(hasPermission);
		}
		
		[Test]
		public void Permission_Check_All_Users_Update_All_With_Permit_User_Return_True_Test()
		{
			var role = this.roleService.GetBySlug("admin", "test_membership");
			const string rbac = "*.users.update.user_1";
			var hasPermission = this.accessControlService.HasPermission(role, rbac);
			Assert.That(hasPermission);
		}
		
		[Test]
		public void Permission_Check_All_Users_Delete_All_With_Permit_User_Return_True_Test()
		{
			var role = this.roleService.GetBySlug("admin", "test_membership");
			const string rbac = "*.users.delete.user_1";
			var hasPermission = this.accessControlService.HasPermission(role, rbac);
			Assert.That(hasPermission);
		}
		
		[Test]
		public void Permission_Check_All_Users_Read_All_With_Forbid_User_Return_True_Test()
		{
			var role = this.roleService.GetBySlug("readonly", "test_membership");
			const string rbac = "*.users.read.user_2";
			var hasPermission = this.accessControlService.HasPermission(role, rbac);
			Assert.That(hasPermission);
		}
		
		[Test]
		public void Permission_Check_All_Users_Create_All_With_Forbid_User_Return_False_Test()
		{
			var role = this.roleService.GetBySlug("readonly", "test_membership");
			const string rbac = "*.users.create.user_2";
			var hasPermission = this.accessControlService.HasPermission(role, rbac);
			Assert.That(!hasPermission);
		}
		
		[Test]
		public void Permission_Check_All_Users_Update_All_With_Forbid_User_Return_False_Test()
		{
			var role = this.roleService.GetBySlug("readonly", "test_membership");
			const string rbac = "*.users.update.user_2";
			var hasPermission = this.accessControlService.HasPermission(role, rbac);
			Assert.That(!hasPermission);
		}
		
		[Test]
		public void Permission_Check_All_Users_Delete_All_With_Forbid_User_Return_False_Test()
		{
			var role = this.roleService.GetBySlug("readonly", "test_membership");
			const string rbac = "*.users.delete.user_2";
			var hasPermission = this.accessControlService.HasPermission(role, rbac);
			Assert.That(!hasPermission);
		}
		
		[Test]
		public void Ubac_Permission_Check_All_Users_Read_All_Return_True_Test()
		{
			var dynamicObject = this.userService.GetAsync("test_membership", "user_1").ConfigureAwait(false).GetAwaiter().GetResult();
			var user = dynamicObject.Deserialize<User>();
			const string rbac = "*.users.read.*";
			var hasPermission = this.accessControlService.HasPermission(user, rbac);
			Assert.That(hasPermission);
		}
		
		[Test]
		public void Ubac_Permission_Check_All_Users_Create_All_Return_False_Test()
		{
			var dynamicObject = this.userService.GetAsync("test_membership", "user_1").ConfigureAwait(false).GetAwaiter().GetResult();
			var user = dynamicObject.Deserialize<User>();
			const string rbac = "*.users.create.*";
			var hasPermission = this.accessControlService.HasPermission(user, rbac);
			Assert.That(hasPermission);
		}

		[Test]
		public void Ubac_Permission_Check_All_Users_Update_All_Return_True_Test()
		{
			var dynamicObject = this.userService.GetAsync("test_membership", "user_1").ConfigureAwait(false).GetAwaiter().GetResult();
			var user = dynamicObject.Deserialize<User>();
			const string rbac = "*.users.update.*";
			var hasPermission = this.accessControlService.HasPermission(user, rbac);
			Assert.That(hasPermission);
		}
		
		[Test]
		public void Ubac_Permission_Check_All_Users_Delete_All_Return_True_Test()
		{
			var dynamicObject = this.userService.GetAsync("test_membership", "user_1").ConfigureAwait(false).GetAwaiter().GetResult();
			var user = dynamicObject.Deserialize<User>();
			const string rbac = "*.users.delete.*";
			var hasPermission = this.accessControlService.HasPermission(user, rbac);
			Assert.That(hasPermission);
		}

		[Test]
		public void Ubac_Permission_Check_All_Users_Create_All_Return_True_Test()
		{
			var dynamicObject = this.userService.GetAsync("test_membership", "user_1").ConfigureAwait(false).GetAwaiter().GetResult();
			var user = dynamicObject.Deserialize<User>();
			const string rbac = "*.users.create.*";
			var hasPermission = this.accessControlService.HasPermission(user, rbac);
			Assert.That(hasPermission);
		}

		[Test]
		public void Ubac_Permission_Check_All_Users_Update_All_Return_False_Test()
		{
			var dynamicObject = this.userService.GetAsync("test_membership", "user_1").ConfigureAwait(false).GetAwaiter().GetResult();
			var user = dynamicObject.Deserialize<User>();
			const string rbac = "*.users.update.*";
			var hasPermission = this.accessControlService.HasPermission(user, rbac);
			Assert.That(hasPermission);
		}
		
		[Test]
		public void Ubac_Permission_Check_All_Users_Delete_All_Return_False_Test()
		{
			var dynamicObject = this.userService.GetAsync("test_membership", "user_1").ConfigureAwait(false).GetAwaiter().GetResult();
			var user = dynamicObject.Deserialize<User>();
			const string rbac = "*.users.delete.*";
			var hasPermission = this.accessControlService.HasPermission(user, rbac);
			Assert.That(hasPermission);
		}
		
		#endregion
	}
}