using Ertis.Core.Collections;
using Ertis.Schema.Dynamics;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Models.Setup;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// The one-time setup of a fresh installation: authorized by a token the operator inserted into the "setup" collection,
/// possible only while no membership exists, compensated when a step fails.
/// </summary>
public class SetupServiceTests
{
	#region Constants
	
	private const string SetupToken = "0123456789abcdef0123456789abcdef0123456789abcdef";
	
	#endregion
	
	#region Fields
	
	private readonly ISetupTokenRepository _setupTokenRepository = Substitute.For<ISetupTokenRepository>();
	
	private readonly IMembershipService _membershipService = Substitute.For<IMembershipService>();
	
	private readonly IRoleService _roleService = Substitute.For<IRoleService>();
	
	private readonly IUserService _userService = Substitute.For<IUserService>();
	
	private readonly IUserTypeService _userTypeService = Substitute.For<IUserTypeService>();
	
	private readonly IApplicationService _applicationService = Substitute.For<IApplicationService>();
	
	private readonly List<SetupToken> _setupTokens;
	
	private readonly List<Membership> _memberships = [];
	
	private readonly List<string> _deleted = [];
	
	private bool _isSetupCollectionDropped;
	
	#endregion
	
	#region Constructors
	
	public SetupServiceTests()
	{
		this._setupTokens = InMemoryRepository.Setup(this._setupTokenRepository, x => x.Id ??= ObjectId.GenerateNewId().ToString());
		this._setupTokenRepository.DropAsync(Arg.Any<CancellationToken>()).Returns(_ =>
		{
			this._setupTokens.Clear();
			this._isSetupCollectionDropped = true;
			return Task.CompletedTask;
		});
		
		this._membershipService
			.GetAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<SortDirection?>(), Arg.Any<CancellationToken>())
			.Returns(_ => new PaginationCollection<Membership> { Count = this._memberships.Count, Items = this._memberships.ToArray() });
		
		this._membershipService
			.CreateAsync(Arg.Any<Membership>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				var membership = callInfo.ArgAt<Membership>(0);
				membership.Id = ObjectId.GenerateNewId().ToString();
				this._memberships.Add(membership);
				return membership;
			});
		
		this._membershipService.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(callInfo => this.Delete("membership", callInfo.ArgAt<string>(0)));
		
		this._roleService
			.EnsureAdministratorRoleAsync(Arg.Any<Membership>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => new Role { Id = "role-id", Name = "Administrator", Slug = "admin", MembershipId = callInfo.ArgAt<Membership>(0).Id });
		
		this._roleService.DeleteAsync(Arg.Any<Utilizer>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(callInfo => this.Delete("role", callInfo.ArgAt<string>(2)));
		
		this._userTypeService
			.CreateAsync(Arg.Any<Utilizer>(), Arg.Any<string>(), Arg.Any<UserType>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				var userType = callInfo.ArgAt<UserType>(2);
				userType.Id = "user-type-id";
				return userType;
			});
		
		this._userTypeService.DeleteAsync(Arg.Any<Utilizer>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(callInfo => this.Delete("user type", callInfo.ArgAt<string>(2)));
		
		this._userService
			.CreateAsync(Arg.Any<Utilizer>(), Arg.Any<string>(), Arg.Any<DynamicObject>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				var user = callInfo.ArgAt<DynamicObject>(2);
				user.SetValue("_id", "user-id", true);
				return user;
			});
		
		this._userService.DeleteAsync(Arg.Any<Utilizer>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(callInfo => this.Delete("user", callInfo.ArgAt<string>(2)));
		
		this._applicationService
			.CreateWithSecretAsync(Arg.Any<Utilizer>(), Arg.Any<string>(), Arg.Any<Application>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => new ApplicationWithSecret(callInfo.ArgAt<Application>(2), "application-secret"));
	}
	
	#endregion
	
	#region Helpers
	
	private SetupService CreateService()
	{
		return new SetupService(this._setupTokenRepository, this._membershipService, this._roleService, this._userService, this._userTypeService, this._applicationService, NullLogger<SetupService>.Instance);
	}
	
	private bool Delete(string resource, string id)
	{
		this._deleted.Add(resource);
		if (resource == "membership")
		{
			this._memberships.RemoveAll(x => x.Id == id);
		}
		
		return true;
	}
	
	private void InsertSetupToken(string token = SetupToken)
	{
		this._setupTokens.Add(new SetupToken { Id = ObjectId.GenerateNewId().ToString(), Token = token });
	}
	
	private static Membership CreateMembership()
	{
		return new Membership { Name = "ErtisAuth", SecretKey = string.Empty, HashAlgorithm = "ARGON2ID", ExpiresIn = 3600, RefreshTokenExpiresIn = 7200 };
	}
	
	private static UserWithPassword CreateUser()
	{
		return new UserWithPassword
		{
			Username = "admin",
			EmailAddress = "admin@example.com",
			FirstName = "Admin",
			LastName = "User",
			Password = "P@ssw0rd!",
			Role = string.Empty,
			MembershipId = string.Empty
		};
	}
	
	private Task<SetupResult> SetupAsync(SetupService service, string? setupToken = SetupToken, Application? application = null)
	{
		return service.SetupAsync(setupToken, CreateMembership(), CreateUser(), application, TestContext.Current.CancellationToken);
	}
	
	private async Task AssertRejectedAsync(Func<Task> action, string errorCode)
	{
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(action);
		Assert.Equal(errorCode, exception.ErrorCode);
		await this._membershipService.DidNotReceiveWithAnyArgs().CreateAsync(default!);
	}
	
	#endregion
	
	#region Setup
	
	[Fact]
	public async Task SetupAsync_WithValidToken_CreatesTheResourcesAndDropsTheSetupCollection()
	{
		this.InsertSetupToken();
		var service = this.CreateService();
		
		var result = await this.SetupAsync(service, application: new Application { Name = "Server", Role = "admin", MembershipId = string.Empty });
		
		Assert.Equal(result.Membership.Id, Assert.Single(this._memberships).Id);
		Assert.Equal("admin", result.Role.Slug);
		Assert.Equal("admin", result.User.GetValue<string>("role"));
		Assert.Equal("application-secret", result.Application?.Secret);
		Assert.False(string.IsNullOrEmpty(result.Membership.SecretKey));
		Assert.True(this._isSetupCollectionDropped);
		Assert.True(await service.IsSetUpAsync(TestContext.Current.CancellationToken));
	}
	
	[Fact]
	public async Task SetupAsync_WhenAlreadySetUp_IsRejectedEvenWithAValidToken()
	{
		this.InsertSetupToken();
		this._memberships.Add(new Membership { Id = ObjectId.GenerateNewId().ToString(), Name = "Existing", SecretKey = "secret" });
		
		await this.AssertRejectedAsync(() => this.SetupAsync(this.CreateService()), "AlreadySetUp");
	}
	
	[Fact]
	public async Task SetupAsync_WithoutTheHeader_IsRejected()
	{
		this.InsertSetupToken();
		
		await this.AssertRejectedAsync(() => this.SetupAsync(this.CreateService(), setupToken: null), "SetupRejected");
	}
	
	[Fact]
	public async Task SetupAsync_WithoutAStoredToken_IsRejected()
	{
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.SetupAsync(this.CreateService()));
		
		Assert.Equal("SetupRejected", exception.ErrorCode);
		Assert.Contains("db.setup.insertOne", exception.Message);
	}
	
	[Fact]
	public async Task SetupAsync_WithWrongToken_IsRejectedAndKeepsTheStoredToken()
	{
		this.InsertSetupToken();
		
		await this.AssertRejectedAsync(() => this.SetupAsync(this.CreateService(), setupToken: SetupToken + "x"), "SetupRejected");
		Assert.Single(this._setupTokens);
	}
	
	[Fact]
	public async Task SetupAsync_WithAShortStoredToken_IsRejectedEvenWhenItMatches()
	{
		// A weak token would be guessable while the setup is pending
		this.InsertSetupToken("123456");
		
		await this.AssertRejectedAsync(() => this.SetupAsync(this.CreateService(), setupToken: "123456"), "SetupRejected");
	}
	
	[Fact]
	public async Task SetupAsync_WhileAnotherSetupIsInProgress_IsRejected()
	{
		this.InsertSetupToken();
		var membershipCreation = new TaskCompletionSource<Membership>();
		this._membershipService.CreateAsync(Arg.Any<Membership>(), Arg.Any<CancellationToken>()).Returns(membershipCreation.Task);
		var service = this.CreateService();
		
		var first = this.SetupAsync(service);
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.SetupAsync(service));
		
		Assert.Equal("SetupInProgress", exception.ErrorCode);
		membershipCreation.SetException(new InvalidOperationException("stop the first setup"));
		await Assert.ThrowsAsync<InvalidOperationException>(() => first);
	}
	
	#endregion
	
	#region Rollback
	
	[Fact]
	public async Task SetupAsync_WhenAStepFails_DeletesTheCreatedResourcesInReverseOrderAndKeepsTheToken()
	{
		// Otherwise the installation would count as set up without a usable administrator
		this.InsertSetupToken();
		this._userService
			.CreateAsync(Arg.Any<Utilizer>(), Arg.Any<string>(), Arg.Any<DynamicObject>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
			.ThrowsAsync(new InvalidOperationException("user validation failed"));
		var service = this.CreateService();
		
		await Assert.ThrowsAsync<InvalidOperationException>(() => this.SetupAsync(service));
		
		Assert.Equal(["user type", "role", "membership"], this._deleted);
		Assert.Empty(this._memberships);
		Assert.False(await service.IsSetUpAsync(TestContext.Current.CancellationToken));
		Assert.Single(this._setupTokens);
		Assert.False(this._isSetupCollectionDropped);
	}
	
	[Fact]
	public async Task SetupAsync_WhenTheApplicationFails_AlsoDeletesTheUser()
	{
		this.InsertSetupToken();
		this._applicationService
			.CreateWithSecretAsync(Arg.Any<Utilizer>(), Arg.Any<string>(), Arg.Any<Application>(), Arg.Any<CancellationToken>())
			.ThrowsAsync(new InvalidOperationException("application validation failed"));
		
		await Assert.ThrowsAsync<InvalidOperationException>(() => this.SetupAsync(this.CreateService(), application: new Application { Name = "Server", Role = "admin", MembershipId = string.Empty }));
		
		Assert.Equal(["user", "user type", "role", "membership"], this._deleted);
	}
	
	#endregion
}