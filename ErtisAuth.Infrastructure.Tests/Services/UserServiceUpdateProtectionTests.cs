using System.Dynamic;
using Ertis.Core.Collections;
using DynamicObject = Ertis.Schema.Dynamics.DynamicObject;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using NSubstitute;

namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// Field protections of UserService.UpdateAsync:
/// privileged fields need a real users.update permission, server managed fields are only writable by the system.
/// </summary>
public class UserServiceUpdateProtectionTests
{
	#region Constants
	
	private const string MembershipId = "5f8a1b2c3d4e5f6a7b8c9d00";
	
	private const string UserId = "5f8a1b2c3d4e5f6a7b8c9d0e";
	
	private const string ManagerId = "5f8a1b2c3d4e5f6a7b8c9d0f";
	
	#endregion
	
	#region Fields
	
	private readonly IMembershipService _membershipService = Substitute.For<IMembershipService>();
	
	private readonly IUserTypeService _userTypeService = Substitute.For<IUserTypeService>();
	
	private readonly IRoleService _roleService = Substitute.For<IRoleService>();
	
	private readonly IUserRepository _repository = Substitute.For<IUserRepository>();
	
	private BsonDocument? _persistedDocument;
	
	#endregion
	
	#region Constructors
	
	public UserServiceUpdateProtectionTests()
	{
		var membership = new Membership
		{
			Id = MembershipId,
			Name = "Test Membership",
			SecretKey = "secret",
			HashAlgorithm = "SHA2-256"
		};
		
		this._membershipService.GetAsync(MembershipId, Arg.Any<CancellationToken>()).Returns(membership);
		
		foreach (var userTypeName in new[] { "user", "premium" })
		{
			this._userTypeService
				.GetByNameOrSlugAsync(MembershipId, userTypeName, Arg.Any<bool>(), Arg.Any<CancellationToken>())
				.Returns(new UserType
				{
					Id = $"{userTypeName}-type-id",
					Name = userTypeName,
					MembershipId = MembershipId,
					AllowAdditionalProperties = true
				});
		}
		
		this.SetupRole("user");
		this.SetupRole("admin", permissions: ["*"]);
		this.SetupRole("manager", permissions: ["users.*"]);
		this.SetupRole("no-self-update", forbidden: ["users.update"]);
		
		this.SetupStoredUser();
	}
	
	#endregion
	
	#region Helpers
	
	private void SetupRole(string slug, string[]? permissions = null, string[]? forbidden = null)
	{
		this._roleService.GetBySlugAsync(slug, MembershipId, Arg.Any<CancellationToken>()).Returns(new Role
		{
			Id = $"{slug}-role-id",
			Name = slug,
			MembershipId = MembershipId,
			Permissions = permissions,
			Forbidden = forbidden
		});
	}
	
	private void SetupStoredUser(string role = "user")
	{
		var collection = new PaginationCollection<object>
		{
			Count = 1,
			Items = new object[] { CreateStoredUser(role) }
		};
		
		this._repository
			.FindAsync(default(string)!, default, default, default, default(Sorting), default)
			.ReturnsForAnyArgs(collection);
			
		this._repository
			.FindAsync(default(string)!, default, default, default, default(Sorting), default, default, default)
			.ReturnsForAnyArgs(collection);
			
		this._repository
			.UpdateAsync(default!, default!, default, default)
			.ReturnsForAnyArgs(x =>
			{
				this._persistedDocument = x.ArgAt<BsonDocument>(0);
				return x.ArgAt<object>(0);
			});
			
		this._repository
			.FindOneAsync(UserId, Arg.Any<CancellationToken>())
			.Returns(_ => CreateStoredUser(role));
	}
	
	private static ExpandoObject CreateStoredUser(string role)
	{
		dynamic user = new ExpandoObject();
		user._id = UserId;
		user.username = "john.doe";
		user.email_address = "john.doe@example.com";
		user.firstname = "John";
		user.lastname = "Doe";
		user.role = role;
		user.user_type = "user";
		user.is_active = true;
		user.permissions = new[] { "users.read" };
		user.membership_id = MembershipId;
		user.password_hash = "hash";
		return user;
	}
	
	private UserService CreateUserService()
	{
		return new UserService(
			this._userTypeService,
			this._membershipService,
			this._roleService,
			new AccessControlService(this._roleService),
			Substitute.For<IEventService>(),
			Substitute.For<IJwtService>(),
			Substitute.For<IMailHookService>(),
			this._repository,
			NullLogger<UserService>.Instance);
	}
	
	private static Utilizer Self(string role = "user", string[]? permissions = null, string[]? scopes = null)
	{
		return new Utilizer
		{
			Id = UserId,
			Username = "john.doe",
			MembershipId = MembershipId,
			Role = role,
			Type = Utilizer.UtilizerType.User,
			Permissions = permissions ?? ["users.read"],
			Scopes = scopes
		};
	}
	
	private static Utilizer Manager(string role = "manager", string[]? permissions = null, string[]? scopes = null)
	{
		return new Utilizer
		{
			Id = ManagerId,
			Username = "manager",
			MembershipId = MembershipId,
			Role = role,
			Type = Utilizer.UtilizerType.User,
			Permissions = permissions,
			Scopes = scopes
		};
	}
	
	private static DynamicObject Update(Action<dynamic> change)
	{
		dynamic model = new ExpandoObject();
		change(model);
		return new DynamicObject(model);
	}
	
	private async Task<DynamicObject?> UpdateAsync(Utilizer utilizer, DynamicObject model)
	{
		return await this.CreateUserService().UpdateAsync(utilizer, MembershipId, UserId, model, fireEvent: false, cancellationToken: TestContext.Current.CancellationToken);
	}
	
	private async Task AssertAccessDeniedAsync(Utilizer utilizer, DynamicObject model)
	{
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.UpdateAsync(utilizer, model));
		
		Assert.Equal("AccessDenied", exception.ErrorCode);
		await this._repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default!, default, TestContext.Current.CancellationToken);
	}
	
	#endregion
	
	#region Self Update (own-update exception)
	
	[Fact]
	public async Task UpdateAsync_SelfUpdatingProfileFields_Succeeds()
	{
		await this.UpdateAsync(Self(), Update(x => x.firstname = "Johnny"));
		
		Assert.NotNull(this._persistedDocument);
		Assert.Equal("Johnny", this._persistedDocument["firstname"].AsString);
	}
	
	[Fact]
	public async Task UpdateAsync_SelfGrantingItselfAllPermissions_IsDenied()
	{
		await this.AssertAccessDeniedAsync(Self(), Update(x => x.permissions = new[] { "*" }));
	}
	
	[Theory]
	[InlineData("role")]
	[InlineData("forbidden")]
	[InlineData("is_active")]
	[InlineData("user_type")]
	public async Task UpdateAsync_SelfChangingPrivilegedField_IsDenied(string field)
	{
		var model = Update(x =>
		{
			var dictionary = (IDictionary<string, object?>)x;
			dictionary[field] = field switch
			{
				"role" => "admin",
				"forbidden" => new[] { "users.delete" },
				"is_active" => false,
				_ => "premium"
			};
		});
		
		await this.AssertAccessDeniedAsync(Self(), model);
	}
	
	[Fact]
	public async Task UpdateAsync_SelfSendingUnchangedPrivilegedFields_Succeeds()
	{
		// Clients typically GET the record and PUT it back with the privileged fields unchanged.
		await this.UpdateAsync(Self(), Update(x =>
		{
			x.firstname = "Johnny";
			x.role = "user";
			x.user_type = "user";
			x.is_active = true;
			x.permissions = new[] { "users.read" };
		}));
		
		Assert.NotNull(this._persistedDocument);
	}
	
	[Fact]
	public void UpdateAsync_SelfWhenRoleForbidsUserUpdate_IsDeniedByAuthorization()
	{
		// The own-update exception no longer applies when the role forbids users.update.
		var accessControlService = new AccessControlService(this._roleService);
		var role = new Role { Id = "role-id", Name = "no-self-update", MembershipId = MembershipId, Forbidden = ["users.update"] };
		var rbac = new Rbac(new RbacSegment(UserId), new RbacSegment("users"), Rbac.CrudActionSegments.Update, new RbacSegment(UserId));
		
		Assert.False(accessControlService.HasPermission(role, rbac, Self("no-self-update", permissions: [])));
	}
	
	#endregion
	
	#region Privileged Updates
	
	[Fact]
	public async Task UpdateAsync_ManagerWithUsersUpdatePermission_CanChangePrivilegedFields()
	{
		await this.UpdateAsync(Manager(), Update(x =>
		{
			x.role = "admin";
			x.permissions = new[] { "roles.read" };
			x.is_active = false;
		}));
		
		Assert.NotNull(this._persistedDocument);
		Assert.Equal("admin", this._persistedDocument["role"].AsString);
	}
	
	[Fact]
	public async Task UpdateAsync_UtilizerGrantedByUbac_CanChangePrivilegedFields()
	{
		await this.UpdateAsync(Manager(role: "user", permissions: ["users.update"]), Update(x => x.role = "admin"));
		
		Assert.NotNull(this._persistedDocument);
	}
	
	[Fact]
	public async Task UpdateAsync_ManagerWithScopedTokenNotCoveringUpdate_IsDenied()
	{
		await this.AssertAccessDeniedAsync(Manager(scopes: ["users.read"]), Update(x => x.role = "admin"));
	}
	
	[Fact]
	public async Task UpdateAsync_SelfWithAdminRole_CanChangeOwnPrivilegedFields()
	{
		await this.UpdateAsync(Self(role: "admin"), Update(x => x.permissions = new[] { "*" }));
		
		Assert.NotNull(this._persistedDocument);
	}
	
	
	[Fact]
	public async Task UpdateAsync_WithSameUbacInPermissionsAndForbidden_IsRejectedAsConflict()
	{
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.UpdateAsync(Manager(role: "admin"), Update(x =>
		{
			x.permissions = new[] { "applications.update.123" };
			x.forbidden = new[] { "applications.update.123" };
		})));
		
		Assert.Equal("UbacsConflicted", exception.ErrorCode);
	}
	
	#endregion
	
	#region Server Managed and Immutable Fields
	
	[Theory]
	[InlineData("source_provider")]
	[InlineData("connected_accounts")]
	public async Task UpdateAsync_ExternalUtilizerSendingServerManagedField_IsIgnored(string field)
	{
		var model = Update(x =>
		{
			var dictionary = (IDictionary<string, object?>)x;
			dictionary[field] = field == "source_provider" ? "Google" : new[] { new Dictionary<string, object?> { ["Provider"] = "Google", ["UserId"] = "victim-google-id" } };
		});
		
		await this.UpdateAsync(Manager(role: "admin"), model);
		
		Assert.NotNull(this._persistedDocument);
		Assert.False(this._persistedDocument.Contains(field));
	}
	
	[Fact]
	public async Task UpdateAsync_SystemUtilizer_CanWriteConnectedAccounts()
	{
		var model = Update(x => x.connected_accounts = new[] { new Dictionary<string, object?> { ["Provider"] = "Google", ["UserId"] = "google-id" } });
		
		await this.UpdateAsync(Utilizer.GetSystemUtilizer(MembershipId), model);
		
		Assert.NotNull(this._persistedDocument);
		Assert.True(this._persistedDocument.Contains("connected_accounts"));
	}
	
	[Fact]
	public async Task UpdateAsync_SendingIdAndMembershipId_DoesNotChangeThem()
	{
		await this.UpdateAsync(Manager(role: "admin"), Update(x =>
		{
			x._id = "000000000000000000000001";
			x.membership_id = "another-membership";
		}));
		
		Assert.NotNull(this._persistedDocument);
		Assert.False(this._persistedDocument.Contains("_id"));
		Assert.Equal(MembershipId, this._persistedDocument["membership_id"].AsString);
	}
	
	#endregion
}
