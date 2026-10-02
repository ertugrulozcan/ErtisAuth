using System.Net;
using Ertis.Core.Exceptions;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// Token code policies: validation, and the policy a membership refers to (Membership.CodePolicy, by slug) can't be
/// deleted and keeps its slug.
/// </summary>
public class TokenCodePolicyServiceTests
{
	#region Constants
	
	private const string MembershipId = "5f8a1b2c3d4e5f6a7b8c9d00";
	
	#endregion
	
	#region Fields
	
	private readonly IMembershipService _membershipService = Substitute.For<IMembershipService>();
	
	private readonly ICodePolicyRepository _repository = Substitute.For<ICodePolicyRepository>();
	
	private readonly List<TokenCodePolicy> _policies;
	
	private readonly Membership _membership;
	
	#endregion
	
	#region Constructors
	
	public TokenCodePolicyServiceTests()
	{
		// ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
		this._policies = InMemoryRepository.Setup(this._repository, x => x.Id ??= ObjectId.GenerateNewId().ToString());
		
		this._membership = TestServiceFactory.CreateMembership();
		this._membership.Id = MembershipId;
		this._membershipService.GetAsync(MembershipId, Arg.Any<CancellationToken>()).Returns(this._membership);
	}
	
	#endregion
	
	#region Helpers
	
	private TokenCodePolicyService CreateService()
	{
		return new TokenCodePolicyService(this._membershipService, Substitute.For<IEventService>(), this._repository, NullLogger<TokenCodePolicyService>.Instance);
	}
	
	private static TokenCodePolicy CreatePolicy(string name = "TV Code", int length = 6, int expiresIn = 300)
	{
		return new TokenCodePolicy { Name = name, Length = length, ContainsDigits = true, ExpiresIn = expiresIn, MembershipId = MembershipId };
	}
	
	private Task<TokenCodePolicy> CreateAsync(TokenCodePolicyService service, TokenCodePolicy policy)
	{
		return service.CreateAsync(policy, MembershipId, Utilizer.GetSystemUtilizer(MembershipId), TestContext.Current.CancellationToken);
	}
	
	#endregion
	
	#region Create
	
	[Theory]
	[InlineData(0, 300, "Length must be greater than zero")]
	[InlineData(6, 0, "Expires in must be greater than zero")]
	public async Task CreateAsync_WithInvalidPolicy_ThrowsValidationError(int length, int expiresIn, string expectedError)
	{
		var exception = await Assert.ThrowsAsync<ValidationException>(() => this.CreateAsync(this.CreateService(), CreatePolicy(length: length, expiresIn: expiresIn)));
		
		Assert.Contains(expectedError, exception.Errors!);
	}
	
	[Fact]
	public async Task CreateAsync_WithExistingSlug_ThrowsConflict()
	{
		var service = this.CreateService();
		await this.CreateAsync(service, CreatePolicy());
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.CreateAsync(service, CreatePolicy()));
		
		Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
	}
	
	[Fact]
	public async Task GetBySlugAsync_ReturnsThePolicyOfTheMembership()
	{
		var service = this.CreateService();
		var created = await this.CreateAsync(service, CreatePolicy());
		
		Assert.Equal(created.Id, (await service.GetBySlugAsync("tv-code", MembershipId, TestContext.Current.CancellationToken))?.Id);
		Assert.Null(await service.GetBySlugAsync("tv-code", "5f8a1b2c3d4e5f6a7b8c9dff", TestContext.Current.CancellationToken));
	}
	
	/// <summary>
	/// The created event is stored in the background: a failure is logged (it was lost unobserved) and the policy is still created
	/// </summary>
	[Fact]
	public async Task CreateAsync_WhenTheEventCanNotBeStored_LogsTheErrorAndCreatesThePolicy()
	{
		var eventService = Substitute.For<IEventService>();
		eventService
			.FireEventAsync(Arg.Any<ErtisAuthEventType>(), Arg.Any<Utilizer>(), Arg.Any<string?>(), Arg.Any<object?>(), Arg.Any<object?>(), Arg.Any<CancellationToken>())
			.ThrowsAsync(new InvalidOperationException("Database unavailable"));
		var logger = Substitute.For<ILogger<TokenCodePolicyService>>();
		var service = new TokenCodePolicyService(this._membershipService, eventService, this._repository, logger);
		
		var created = await this.CreateAsync(service, CreatePolicy());
		
		Assert.Contains(this._policies, x => x.Id == created.Id);
		Assert.Contains(logger.ReceivedCalls(), x => x.GetMethodInfo().Name == nameof(ILogger.Log) && (LogLevel) x.GetArguments()[0]! == LogLevel.Error && x.GetArguments()[3] is InvalidOperationException);
	}
	
	#endregion
	
	#region Policy in Use
	
	[Fact]
	public async Task DeleteAsync_WithThePolicyOfTheMembership_ThrowsTokenCodePolicyInUse()
	{
		var service = this.CreateService();
		var policy = await this.CreateAsync(service, CreatePolicy());
		this._membership.CodePolicy = policy.Slug;
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => service.DeleteAsync(policy.Id, MembershipId, Utilizer.GetSystemUtilizer(MembershipId), TestContext.Current.CancellationToken));
		
		Assert.Equal("TokenCodePolicyInUse", exception.ErrorCode);
		Assert.Single(this._policies);
	}
	
	[Fact]
	public async Task DeleteAsync_WithAnUnusedPolicy_Deletes()
	{
		var service = this.CreateService();
		var policy = await this.CreateAsync(service, CreatePolicy());
		this._membership.CodePolicy = "another-policy";
		
		Assert.True(await service.DeleteAsync(policy.Id, MembershipId, Utilizer.GetSystemUtilizer(MembershipId), TestContext.Current.CancellationToken));
		Assert.Empty(this._policies);
	}
	
	[Fact]
	public async Task BulkDeleteAsync_IncludingThePolicyOfTheMembership_DeletesNothing()
	{
		var service = this.CreateService();
		
		// ReSharper disable once RedundantArgumentDefaultValue
		var used = await this.CreateAsync(service, CreatePolicy("TV Code"));
		var unused = await this.CreateAsync(service, CreatePolicy("Legacy Code"));
		this._membership.CodePolicy = used.Slug;
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => service.BulkDeleteAsync([unused.Id, used.Id], MembershipId, Utilizer.GetSystemUtilizer(MembershipId), TestContext.Current.CancellationToken));
		
		Assert.Equal("TokenCodePolicyInUse", exception.ErrorCode);
		Assert.Equal(2, this._policies.Count);
	}
	
	[Fact]
	public async Task UpdateAsync_RenamingThePolicyOfTheMembership_KeepsItsSlug()
	{
		var service = this.CreateService();
		
		// ReSharper disable once RedundantArgumentDefaultValue
		var policy = await this.CreateAsync(service, CreatePolicy("TV Code"));
		this._membership.CodePolicy = policy.Slug;
		var update = CreatePolicy("Smart TV Code", length: 8);
		update.Id = policy.Id;
		
		var updated = await service.UpdateAsync(update, MembershipId, Utilizer.GetSystemUtilizer(MembershipId), TestContext.Current.CancellationToken);
		
		Assert.Equal("Smart TV Code", updated.Name);
		Assert.Equal("tv-code", updated.Slug);
		Assert.Equal(8, updated.Length);
	}
	
	[Fact]
	public async Task UpdateAsync_RenamingAnUnusedPolicy_ChangesItsSlug()
	{
		var service = this.CreateService();
		
		// ReSharper disable once RedundantArgumentDefaultValue
		var policy = await this.CreateAsync(service, CreatePolicy("TV Code"));
		var update = CreatePolicy("Smart TV Code");
		update.Id = policy.Id;
		
		var updated = await service.UpdateAsync(update, MembershipId, Utilizer.GetSystemUtilizer(MembershipId), TestContext.Current.CancellationToken);
		
		Assert.Equal("smart-tv-code", updated.Slug);
	}
	
	#endregion
}