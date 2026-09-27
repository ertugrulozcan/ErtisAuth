using System.Dynamic;
using System.Text;
using Ertis.Core.Collections;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Constants;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using NSubstitute;

namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// Reset password and activation tokens must be signed with the membership key, of the expected type, bound to their user,
/// single use, and never usable as bearer tokens. Uses the real JwtService.
/// </summary>
public class UserServicePurposeTokenTests
{
	#region Constants
	
	private const string VictimId = "5f8a1b2c3d4e5f6a7b8c9d01";
	
	private const string AttackerId = "5f8a1b2c3d4e5f6a7b8c9d02";
	
	private const string InactiveUserId = "5f8a1b2c3d4e5f6a7b8c9d03";
	
	private const string AttackerSecretKey = "attacker-secret-key-attacker-secret-key-attacker";
	
	private const string NewPassword = "N3wP@ssw0rd!";
	
	#endregion
	
	#region Fields
	
	private readonly IMembershipService _membershipService = Substitute.For<IMembershipService>();
	
	private readonly IUserTypeService _userTypeService = Substitute.For<IUserTypeService>();
	
	private readonly IRoleService _roleService = Substitute.For<IRoleService>();
	
	private readonly IAccessControlService _accessControlService = Substitute.For<IAccessControlService>();
	
	private readonly IUserRepository _repository = Substitute.For<IUserRepository>();
	
	private readonly JwtService _jwtService = new();
	
	private readonly Dictionary<string, ExpandoObject> _users = new();
	
	private Membership _membership = null!;
	
	private BsonDocument? _persistedDocument;
	
	#endregion
	
	#region Constructors
	
	public UserServicePurposeTokenTests()
	{
		this.SetupMembership();
		
		this._userTypeService.GetByNameOrSlugAsync(Arg.Any<string>(), "user", Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(new UserType
		{
			Id = "user-type-id",
			Name = "user",
			MembershipId = this._membership.Id,
			AllowAdditionalProperties = true
		});
		
		this._roleService.GetBySlugAsync("user", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new Role
		{
			Id = "user-role-id",
			Name = "user",
			MembershipId = this._membership.Id
		});
		
		// The utilizer calling the reset and activation endpoints (e.g. the application behind the public pages) holds users.update
		this._accessControlService.HasGrantedPermission(default, default!, default!).ReturnsForAnyArgs(true);
		
		this.AddUser(VictimId, "victim", isActive: true, passwordHash: "victim-password-hash");
		this.AddUser(AttackerId, "attacker", isActive: true, passwordHash: "attacker-password-hash");
		this.AddUser(InactiveUserId, "newcomer", isActive: false, passwordHash: "newcomer-password-hash");
		
		// Reads go through query strings; return the users the query refers to (by id, username or email address)
		this._repository
			.FindAsync(default(string)!, null, null, null, default(Sorting))
			.ReturnsForAnyArgs(x => this.FindUsers(x.ArgAt<string>(0)));
		
		this._repository
			.FindAsync(default(string)!, null, null, null, null, null, null, null)
			.ReturnsForAnyArgs(x => this.FindUsers(x.ArgAt<string>(0)));
		
		this._repository
			.UpdateAsync(default!, default!, default, default)
			.ReturnsForAnyArgs(x =>
			{
				this._persistedDocument = x.ArgAt<BsonDocument>(0);
				return x.ArgAt<object>(0);
			});
	}
	
	#endregion
	
	#region Helpers
	
	private void SetupMembership(string? defaultEncoding = null)
	{
		this._membership = TestServiceFactory.CreateMembership("SHA2-256", defaultEncoding);
		this._membershipService.GetAsync(this._membership.Id, Arg.Any<CancellationToken>()).Returns(this._membership);
	}
	
	private void AddUser(string id, string username, bool isActive, string passwordHash, DateTime? modifiedAt = null)
	{
		dynamic user = new ExpandoObject();
		user._id = id;
		user.username = username;
		user.email_address = $"{username}@example.com";
		user.firstname = username;
		user.lastname = "Doe";
		user.role = "user";
		user.user_type = "user";
		user.is_active = isActive;
		user.membership_id = this._membership.Id;
		user.password_hash = passwordHash;
		if (modifiedAt != null)
		{
			user.sys = new Dictionary<string, object?>
			{
				{ "created_at", modifiedAt.Value.AddDays(-1) },
				{ "created_by", "system" },
				{ "modified_at", modifiedAt.Value },
				{ "modified_by", "admin" }
			};
		}
		
		this._users[id] = user;
	}
	
	private IPaginationCollection<object> FindUsers(string query)
	{
		var matches = this._users
			.Where(x =>
			{
				var user = (IDictionary<string, object?>)x.Value;
				return query.Contains(x.Key) || query.Contains($"\"{user["username"]}\"") || query.Contains($"\"{user["email_address"]}\"");
			})
			.Select(x => (object)CloneUser(x.Value))
			.ToArray();
		
		return new PaginationCollection<object> { Count = matches.Length, Items = matches };
	}
	
	/// <summary>
	/// Returns a copy, as a database would, so that the service can not change the stored user in place.
	/// </summary>
	private static ExpandoObject CloneUser(ExpandoObject user)
	{
		var clone = new ExpandoObject();
		var cloneDictionary = (IDictionary<string, object?>)clone;
		foreach (var (key, value) in user)
		{
			cloneDictionary[key] = value is Dictionary<string, object?> dictionary ? new Dictionary<string, object?>(dictionary) : value;
		}
		
		return clone;
	}
	
	private UserService CreateUserService()
	{
		return new UserService(
			this._userTypeService,
			this._membershipService,
			this._roleService,
			this._accessControlService,
			Substitute.For<IEventService>(),
			this._jwtService,
			Substitute.For<IMailHookService>(),
			this._repository,
			NullLogger<UserService>.Instance);
	}
	
	private Utilizer PublicPageApplication()
	{
		return new Utilizer
		{
			Id = "application-id",
			Username = "public-page",
			MembershipId = this._membership.Id,
			Role = "server",
			Type = Utilizer.UtilizerType.Application
		};
	}
	
	private User UserModel(string id, string username)
	{
		return new User
		{
			Id = id,
			Username = username,
			EmailAddress = $"{username}@example.com",
			Role = "user",
			IsActive = true,
			MembershipId = this._membership.Id
		};
	}
	
	/// <summary>
	/// A token with the claims of a purpose token, signed with the given key (the membership key unless stated otherwise).
	/// </summary>
	private string CreateToken(
		string userId,
		string username,
		string tokenType,
		string? passwordHash = null,
		string? signingSecretKey = null,
		DateTime? generationTime = null)
	{
		var membership = this._membership;
		if (signingSecretKey != null)
		{
			membership = TestServiceFactory.CreateMembership("SHA2-256");
			membership.Id = this._membership.Id;
			membership.Name = this._membership.Name;
			membership.SecretKey = signingSecretKey;
		}
		
		var claims = new TokenClaims(Guid.NewGuid().ToString(), this.UserModel(userId, username), membership, TimeSpan.FromHours(1));
		claims.AddClaim(PurposeTokens.TokenTypeClaim, tokenType);
		if (tokenType == PurposeTokens.ResetPasswordTokenType)
		{
			claims.AddClaim(PurposeTokens.PasswordFingerprintClaim, Fingerprint(passwordHash));
		}
		
		return this._jwtService.GenerateToken(claims, generationTime: generationTime, encoding: this._membership.GetEncoding());
	}
	
	private static string Fingerprint(string? passwordHash)
	{
		return Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(passwordHash ?? string.Empty)))[..32];
	}
	
	private string Link(string token, string? membershipId = null)
	{
		return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{membershipId ?? this._membership.Id}:{token}"));
	}
	
	private async Task<string> GenerateResetLinkAsync(string userId, string username)
	{
		var token = await this.CreateUserService().GenerateResetPasswordTokenAsync(this.UserModel(userId, username), this._membership, asBase64: true, cancellationToken: TestContext.Current.CancellationToken);
		return token.Token;
	}
	
	private async Task SetPasswordAsync(string resetLink, string usernameOrEmailAddress)
	{
		await this.CreateUserService().SetPasswordAsync(this.PublicPageApplication(), this._membership.Id, resetLink, usernameOrEmailAddress, NewPassword, TestContext.Current.CancellationToken);
	}
	
	private async Task AssertRejectedAsync(Func<Task> action, string errorCode = "InvalidToken")
	{
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(action);
		Assert.Equal(errorCode, exception.ErrorCode);
		await this._repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default!, default, default);
	}
	
	#endregion
	
	#region Reset Password
	
	[Fact]
	public async Task SetPasswordAsync_WithIssuedResetToken_ChangesPassword()
	{
		var resetLink = await this.GenerateResetLinkAsync(VictimId, "victim");
		
		await this.SetPasswordAsync(resetLink, "victim");
		
		Assert.NotNull(this._persistedDocument);
		Assert.NotEqual("victim-password-hash", this._persistedDocument["password_hash"].AsString);
	}
	
	[Fact]
	public async Task SetPasswordAsync_WithTokenSignedByAnotherKey_IsRejected()
	{
		var forgedToken = this.CreateToken(VictimId, "victim", PurposeTokens.ResetPasswordTokenType, "victim-password-hash", signingSecretKey: AttackerSecretKey);
		
		await this.AssertRejectedAsync(() => this.SetPasswordAsync(this.Link(forgedToken), "victim"));
	}
	
	[Fact]
	public async Task SetPasswordAsync_WithOwnTokenForAnotherUser_IsRejected()
	{
		// A genuine reset link of the attacker's own account, used to set the victim's password
		var attackerResetLink = await this.GenerateResetLinkAsync(AttackerId, "attacker");
		
		await this.AssertRejectedAsync(() => this.SetPasswordAsync(attackerResetLink, "victim"));
	}
	
	[Fact]
	public async Task SetPasswordAsync_WithActivationToken_IsRejected()
	{
		var activationToken = this.CreateToken(VictimId, "victim", PurposeTokens.ActivationTokenType);
		
		await this.AssertRejectedAsync(() => this.SetPasswordAsync(this.Link(activationToken), "victim"));
	}
	
	[Fact]
	public async Task SetPasswordAsync_WithTokenWithoutType_IsRejected()
	{
		// e.g. an access token of the victim
		var claims = new TokenClaims(Guid.NewGuid().ToString(), this.UserModel(VictimId, "victim"), this._membership, TimeSpan.FromHours(1));
		var accessToken = this._jwtService.GenerateToken(claims);
		
		await this.AssertRejectedAsync(() => this.SetPasswordAsync(this.Link(accessToken), "victim"));
	}
	
	[Fact]
	public async Task SetPasswordAsync_WithTokenOfAnotherMembership_IsRejected()
	{
		var resetLink = await this.GenerateResetLinkAsync(VictimId, "victim");
		var token = Encoding.UTF8.GetString(Convert.FromBase64String(resetLink)).Split(':', 2)[1];
		
		await this.AssertRejectedAsync(() => this.SetPasswordAsync(this.Link(token, "another-membership-id"), "victim"));
	}
	
	[Fact]
	public async Task SetPasswordAsync_AfterPasswordWasChanged_IsRejected()
	{
		var resetLink = await this.GenerateResetLinkAsync(VictimId, "victim");
		
		// The first use changes the password
		((IDictionary<string, object?>)this._users[VictimId])["password_hash"] = "changed-password-hash";
		
		await this.AssertRejectedAsync(() => this.SetPasswordAsync(resetLink, "victim"));
	}
	
	[Fact]
	public async Task SetPasswordAsync_WithExpiredToken_IsRejectedAsExpired()
	{
		var expiredToken = this.CreateToken(VictimId, "victim", PurposeTokens.ResetPasswordTokenType, "victim-password-hash", generationTime: DateTime.UtcNow.AddHours(-2));
		
		await this.AssertRejectedAsync(() => this.SetPasswordAsync(this.Link(expiredToken), "victim"), "TokenWasExpired");
	}
	
	[Fact]
	public async Task SetPasswordAsync_WithUtf16Membership_ChangesPassword()
	{
		this.SetupMembership("utf-16");
		var resetLink = await this.GenerateResetLinkAsync(VictimId, "victim");
		
		await this.SetPasswordAsync(resetLink, "victim");
		
		Assert.NotNull(this._persistedDocument);
	}
	
	[Fact]
	public async Task GenerateResetPasswordTokenAsync_ForInactiveUser_IsRejected()
	{
		var inactiveUser = this.UserModel(InactiveUserId, "newcomer");
		inactiveUser.IsActive = false;
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.CreateUserService().GenerateResetPasswordTokenAsync(inactiveUser, this._membership, cancellationToken: TestContext.Current.CancellationToken));
		
		Assert.Equal("UserInactive", exception.ErrorCode);
	}
	
	[Fact]
	public async Task SetPasswordAsync_ForUserFrozenAfterTokenWasIssued_IsRejected()
	{
		var resetLink = await this.GenerateResetLinkAsync(VictimId, "victim");
		((IDictionary<string, object?>)this._users[VictimId])["is_active"] = false;
		
		await this.AssertRejectedAsync(() => this.SetPasswordAsync(resetLink, "victim"), "UserInactive");
	}
	
	[Fact]
	public async Task VerifyResetTokenAsync_WithMalformedLink_IsRejected()
	{
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.CreateUserService().VerifyResetTokenAsync(this._membership.Id, "not-a-reset-link", TestContext.Current.CancellationToken));
		
		Assert.Equal("InvalidToken", exception.ErrorCode);
	}
	
	#endregion
	
	#region Activation
	
	private async Task<User?> ActivateAsync(string token)
	{
		return await this.CreateUserService().ActivateUserAsync(this.PublicPageApplication(), this._membership.Id, this.Link(token), TestContext.Current.CancellationToken);
	}
	
	[Fact]
	public async Task ActivateUserAsync_WithIssuedActivationToken_ActivatesUser()
	{
		var activationToken = this.CreateToken(InactiveUserId, "newcomer", PurposeTokens.ActivationTokenType);
		
		await this.ActivateAsync(activationToken);
		
		Assert.NotNull(this._persistedDocument);
		Assert.True(this._persistedDocument["is_active"].AsBoolean);
	}
	
	[Fact]
	public async Task ActivateUserAsync_WithTokenSignedByAnotherKey_IsRejected()
	{
		var forgedToken = this.CreateToken(InactiveUserId, "newcomer", PurposeTokens.ActivationTokenType, signingSecretKey: AttackerSecretKey);
		
		await this.AssertRejectedAsync(() => this.ActivateAsync(forgedToken));
	}
	
	[Fact]
	public async Task ActivateUserAsync_WithResetToken_IsRejected()
	{
		var resetToken = this.CreateToken(InactiveUserId, "newcomer", PurposeTokens.ResetPasswordTokenType, "newcomer-password-hash");
		
		await this.AssertRejectedAsync(() => this.ActivateAsync(resetToken));
	}
	
	[Fact]
	public async Task ActivateUserAsync_WithTokenIssuedBeforeUserWasFrozen_IsRejected()
	{
		// The activation link was sent, then an administrator froze the user
		var issuedAt = DateTime.UtcNow.AddMinutes(-30);
		this.AddUser(InactiveUserId, "newcomer", isActive: false, passwordHash: "newcomer-password-hash", modifiedAt: DateTime.UtcNow.AddMinutes(-10));
		var activationToken = this.CreateToken(InactiveUserId, "newcomer", PurposeTokens.ActivationTokenType, generationTime: issuedAt);
		
		await this.AssertRejectedAsync(() => this.ActivateAsync(activationToken));
	}
	
	[Fact]
	public async Task ActivateUserAsync_WithTokenIssuedAfterLastChange_ActivatesUser()
	{
		this.AddUser(InactiveUserId, "newcomer", isActive: false, passwordHash: "newcomer-password-hash", modifiedAt: DateTime.UtcNow.AddMinutes(-30));
		var activationToken = this.CreateToken(InactiveUserId, "newcomer", PurposeTokens.ActivationTokenType, generationTime: DateTime.UtcNow.AddMinutes(-10));
		
		await this.ActivateAsync(activationToken);
		
		Assert.NotNull(this._persistedDocument);
		Assert.True(this._persistedDocument["is_active"].AsBoolean);
	}
	
	#endregion
}
