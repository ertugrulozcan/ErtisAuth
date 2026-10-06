using Ertis.Core.Collections;
using Ertis.Schema.Dynamics;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.Infrastructure.Tests.Helpers;
using ErtisAuth.Integrations.OAuth;
using ErtisAuth.Integrations.OAuth.Abstractions;
using ErtisAuth.Integrations.OAuth.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// ProviderService.LoginAsync: how a verified provider identity is matched to a user. The identity itself
/// (user id, email, email verification) is set by the authenticators; see ErtisAuth.Integrations.OAuth.Tests.
/// </summary>
public class ProviderServiceLoginTests
{
	#region Constants
	
	private const string MembershipId = "5f8a1b2c3d4e5f6a7b8c9d00";
	private const string ProviderUserId = "provider-user-id";
	private const string Email = "john.doe@example.com";
	
	#endregion
	
	#region Fields
	
	private readonly IMembershipService _membershipService = Substitute.For<IMembershipService>();
	
	private readonly IUserService _userService = Substitute.For<IUserService>();
	
	private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
	
	private readonly IProviderRepository _repository = Substitute.For<IProviderRepository>();
	
	private readonly IProviderAuthenticator _authenticator = Substitute.For<IProviderAuthenticator>();
	
	private readonly IEventService _eventService = Substitute.For<IEventService>();
	
	private readonly List<Provider> _providers;
	
	private readonly List<User> _users = [];
	
	private User? _tokenOwner;
	
	#endregion
	
	#region Constructors
	
	public ProviderServiceLoginTests()
	{
		// ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
		this._providers = InMemoryRepository.Setup(this._repository, x => x.Id ??= Guid.NewGuid().ToString("N")[..24]);
		this._membershipService.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(callInfo => CreateMembership(callInfo.ArgAt<string>(0)));
		
		this._userService
			.QueryAsync(Arg.Any<string>(), MembershipId, Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<bool?>(), Arg.Any<string?>(), Arg.Any<SortDirection?>(), Arg.Any<IDictionary<string, bool>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => this.QueryUsers(callInfo.ArgAt<string>(0)));
		
		this._userService
			.CreateAsync(Arg.Any<DynamicObject>(), MembershipId, Arg.Any<Utilizer>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => this.StoreUser(callInfo.ArgAt<DynamicObject>(0), Guid.NewGuid().ToString("N")[..24]));
		
		this._userService
			.UpdateAsync(Arg.Any<DynamicObject>(), Arg.Any<string>(), MembershipId, Arg.Any<Utilizer>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => this.StoreUser(callInfo.ArgAt<DynamicObject>(0), callInfo.ArgAt<string>(1)));
		
		this._tokenService
			.GenerateTokenAsync(Arg.Any<User>(), MembershipId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				this._tokenOwner = callInfo.ArgAt<User>(0);
				return new BearerToken("access-token", TimeSpan.FromHours(1));
			});
	}
	
	#endregion
	
	#region Helpers
	
	private static Membership CreateMembership(string id)
	{
		var membership = TestServiceFactory.CreateMembership();
		membership.Id = id;
		return membership;
	}
	
	private ProviderService CreateService()
	{
		var authenticatorFactory = Substitute.For<IAuthenticatorFactory>();
		authenticatorFactory.GetAuthenticator(Arg.Any<Provider>()).Returns(this._authenticator);
		
		return new ProviderService(
			this._membershipService,
			this._userService,
			Substitute.For<IUserTypeService>(),
			this._tokenService,
			this._eventService,
			authenticatorFactory,
			new MemoryCache(new MemoryCacheOptions()),
			this._repository,
			NullLogger<ProviderService>.Instance);
	}
	
	private void AddProvider(bool isActive = true, bool trustEmail = false)
	{
		this._providers.Add(new AppleProvider
		{
			Id = "5f8a1b2c3d4e5f6a7b8c9d10",
			MembershipId = MembershipId,
			IsActive = isActive,
			TrustEmail = trustEmail,
			AppClientId = "app-id",
			DefaultRole = "user",
			DefaultUserType = "base-user"
		});
	}
	
	// ReSharper disable once UnusedMethodReturnValue.Local
	private User AddUser(string id, string email, bool isActive = true, ProviderAccountInfo[]? connectedAccounts = null)
	{
		var user = new User
		{
			Id = id,
			Username = email,
			EmailAddress = email,
			Role = "user",
			UserType = "base-user",
			IsActive = isActive,
			MembershipId = MembershipId,
			ConnectedAccounts = connectedAccounts
		};
		
		this._users.Add(user);
		return user;
	}
	
	/// <summary>
	/// Authenticator outcome: verified or not, with the identity the authenticator set from provider data.
	/// </summary>
	private void AuthenticatorReturns(bool isVerified)
	{
		this._authenticator.VerifyTokenAsync(Arg.Any<IProviderLoginRequest>(), Arg.Any<Provider>(), Arg.Any<CancellationToken>()).Returns(isVerified);
	}
	
	private static TestLoginRequest CreateRequest(string? userId = ProviderUserId, string? email = Email, bool isEmailVerified = false)
	{
		return new TestLoginRequest
		{
			UserId = userId,
			EmailAddress = email,
			IsEmailVerified = isEmailVerified
		};
	}
	
	private IPaginationCollection<DynamicObject> QueryUsers(string query)
	{
		// FindUserAsync queries by connected account (provider + provider user id) or by email address
		var matches = query.Contains("connected_accounts")
			? this._users.Where(x => x.ConnectedAccounts?.Any(a => a.Provider == ProviderType.Facebook.ToString() && query.Contains($"\"{a.UserId}\"")) == true)
			: this._users.Where(x => query.Contains($"\"{x.EmailAddress}\""));
		
		var items = matches.Select(x => new DynamicObject(x)).ToArray();
		return new PaginationCollection<DynamicObject> { Count = items.Length, Items = items };
	}
	
	private DynamicObject StoreUser(DynamicObject model, string id)
	{
		var user = model.Deserialize<User>()!;
		user.Id = id;
		this._users.RemoveAll(x => x.Id == id);
		this._users.Add(user);
		return new DynamicObject(user);
	}
	
	private Task<BearerToken> LoginAsync(ProviderService service, TestLoginRequest request)
	{
		return service.LoginAsync(request, MembershipId, cancellationToken: TestContext.Current.CancellationToken);
	}
	
	#endregion
	
	#region Matching
	
	[Fact]
	public async Task LoginAsync_WithLinkedAccount_LogsInTheLinkedUser()
	{
		this.AddProvider();
		this.AddUser("5f8a1b2c3d4e5f6a7b8c9d01", "linked@example.com", connectedAccounts: [new ProviderAccountInfo { Provider = "Facebook", UserId = ProviderUserId }]);
		this.AuthenticatorReturns(true);
		
		await this.LoginAsync(this.CreateService(), CreateRequest(email: "other@example.com"));
		
		Assert.Equal("5f8a1b2c3d4e5f6a7b8c9d01", this._tokenOwner?.Id);
	}
	
	[Fact]
	public async Task LoginAsync_WithVerifiedEmailOfExistingUser_LinksTheAccount()
	{
		this.AddProvider(trustEmail: false);
		this.AddUser("5f8a1b2c3d4e5f6a7b8c9d01", Email);
		this.AuthenticatorReturns(true);
		
		await this.LoginAsync(this.CreateService(), CreateRequest(isEmailVerified: true));
		
		Assert.Equal("5f8a1b2c3d4e5f6a7b8c9d01", this._tokenOwner?.Id);
		var linked = Assert.Single(this._users.Single(x => x.Id == "5f8a1b2c3d4e5f6a7b8c9d01").ConnectedAccounts!);
		Assert.Equal(ProviderUserId, linked.UserId);
	}
	
	[Fact]
	public async Task LoginAsync_WithUnverifiedEmail_WhenProviderTrustsEmail_LinksTheAccount()
	{
		this.AddProvider(trustEmail: true);
		this.AddUser("5f8a1b2c3d4e5f6a7b8c9d01", Email);
		this.AuthenticatorReturns(true);
		
		await this.LoginAsync(this.CreateService(), CreateRequest(isEmailVerified: false));
		
		Assert.Equal("5f8a1b2c3d4e5f6a7b8c9d01", this._tokenOwner?.Id);
	}
	
	[Fact]
	public async Task LoginAsync_WithUnverifiedEmail_WhenProviderDoesNotTrustEmail_IsRejected()
	{
		this.AddProvider(trustEmail: false);
		this.AddUser("5f8a1b2c3d4e5f6a7b8c9d01", Email);
		this.AuthenticatorReturns(true);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.LoginAsync(this.CreateService(), CreateRequest(isEmailVerified: false)));
		
		Assert.Equal("ProviderEmailNotTrusted", exception.ErrorCode);
		Assert.Null(this._tokenOwner);
		Assert.Null(this._users.Single().ConnectedAccounts);
	}
	
	[Fact]
	public async Task LoginAsync_WithUnknownUser_CreatesALinkedUser()
	{
		this.AddProvider();
		this.AuthenticatorReturns(true);
		
		await this.LoginAsync(this.CreateService(), CreateRequest());
		
		var created = Assert.Single(this._users);
		Assert.Equal(created.Id, this._tokenOwner?.Id);
		Assert.Equal(Email, created.EmailAddress);
		Assert.Equal(ProviderUserId, Assert.Single(created.ConnectedAccounts!).UserId);
	}
	
	[Fact]
	public async Task LoginAsync_WithoutEmail_DoesNotMatchAUserByEmail()
	{
		this.AddProvider(trustEmail: true);
		this.AddUser("5f8a1b2c3d4e5f6a7b8c9d01", Email);
		this.AuthenticatorReturns(true);
		
		await this.LoginAsync(this.CreateService(), CreateRequest(email: null));
		
		Assert.NotEqual("5f8a1b2c3d4e5f6a7b8c9d01", this._tokenOwner?.Id);
		await this._userService.DidNotReceive().QueryAsync(Arg.Is<string>(x => x.Contains("email_address")), MembershipId, Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<bool?>(), Arg.Any<string?>(), Arg.Any<SortDirection?>(), Arg.Any<IDictionary<string, bool>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
	}
	
	[Fact]
	public async Task LoginAsync_WithInactiveUser_ThrowsUserInactive()
	{
		this.AddProvider();
		this.AddUser("5f8a1b2c3d4e5f6a7b8c9d01", Email, isActive: false, connectedAccounts: [new ProviderAccountInfo { Provider = "Facebook", UserId = ProviderUserId }]);
		this.AuthenticatorReturns(true);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.LoginAsync(this.CreateService(), CreateRequest()));
		
		Assert.Equal("UserInactive", exception.ErrorCode);
	}
	
	#endregion
	
	#region Verification
	
	[Fact]
	public async Task LoginAsync_WhenProviderRejectsTheToken_ThrowsUnauthorized()
	{
		this.AddProvider();
		this.AuthenticatorReturns(false);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.LoginAsync(this.CreateService(), CreateRequest()));
		
		Assert.Equal(System.Net.HttpStatusCode.Unauthorized, exception.StatusCode);
		Assert.Null(this._tokenOwner);
	}
	
	[Fact]
	public async Task LoginAsync_WhenAuthenticatorSetNoProviderUserId_ThrowsUnauthorized()
	{
		this.AddProvider();
		this.AuthenticatorReturns(true);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.LoginAsync(this.CreateService(), CreateRequest(userId: null)));
		
		Assert.Equal(System.Net.HttpStatusCode.Unauthorized, exception.StatusCode);
	}
	
	[Fact]
	public async Task LoginAsync_WithInactiveProvider_ThrowsProviderIsDisable()
	{
		this.AddProvider(isActive: false);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.LoginAsync(this.CreateService(), CreateRequest()));
		
		Assert.Equal("ProviderIsDisable", exception.ErrorCode);
	}
	
	[Fact]
	public async Task LoginAsync_WithoutProvider_ThrowsProviderNotConfigured()
	{
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.LoginAsync(this.CreateService(), CreateRequest()));
		
		Assert.Equal("ProviderNotConfigured", exception.ErrorCode);
	}
	
	#endregion
	
	#region Events
	
	[Fact]
	public async Task UpdateAsync_EventDoesNotContainThePrivateKey()
	{
		// Events are readable (events.read) and forwarded to webhooks: not the Apple signing key
		this.AddProvider();
		(this._providers.Single() as AppleProvider)?.PrivateKey = "-----BEGIN PRIVATE KEY-----current";
		object? document = null;
		object? prior = null;
		this._eventService
			.When(x => x.FireEventAsync(ErtisAuthEventType.ProviderUpdated, Arg.Any<Utilizer>(), Arg.Any<string?>(), Arg.Any<object?>(), Arg.Any<object?>(), Arg.Any<CancellationToken>()))
			.Do(x =>
			{
				document = x.ArgAt<object?>(3);
				prior = x.ArgAt<object?>(4);
			});
		
		var update = new AppleProvider
		{
			Id = "5f8a1b2c3d4e5f6a7b8c9d10",
			MembershipId = MembershipId,
			AppClientId = "new-app-id",
			PrivateKey = "-----BEGIN PRIVATE KEY-----updated"
		};
		
		await this.CreateService().UpdateAsync(update, MembershipId, Utilizer.GetSystemUtilizer(MembershipId), TestContext.Current.CancellationToken);
		
		var json = System.Text.Json.JsonSerializer.Serialize(new { document, prior });
		Assert.DoesNotContain("BEGIN PRIVATE KEY", json);
		Assert.Contains("new-app-id", json);
	}
	
	#endregion
	
	#region Default Providers
	
	[Fact]
	public async Task GetProvidersAsync_CreatesTheDefaultProvidersForEveryMembership()
	{
		var service = this.CreateService();
		
		await service.GetProvidersAsync("5f8a1b2c3d4e5f6a7b8c9d0a", TestContext.Current.CancellationToken);
		await service.GetProvidersAsync("5f8a1b2c3d4e5f6a7b8c9d0b", TestContext.Current.CancellationToken);
		
		var expected = Enum.GetValues<ProviderType>().Count(x => x != ProviderType.ErtisAuth);
		Assert.Equal(expected, this._providers.Count(x => x.MembershipId == "5f8a1b2c3d4e5f6a7b8c9d0a"));
		Assert.Equal(expected, this._providers.Count(x => x.MembershipId == "5f8a1b2c3d4e5f6a7b8c9d0b"));
		Assert.All(this._providers, x => Assert.False(x.IsActive || x.TrustEmail));
	}
	
	#endregion
	
	#region Update
	
	[Theory]
	[InlineData(false, true)]
	[InlineData(true, false)]
	public async Task UpdateAsync_ChangesTheActivation(bool isActive, bool newIsActive)
	{
		// Regression: Overwrite copied the stored IsActive over the incoming one, so a provider could never be (de)activated
		this.AddProvider(isActive: isActive);
		var update = new FacebookProvider
		{
			Id = "5f8a1b2c3d4e5f6a7b8c9d10",
			MembershipId = MembershipId,
			IsActive = newIsActive,
			AppClientId = "app-id",
			DefaultRole = "user",
			DefaultUserType = "base-user"
		};
		
		var updated = await this.CreateService().UpdateAsync(update, MembershipId, Utilizer.GetSystemUtilizer(MembershipId), TestContext.Current.CancellationToken);
		
		Assert.Equal(newIsActive, updated.IsActive);
		Assert.Equal(newIsActive, Assert.Single(this._providers).IsActive);
	}
	
	#endregion
	
	#region Test Request
	
	private sealed class TestLoginRequest : IProviderLoginRequest
	{
		public ProviderType Provider => ProviderType.Facebook;
		
		public string? UserId { get; init; }
		
		public string? EmailAddress { get; init; }
		
		public bool IsEmailVerified { get; init; }
		
		public string? AvatarUrl => null;
		
		public string AccessToken => "provider-access-token";
		
		public bool IsValid() => true;
		
		public object ToUser(string membershipId, string? role, string? userType)
		{
			return new User
			{
				MembershipId = membershipId,
				Username = this.EmailAddress ?? string.Empty,
				EmailAddress = this.EmailAddress,
				Role = role ?? string.Empty,
				UserType = userType,
				SourceProvider = ProviderType.Facebook.ToString()
			};
		}
	}
	
	#endregion
}