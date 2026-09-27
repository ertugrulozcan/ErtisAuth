using System.Dynamic;
using Ertis.Core.Collections;
using DynamicObject = Ertis.Schema.Dynamics.DynamicObject;
using ErtisAuth.Abstractions.Services;
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
/// Users always store the slug of their user type, whether the user type was given by name or slug.
/// </summary>
public class UserServiceUserTypeTests
{
	#region Constants
	
	private const string MembershipId = "5f8a1b2c3d4e5f6a7b8c9d00";
	
	private const string UserId = "5f8a1b2c3d4e5f6a7b8c9d0e";
	
	private const string UserTypeName = "Standard Customer";
	
	private const string UserTypeSlug = "standard-customer";
	
	#endregion
	
	#region Fields
	
	private readonly IMembershipService _membershipService = Substitute.For<IMembershipService>();
	
	private readonly IUserTypeService _userTypeService = Substitute.For<IUserTypeService>();
	
	private readonly IRoleService _roleService = Substitute.For<IRoleService>();
	
	private readonly IUserRepository _repository = Substitute.For<IUserRepository>();
	
	private BsonDocument? _persistedDocument;
	
	#endregion
	
	#region Constructors
	
	public UserServiceUserTypeTests()
	{
		var membership = new Membership
		{
			Id = MembershipId,
			Name = "Test Membership",
			SecretKey = "secret",
			HashAlgorithm = "SHA2-256"
		};
		
		this._membershipService.GetAsync(MembershipId, Arg.Any<CancellationToken>()).Returns(membership);
		
		// The user type can be resolved by both its name and its slug
		var userType = new UserType
		{
			Id = "user-type-id",
			Name = UserTypeName,
			MembershipId = MembershipId,
			AllowAdditionalProperties = true
		};
		
		this._userTypeService.GetByNameOrSlugAsync(MembershipId, UserTypeName, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(userType);
		this._userTypeService.GetByNameOrSlugAsync(MembershipId, UserTypeSlug, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(userType);
		
		this._roleService.GetBySlugAsync("user", MembershipId, Arg.Any<CancellationToken>()).Returns(new Role
		{
			Id = "user-role-id",
			Name = "user",
			MembershipId = MembershipId
		});
		
		var collection = new PaginationCollection<object>
		{
			Count = 1,
			Items = new object[] { CreateStoredUser() }
		};
		
		this._repository
			.FindAsync(default(string)!, null, null, null, default(Sorting))
			.ReturnsForAnyArgs(collection);
		
		this._repository
			.FindAsync(default(string)!, null, null, null, null, null, null, null)
			.ReturnsForAnyArgs(collection);
		
		this._repository
			.FindOneAsync(UserId, Arg.Any<CancellationToken>())
			.Returns(_ => CreateStoredUser());
		
		this._repository
			.UpdateAsync(null!)
			.ReturnsForAnyArgs(x =>
			{
				this._persistedDocument = x.ArgAt<BsonDocument>(0);
				return x.ArgAt<object>(0);
			});
		
		this._repository
			.InsertAsync(null!)
			.ReturnsForAnyArgs(x =>
			{
				this._persistedDocument = x.ArgAt<BsonDocument>(0);
				return x.ArgAt<object>(0);
			});
	}
	
	#endregion
	
	#region Helpers
	
	private static ExpandoObject CreateStoredUser()
	{
		dynamic user = new ExpandoObject();
		user._id = UserId;
		user.username = "john.doe";
		user.email_address = "john.doe@example.com";
		user.firstname = "John";
		user.lastname = "Doe";
		user.role = "user";
		user.user_type = UserTypeSlug;
		user.is_active = true;
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
			new AccessControlService(),
			Substitute.For<IEventService>(),
			Substitute.For<IJwtService>(),
			Substitute.For<IMailHookService>(),
			this._repository,
			NullLogger<UserService>.Instance);
	}
	
	private static Utilizer Self()
	{
		return new Utilizer
		{
			Id = UserId,
			Username = "john.doe",
			MembershipId = MembershipId,
			Role = "user",
			Type = Utilizer.UtilizerType.User
		};
	}
	
	private static DynamicObject Model(Action<dynamic> change)
	{
		dynamic model = new ExpandoObject();
		change(model);
		return new DynamicObject(model);
	}
	
	#endregion
	
	#region Create
	
	[Theory]
	[InlineData(UserTypeName)]
	[InlineData(UserTypeSlug)]
	public async Task CreateAsync_WithUserTypeGivenByNameOrSlug_StoresSlug(string userType)
	{
		var model = Model(x =>
		{
			x.username = "jane.doe";
			x.email_address = "jane.doe@example.com";
			x.firstname = "Jane";
			x.lastname = "Doe";
			x.role = "user";
			x.user_type = userType;
			x.password = "P@ssw0rd!";
		});
		
		await this.CreateUserService().CreateAsync(Utilizer.GetSystemUtilizer(MembershipId), MembershipId, model, cancellationToken: TestContext.Current.CancellationToken);
		
		Assert.NotNull(this._persistedDocument);
		Assert.Equal(UserTypeSlug, this._persistedDocument["user_type"].AsString);
	}
	
	#endregion
	
	#region Update
	
	[Fact]
	public async Task UpdateAsync_SelfSendingOwnUserTypeByName_IsNotSeenAsChangeAndStoresSlug()
	{
		// Before normalization the name differed from the stored slug, so it was treated as a privileged change (403)
		// and as a user type change (UserTypeImmutable)
		var model = Model(x =>
		{
			x.firstname = "Johnny";
			x.user_type = UserTypeName;
		});
		
		await this.CreateUserService().UpdateAsync(Self(), MembershipId, UserId, model, fireEvent: false, cancellationToken: TestContext.Current.CancellationToken);
		
		Assert.NotNull(this._persistedDocument);
		Assert.Equal("Johnny", this._persistedDocument["firstname"].AsString);
		Assert.Equal(UserTypeSlug, this._persistedDocument["user_type"].AsString);
	}
	
	[Fact]
	public async Task UpdateAsync_WithoutUserType_KeepsStoredSlug()
	{
		await this.CreateUserService().UpdateAsync(Self(), MembershipId, UserId, Model(x => x.firstname = "Johnny"), fireEvent: false, cancellationToken: TestContext.Current.CancellationToken);
		
		Assert.NotNull(this._persistedDocument);
		Assert.Equal(UserTypeSlug, this._persistedDocument["user_type"].AsString);
	}
	
	#endregion
}
