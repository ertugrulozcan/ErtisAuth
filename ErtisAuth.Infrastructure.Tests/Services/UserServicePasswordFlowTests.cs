using System.Dynamic;
using Ertis.Core.Collections;
using Ertis.Core.Exceptions;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.Infrastructure.Tests.Helpers;
using MongoDB.Bson;
using NSubstitute;

namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// Password write (ChangePasswordAsync) and check (CheckPasswordAsync) flows of UserService.
/// </summary>
public class UserServicePasswordFlowTests
{
	#region Constants
	
	private const string UserId = "5f8a1b2c3d4e5f6a7b8c9d0e";
	
	private const string Password = "P@ssw0rd!";
	
	private const string Sha256OfPassword = "0e44ce7308af2b3de5232e4616403ce7d49ba2aec83f79c196409556422a4927";
	
	#endregion
	
	#region Fields
	
	private readonly IMembershipService _membershipService = Substitute.For<IMembershipService>();
	
	private readonly IUserRepository _repository = Substitute.For<IUserRepository>();
	
	private BsonDocument? _updatedDocument;
	
	#endregion
	
	#region Helpers
	
	private UserService CreateUserService()
	{
		return TestServiceFactory.CreateUserService(this._membershipService, this._repository);
	}
	
	private Membership SetupMembership(string hashAlgorithm)
	{
		var membership = TestServiceFactory.CreateMembership(hashAlgorithm);
		this._membershipService.GetAsync(membership.Id, Arg.Any<CancellationToken>()).Returns(membership);
		return membership;
	}
	
	private void SetupStoredUser(Membership membership, string? passwordHash)
	{
		var user = CreateUserDocument(membership, passwordHash);
		var collection = new PaginationCollection<object>
		{
			Count = 1,
			Items = new object[] { user }
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
				this._updatedDocument = x.ArgAt<BsonDocument>(0);
				return x.ArgAt<object>(0);
			});
			
		this._repository
			.FindOneAsync(UserId, Arg.Any<CancellationToken>())
			.Returns(_ => CreateUserDocument(membership, this._updatedDocument?["password_hash"].AsString));
	}
	
	private static ExpandoObject CreateUserDocument(Membership membership, string? passwordHash)
	{
		dynamic user = new ExpandoObject();
		user._id = UserId;
		user.username = "john.doe";
		user.email_address = "john.doe@example.com";
		user.firstname = "John";
		user.lastname = "Doe";
		user.role = "user";
		user.user_type = "user";
		user.is_active = true;
		user.membership_id = membership.Id;
		if (passwordHash != null)
		{
			user.password_hash = passwordHash;
		}
		
		return user;
	}
	
	private static Utilizer CreateUtilizer(Membership membership)
	{
		return new Utilizer
		{
			Id = UserId,
			Username = "john.doe",
			MembershipId = membership.Id
		};
	}
	
	#endregion
	
	#region ChangePasswordAsync
	
	[Theory]
	[InlineData("SHA2-256")]
	[InlineData("MD5")]
	public async Task ChangePasswordAsync_WithLegacyAlgorithm_StoresKnownHexDigest(string hashAlgorithm)
	{
		var membership = this.SetupMembership(hashAlgorithm);
		this.SetupStoredUser(membership, "old-hash");
		var userService = this.CreateUserService();
		
		await userService.ChangePasswordAsync(CreateUtilizer(membership), membership.Id, UserId, Password, TestContext.Current.CancellationToken);
		
		Assert.NotNull(this._updatedDocument);
		var expected = hashAlgorithm == "MD5" ? "8a24367a1f46c141048752f2d5bbd14b" : Sha256OfPassword;
		Assert.Equal(expected, this._updatedDocument["password_hash"].AsString);
	}
	
	[Theory]
	[InlineData("ARGON2ID", "$argon2id$v=19$m=19456,t=2,p=1$")]
	[InlineData("PBKDF2-SHA256", "$pbkdf2-sha256$i=600000$")]
	[InlineData("PBKDF2-SHA512", "$pbkdf2-sha512$i=210000$")]
	public async Task ChangePasswordAsync_WithModernAlgorithm_StoresVerifiablePhcHash(string hashAlgorithm, string expectedPrefix)
	{
		var membership = this.SetupMembership(hashAlgorithm);
		this.SetupStoredUser(membership, Sha256OfPassword);
		var userService = this.CreateUserService();
		
		await userService.ChangePasswordAsync(CreateUtilizer(membership), membership.Id, UserId, Password, TestContext.Current.CancellationToken);
		
		Assert.NotNull(this._updatedDocument);
		var storedHash = this._updatedDocument["password_hash"].AsString;
		Assert.StartsWith(expectedPrefix, storedHash);
		Assert.True(userService.VerifyPassword(membership, Password, storedHash));
		Assert.False(userService.VerifyPassword(membership, "wrong-password", storedHash));
	}
	
	[Fact]
	public async Task ChangePasswordAsync_DoesNotPersistPlainPassword()
	{
		var membership = this.SetupMembership("ARGON2ID");
		this.SetupStoredUser(membership, Sha256OfPassword);
		var userService = this.CreateUserService();
		
		await userService.ChangePasswordAsync(CreateUtilizer(membership), membership.Id, UserId, Password, TestContext.Current.CancellationToken);
		
		Assert.NotNull(this._updatedDocument);
		Assert.False(this._updatedDocument.Contains("password"));
		Assert.DoesNotContain(Password, this._updatedDocument.ToJson());
	}
	
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public async Task ChangePasswordAsync_WithEmptyPassword_ThrowsValidationError(string? newPassword)
	{
		var membership = this.SetupMembership("ARGON2ID");
		var userService = this.CreateUserService();
		
		await Assert.ThrowsAsync<ValidationException>(() => userService.ChangePasswordAsync(CreateUtilizer(membership), membership.Id, UserId, newPassword!, TestContext.Current.CancellationToken));
		
		await this._repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default!, default, TestContext.Current.CancellationToken);
	}
	
	#endregion
	
	#region CheckPasswordAsync
	
	[Theory]
	[InlineData("SHA2-256", Sha256OfPassword)]
	[InlineData("ARGON2ID", "$argon2id$v=19$m=19456,t=2,p=1$RXJ0aXNBdXRoU2FsdCExNg$fbBQyhNR+kJrPcAQUBon1Sg6Erqte6DHJbnLH1xxVCc")]
	[InlineData("PBKDF2-SHA512", "$pbkdf2-sha512$i=210000$RXJ0aXNBdXRoU2FsdCExNg$EyVtj8iEUqCqsFGtUYNm5qVfvK4KkX8D02ZxVgm7S9il8HfNNqpjv++5W8FB+vXN7Xq5j0o1+CpMdklOaSJPIw")]
	public async Task CheckPasswordAsync_WithCorrectAndWrongPassword_ReturnsExpectedResult(string hashAlgorithm, string storedHash)
	{
		var membership = this.SetupMembership(hashAlgorithm);
		this.SetupStoredUser(membership, storedHash);
		var userService = this.CreateUserService();
		var utilizer = CreateUtilizer(membership);
		
		Assert.True(await userService.CheckPasswordAsync(utilizer, Password, TestContext.Current.CancellationToken));
		Assert.False(await userService.CheckPasswordAsync(utilizer, "wrong-password", TestContext.Current.CancellationToken));
	}
	
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public async Task CheckPasswordAsync_WithEmptyPassword_ReturnsFalse(string? password)
	{
		var membership = this.SetupMembership("SHA2-256");
		this.SetupStoredUser(membership, Sha256OfPassword);
		var userService = this.CreateUserService();
		
		Assert.False(await userService.CheckPasswordAsync(CreateUtilizer(membership), password!, TestContext.Current.CancellationToken));
	}
	
	[Fact]
	public async Task CheckPasswordAsync_WithoutStoredPasswordHash_ReturnsFalse()
	{
		var membership = this.SetupMembership("SHA2-256");
		this.SetupStoredUser(membership, passwordHash: null);
		var userService = this.CreateUserService();
		
		Assert.False(await userService.CheckPasswordAsync(CreateUtilizer(membership), Password, TestContext.Current.CancellationToken));
	}
	
	[Fact]
	public async Task CheckPasswordAsync_WithUnknownMembership_ThrowsMembershipNotFound()
	{
		var userService = this.CreateUserService();
		var utilizer = new Utilizer { Id = UserId, Username = "john.doe", MembershipId = "unknown-membership" };
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => userService.CheckPasswordAsync(utilizer, Password, TestContext.Current.CancellationToken));
		
		Assert.Equal("MembershipNotFound", exception.ErrorCode);
	}
	
	#endregion
}
