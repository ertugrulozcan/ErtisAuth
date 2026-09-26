using System.Net;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// Characterization tests for username/password sign-in (TokenService.GenerateTokenAsync).
/// The real UserService.VerifyPassword is used, so these tests lock the end-to-end legacy password check.
/// </summary>
public class TokenServicePasswordLoginTests
{
	#region Constants
	
	private const string Username = "john.doe";
	
	private const string Password = "P@ssw0rd!";
	
	private const string Sha256OfPassword = "0e44ce7308af2b3de5232e4616403ce7d49ba2aec83f79c196409556422a4927";
	
	#endregion
	
	#region Fields
	
	private readonly IMembershipService _membershipService = Substitute.For<IMembershipService>();
	
	private readonly IUserService _userService = Substitute.For<IUserService>();
	
	private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
	
	private readonly IActiveTokenService _activeTokenService = Substitute.For<IActiveTokenService>();
	
	private readonly UserService _realUserService = TestServiceFactory.CreateUserService();
	
	#endregion
	
	#region Constructors
	
	public TokenServicePasswordLoginTests()
	{
		this._userService
			.VerifyPassword(Arg.Any<Membership>(), Arg.Any<string>(), Arg.Any<string?>())
			.Returns(x => this._realUserService.VerifyPassword(x.ArgAt<Membership>(0), x.ArgAt<string>(1), x.ArgAt<string?>(2)));
			
		this._jwtService
			.GenerateToken(Arg.Any<TokenClaims>(), Arg.Any<DateTime?>(), Arg.Any<TimeSpan?>(), Arg.Any<System.Text.Encoding?>())
			.Returns("jwt-token");
	}
	
	#endregion
	
	#region Helpers
	
	private TokenService CreateTokenService()
	{
		return new TokenService(
			this._membershipService,
			this._userService,
			Substitute.For<IApplicationService>(),
			Substitute.For<IRoleService>(),
			this._jwtService,
			Substitute.For<IEventService>(),
			this._activeTokenService,
			Substitute.For<IRevokedTokenService>(),
			NullLogger<TokenService>.Instance);
	}
	
	private Membership SetupMembership(string? hashAlgorithm = "SHA2-256")
	{
		var membership = TestServiceFactory.CreateMembership(hashAlgorithm);
		this._membershipService.GetAsync(membership.Id, Arg.Any<CancellationToken>()).Returns(membership);
		return membership;
	}
	
	private void SetupUser(Membership membership, string? passwordHash, bool isActive = true)
	{
		var user = new UserWithPasswordHash
		{
			Id = "user-id",
			Username = Username,
			EmailAddress = "john.doe@example.com",
			Role = "user",
			IsActive = isActive,
			MembershipId = membership.Id,
			PasswordHash = passwordHash
		};
		
		this._userService
			.GetUserWithPasswordAsync(membership.Id, Username, Username, Arg.Any<CancellationToken>())
			.Returns(user);
	}
	
	#endregion
	
	#region Successful Sign-in
	
	[Theory]
	[InlineData("MD5", "8a24367a1f46c141048752f2d5bbd14b")]
	[InlineData("SHA1", "076d3e6c4b9f654b5b220b9045b7458ab6b4cbc6")]
	[InlineData("SHA2-256", Sha256OfPassword)]
	[InlineData("SHA2-512-256", "f85c8eeee9d3e814996f813ce1b89f144f0737b36813e7eccf0f8373260e4676")]
	[InlineData("SHA3-512", "a3cba91b5f7abc3d35e3e0603caba9ff85f36ccae7e3f5901cf6fa58186fe587dada0dc90fdffc35ab5383080d84afa93a47138f9735d1fee4bea1ca4ab74157")]
	[InlineData("ARGON2ID", "$argon2id$v=19$m=19456,t=2,p=1$RXJ0aXNBdXRoU2FsdCExNg$fbBQyhNR+kJrPcAQUBon1Sg6Erqte6DHJbnLH1xxVCc")]
	[InlineData("PBKDF2-SHA256", "$pbkdf2-sha256$i=600000$RXJ0aXNBdXRoU2FsdCExNg$KHerChsPs4Ruu9BoHAgcQZwRsn9TwHuFh32Nm17nPTU")]
	// Stored PBKDF2 hash while the membership has since been switched to Argon2id: PHC hashes carry their own algorithm.
	[InlineData("ARGON2ID", "$pbkdf2-sha512$i=210000$RXJ0aXNBdXRoU2FsdCExNg$EyVtj8iEUqCqsFGtUYNm5qVfvK4KkX8D02ZxVgm7S9il8HfNNqpjv++5W8FB+vXN7Xq5j0o1+CpMdklOaSJPIw")]
	public async Task GenerateTokenAsync_WithCorrectPasswordAndStoredHash_ReturnsBearerToken(string hashAlgorithm, string storedHash)
	{
		var membership = this.SetupMembership(hashAlgorithm);
		this.SetupUser(membership, storedHash);
		var tokenService = this.CreateTokenService();
		
		var token = await tokenService.GenerateTokenAsync(Username, Password, membership.Id, fireEvent: false, cancellationToken: TestContext.Current.CancellationToken);
		
		Assert.Equal("jwt-token", token.AccessToken);
		Assert.Equal("jwt-token", token.RefreshToken);
		await this._activeTokenService.ReceivedWithAnyArgs(1).CreateAsync(default!, default!, default!, default, default, TestContext.Current.CancellationToken);
	}
	
	#endregion
	
	#region Failed Sign-in
	
	[Fact]
	public async Task GenerateTokenAsync_WithWrongPassword_ThrowsInvalidCredentials()
	{
		var membership = this.SetupMembership();
		this.SetupUser(membership, Sha256OfPassword);
		var tokenService = this.CreateTokenService();
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.GenerateTokenAsync(Username, "wrong-password", membership.Id, cancellationToken: TestContext.Current.CancellationToken));
		
		AssertInvalidCredentials(exception);
		await this._activeTokenService.DidNotReceiveWithAnyArgs().CreateAsync(default!, default!, default!, default, default, TestContext.Current.CancellationToken);
	}
	
	[Fact]
	public async Task GenerateTokenAsync_WhenMembershipAlgorithmDiffersFromStoredHashAlgorithm_ThrowsInvalidCredentials()
	{
		// The stored hash was produced with SHA2-256, but the membership is now configured for SHA3-256.
		// Legacy hashes carry no algorithm information, so changing a membership's algorithm locks out existing users.
		var membership = this.SetupMembership("SHA3-256");
		this.SetupUser(membership, Sha256OfPassword);
		var tokenService = this.CreateTokenService();
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.GenerateTokenAsync(Username, Password, membership.Id, cancellationToken: TestContext.Current.CancellationToken));
		
		AssertInvalidCredentials(exception);
	}
	
	[Fact]
	public async Task GenerateTokenAsync_WithUppercaseStoredHash_ThrowsInvalidCredentials()
	{
		// Comparison is an exact, case-sensitive string comparison against lowercase hex.
		var membership = this.SetupMembership();
		this.SetupUser(membership, Sha256OfPassword.ToUpperInvariant());
		var tokenService = this.CreateTokenService();
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.GenerateTokenAsync(Username, Password, membership.Id, cancellationToken: TestContext.Current.CancellationToken));
		
		AssertInvalidCredentials(exception);
	}
	
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public async Task GenerateTokenAsync_WithoutStoredPasswordHash_ThrowsInvalidCredentials(string? storedHash)
	{
		var membership = this.SetupMembership();
		this.SetupUser(membership, storedHash);
		var tokenService = this.CreateTokenService();
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.GenerateTokenAsync(Username, Password, membership.Id, cancellationToken: TestContext.Current.CancellationToken));
		
		AssertInvalidCredentials(exception);
	}
	
	[Fact]
	public async Task GenerateTokenAsync_WithEmptyPassword_ThrowsInvalidCredentials()
	{
		var membership = this.SetupMembership();
		this.SetupUser(membership, Sha256OfPassword);
		var tokenService = this.CreateTokenService();
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.GenerateTokenAsync(Username, string.Empty, membership.Id, cancellationToken: TestContext.Current.CancellationToken));
		
		AssertInvalidCredentials(exception);
	}
	
	[Fact]
	public async Task GenerateTokenAsync_WithUnknownUser_ThrowsInvalidCredentials()
	{
		var membership = this.SetupMembership();
		var tokenService = this.CreateTokenService();
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.GenerateTokenAsync("unknown.user", Password, membership.Id, cancellationToken: TestContext.Current.CancellationToken));
		
		AssertInvalidCredentials(exception);
		
		// A comparable hashing cost is spent so that response times do not reveal whether the user exists
		this._userService.Received(1).CalculatePasswordHash(membership, Password);
	}
	
	[Fact]
	public async Task GenerateTokenAsync_WithInactiveUserAndWrongPassword_ThrowsInvalidCredentials()
	{
		// The account status must not be revealed to callers without valid credentials.
		var membership = this.SetupMembership();
		this.SetupUser(membership, Sha256OfPassword, isActive: false);
		var tokenService = this.CreateTokenService();
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.GenerateTokenAsync(Username, "wrong-password", membership.Id, cancellationToken: TestContext.Current.CancellationToken));
		
		AssertInvalidCredentials(exception);
	}
	
	[Fact]
	public async Task GenerateTokenAsync_WithInactiveUserAndCorrectPassword_ThrowsUserInactive()
	{
		var membership = this.SetupMembership();
		this.SetupUser(membership, Sha256OfPassword, isActive: false);
		var tokenService = this.CreateTokenService();
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.GenerateTokenAsync(Username, Password, membership.Id, cancellationToken: TestContext.Current.CancellationToken));
		
		Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
		Assert.Equal("UserInactive", exception.ErrorCode);
		await this._activeTokenService.DidNotReceiveWithAnyArgs().CreateAsync(default!, default!, default!, default, default, TestContext.Current.CancellationToken);
	}
	
	[Fact]
	public async Task GenerateTokenAsync_WithUnknownMembership_ThrowsMembershipNotFound()
	{
		var tokenService = this.CreateTokenService();
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.GenerateTokenAsync(Username, Password, "unknown-membership", cancellationToken: TestContext.Current.CancellationToken));
		
		Assert.Equal("MembershipNotFound", exception.ErrorCode);
	}
	
	[Theory]
	[InlineData(null)]
	[InlineData("SHA3_512")]
	public async Task GenerateTokenAsync_WhenMembershipHasNoValidHashAlgorithm_ThrowsMembershipHashAlgorithmInvalid(string? hashAlgorithm)
	{
		// Memberships persisted without a (valid) algorithm no longer fall back to SHA2-256; they must be fixed in the database.
		var membership = this.SetupMembership(hashAlgorithm);
		this.SetupUser(membership, Sha256OfPassword);
		var tokenService = this.CreateTokenService();
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.GenerateTokenAsync(Username, Password, membership.Id, cancellationToken: TestContext.Current.CancellationToken));
		
		Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
		Assert.Equal("MembershipHashAlgorithmInvalid", exception.ErrorCode);
	}
	
	private static void AssertInvalidCredentials(ErtisAuthException exception)
	{
		Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
		Assert.Equal("InvalidCredentials", exception.ErrorCode);
	}
	
	#endregion
}
