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

// ReSharper disable MemberCanBePrivate.Global
namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// Shared setup of the reset password and activation token tests: users in an in-memory store queried through
/// the dynamic user repository, and the real JwtService.
/// </summary>
public abstract class BaseActionTokenTests
{
	#region Constants
	
	protected const string VictimId = "5f8a1b2c3d4e5f6a7b8c9d01";
	
	protected const string AttackerId = "5f8a1b2c3d4e5f6a7b8c9d02";
	
	protected const string InactiveUserId = "5f8a1b2c3d4e5f6a7b8c9d03";
	
	protected const string AttackerSecretKey = "attacker-secret-key-attacker-secret-key-attacker";
	
	protected const string NewPassword = "N3wP@ssw0rd!";
	
	#endregion
	
	#region Fields
	
	protected readonly IMembershipService _membershipService = Substitute.For<IMembershipService>();
	
	protected readonly IUserTypeService _userTypeService = Substitute.For<IUserTypeService>();
	
	protected readonly IRoleService _roleService = Substitute.For<IRoleService>();
	
	protected readonly IAccessControlService _accessControlService = Substitute.For<IAccessControlService>();
	
	protected readonly IUserRepository _repository = Substitute.For<IUserRepository>();
	
	protected readonly JwtService _jwtService = new();
	
	protected readonly Dictionary<string, ExpandoObject> _users = new();
	
	protected Membership _membership = null!;
	
	protected BsonDocument? _persistedDocument;
	
	#endregion
	
	#region Constructors
	
	protected BaseActionTokenTests()
	{
		this.SetupMembership();
		
		this._userTypeService.GetByNameOrSlugAsync("user", Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(new UserType
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
		this._accessControlService.HasGrantedPermission(null, null!, default!).ReturnsForAnyArgs(true);
		
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
			.UpdateAsync(null!)
			.ReturnsForAnyArgs(x =>
			{
				this._persistedDocument = x.ArgAt<BsonDocument>(0);
				return x.ArgAt<object>(0);
			});
	}
	
	#endregion
	
	#region Helpers
	
	protected void SetupMembership(string? defaultEncoding = null)
	{
		this._membership = TestServiceFactory.CreateMembership("SHA2-256", defaultEncoding);
		this._membershipService.GetAsync(this._membership.Id, Arg.Any<CancellationToken>()).Returns(this._membership);
	}
	
	protected void AddUser(string id, string username, bool isActive, string passwordHash, DateTime? modifiedAt = null)
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
	
	protected PasswordResetService CreatePasswordResetService()
	{
		return new PasswordResetService(
			this.CreateUserService(),
			this._membershipService,
			this._jwtService,
			Substitute.For<IMailHookService>(),
			Substitute.For<IEventService>());
	}
	
	protected UserService CreateUserService()
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
	
	protected Utilizer PublicPageApplication()
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
	
	protected User UserModel(string id, string username)
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
	/// A token with the claims of a action token, signed with the given key (the membership key unless stated otherwise).
	/// </summary>
	protected string CreateToken(
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
		claims.AddClaim(ActionTokens.TokenTypeClaim, tokenType);
		if (tokenType == ActionTokens.ResetPasswordTokenType)
		{
			claims.AddClaim(ActionTokens.PasswordFingerprintClaim, Fingerprint(passwordHash));
		}
		
		return this._jwtService.GenerateToken(claims, generationTime: generationTime, encoding: this._membership.GetEncoding());
	}
	
	protected static string Fingerprint(string? passwordHash)
	{
		return Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(passwordHash ?? string.Empty)))[..32];
	}
	
	protected string Link(string token, string? membershipId = null)
	{
		return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{membershipId ?? this._membership.Id}:{token}"));
	}
	
	protected async Task AssertRejectedAsync(Func<Task> action, string errorCode = "InvalidToken")
	{
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(action);
		Assert.Equal(errorCode, exception.ErrorCode);
		await this._repository.DidNotReceiveWithAnyArgs().UpdateAsync(null!);
	}
	
	#endregion
}
