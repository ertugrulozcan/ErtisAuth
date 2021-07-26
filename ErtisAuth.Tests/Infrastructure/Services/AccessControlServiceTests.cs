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

		/*
			# Admin Role Tests
			
			*.users.read.*
			*.users.create.*
			*.users.update.*
			*.users.delete.*
			*.users.read.permit_user_id
			*.users.create.permit_user_id
			*.users.update.permit_user_id
			*.users.delete.permit_user_id
			*.users.read.forbid_user_id
			*.users.create.forbid_user_id
			*.users.update.forbid_user_id
			*.users.delete.forbid_user_id
			*.users.read.other_user_id
			*.users.create.other_user_id
			*.users.update.other_user_id
			*.users.delete.other_user_id
		
			subject_id.users.read.*
			subject_id.users.create.*
			subject_id.users.update.*
			subject_id.users.delete.*
			subject_id.users.read.permit_user_id
			subject_id.users.create.permit_user_id
			subject_id.users.update.permit_user_id
			subject_id.users.delete.permit_user_id
			subject_id.users.read.forbid_user_id
			subject_id.users.create.forbid_user_id
			subject_id.users.update.forbid_user_id
			subject_id.users.delete.forbid_user_id
			subject_id.users.read.other_user_id
			subject_id.users.create.other_user_id
			subject_id.users.update.other_user_id
			subject_id.users.delete.other_user_id
		*/

		#endregion
	}
}