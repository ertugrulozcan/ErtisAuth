using Ertis.Core.Exceptions;
using Ertis.Data.Models;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;

namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// Hash algorithm and encoding validation on membership create/update.
/// </summary>
public class MembershipServiceValidationTests
{
	#region Fields
	
	private readonly IMembershipRepository _repository = Substitute.For<IMembershipRepository>();
	
	#endregion
	
	#region Helpers
	
	private MembershipService CreateMembershipService()
	{
		return new MembershipService(this._repository, new MemoryCache(new MemoryCacheOptions()));
	}
	
	// ReSharper disable once UnusedMethodReturnValue.Local
	private static async Task<ValidationException> AssertValidationErrorAsync(Func<Task> action, string expectedError)
	{
		var exception = await Assert.ThrowsAsync<ValidationException>(action);
		Assert.Equal("ModelValidationError", exception.ErrorCode);
		Assert.NotNull(exception.Errors);
		Assert.Contains(expectedError, exception.Errors);
		return exception;
	}
	
	#endregion
	
	#region Hash Algorithm
	
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public async Task CreateAsync_WithoutHashAlgorithm_ThrowsValidationError(string? hashAlgorithm)
	{
		var membershipService = this.CreateMembershipService();
		var membership = TestServiceFactory.CreateMembership(hashAlgorithm);
		
		await AssertValidationErrorAsync(() => membershipService.CreateAsync(membership, TestContext.Current.CancellationToken), "hash_algorithm is a required field");
		await this._repository.DidNotReceiveWithAnyArgs().InsertAsync(null!, cancellationToken: TestContext.Current.CancellationToken);
	}
	
	[Theory]
	[InlineData("UNKNOWN")]
	[InlineData("SHA3_512")]
	[InlineData("ARGON2")]
	[InlineData("PBKDF2-SHA1")]
	public async Task CreateAsync_WithUnsupportedHashAlgorithm_ThrowsValidationError(string hashAlgorithm)
	{
		var membershipService = this.CreateMembershipService();
		var membership = TestServiceFactory.CreateMembership(hashAlgorithm);
		
		await AssertValidationErrorAsync(() => membershipService.CreateAsync(membership, TestContext.Current.CancellationToken), $"Unsupported hash algorithm ({hashAlgorithm})");
		await this._repository.DidNotReceiveWithAnyArgs().InsertAsync(null!, cancellationToken: TestContext.Current.CancellationToken);
	}
	
	[Theory]
	[InlineData("MD5")]
	[InlineData("SHA2-256")]
	[InlineData("SHA3-512")]
	[InlineData("ARGON2ID")]
	[InlineData("PBKDF2-SHA256")]
	[InlineData("PBKDF2-SHA512")]
	public async Task CreateAsync_WithSupportedHashAlgorithm_InsertsMembership(string hashAlgorithm)
	{
		var membershipService = this.CreateMembershipService();
		var membership = TestServiceFactory.CreateMembership(hashAlgorithm);
		
		await membershipService.CreateAsync(membership, TestContext.Current.CancellationToken);
		
		await this._repository.Received(1).InsertAsync(membership, Arg.Any<InsertOptions?>(), Arg.Any<CancellationToken>());
	}
	
	[Fact]
	public async Task UpdateAsync_WhenStoredMembershipHasNoHashAlgorithm_RequiresOne()
	{
		var current = TestServiceFactory.CreateMembership(hashAlgorithm: null);
		this._repository.FindOneAsync(current.Id, Arg.Any<CancellationToken>()).Returns(current);
		var membershipService = this.CreateMembershipService();
		var update = TestServiceFactory.CreateMembership(hashAlgorithm: null);
		
		var exception = await Assert.ThrowsAsync<ValidationException>(() => membershipService.UpdateAsync(update, TestContext.Current.CancellationToken));
		
		Assert.NotNull(exception.Errors);
		Assert.Contains("hash_algorithm is a required field", exception.Errors);
	}
	
	[Fact]
	public async Task UpdateAsync_WhenStoredMembershipHasNoHashAlgorithmAndUpdateProvidesOne_Succeeds()
	{
		var current = TestServiceFactory.CreateMembership(hashAlgorithm: null);
		this._repository.FindOneAsync(current.Id, Arg.Any<CancellationToken>()).Returns(current);
		var membershipService = this.CreateMembershipService();
		var update = TestServiceFactory.CreateMembership("SHA2-256");
		
		await membershipService.UpdateAsync(update, TestContext.Current.CancellationToken);
		
		Assert.Equal("SHA2-256", update.HashAlgorithm);
	}
	
	#endregion
	
	#region Encoding
	
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("utf-8")]
	[InlineData("UTF-8")]
	[InlineData("utf-16")]
	[InlineData("iso-8859-1")]
	public async Task CreateAsync_WithEmptyOrKnownEncoding_InsertsMembership(string? defaultEncoding)
	{
		var membershipService = this.CreateMembershipService();
		var membership = TestServiceFactory.CreateMembership("ARGON2ID", defaultEncoding);
		
		await membershipService.CreateAsync(membership, TestContext.Current.CancellationToken);
		
		await this._repository.Received(1).InsertAsync(membership, Arg.Any<InsertOptions?>(), Arg.Any<CancellationToken>());
	}
	
	[Fact]
	public async Task CreateAsync_WithUnknownEncoding_ThrowsValidationError()
	{
		var membershipService = this.CreateMembershipService();
		var membership = TestServiceFactory.CreateMembership("ARGON2ID", "unknown-encoding");
		
		await AssertValidationErrorAsync(() => membershipService.CreateAsync(membership, TestContext.Current.CancellationToken), "Unsupported encoding (unknown-encoding)");
	}
	
	#endregion
	
	#region Membership Model
	
	[Fact]
	public void GetHashAlgorithm_WithoutHashAlgorithm_DoesNotFallBackToDefault()
	{
		var membership = new Membership
		{
			Id = "membership-id",
			Name = "Test Membership",
			SecretKey = "secret"
		};
		
		Assert.False(membership.TryGetHashAlgorithm(out _));
	}
	
	#endregion
}
