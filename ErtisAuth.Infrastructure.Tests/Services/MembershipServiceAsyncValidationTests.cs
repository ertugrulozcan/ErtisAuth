using System.Linq.Expressions;
using Ertis.Core.Exceptions;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// The async create and update flows validate the slug uniqueness without a blocking database call.
/// </summary>
public class MembershipServiceAsyncValidationTests
{
	#region Fields
	
	private readonly IMembershipRepository _repository = Substitute.For<IMembershipRepository>();
	
	// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
	private readonly Membership _existing;
	
	#endregion
	
	#region Constructors
	
	public MembershipServiceAsyncValidationTests()
	{
		var memberships = InMemoryRepository.Setup(this._repository);
		this._existing = TestServiceFactory.CreateMembership("SHA2-256");
		memberships.Add(this._existing);
	}
	
	#endregion
	
	#region Helpers
	
	private MembershipService CreateMembershipService()
	{
		return new MembershipService(this._repository, new MemoryCache(new MemoryCacheOptions()));
	}
	
	private void AssertNoBlockingSlugLookup()
	{
		this._repository.DidNotReceive().FindOne(Arg.Any<Expression<Func<Membership, bool>>>());
	}
	
	#endregion
	
	#region Tests
	
	[Fact]
	public async Task CreateAsync_WithSlugOfAnotherMembership_IsRejected()
	{
		var membership = TestServiceFactory.CreateMembership("SHA2-256");
		membership.Id = "another-membership-id";
		
		var exception = await Assert.ThrowsAsync<ValidationException>(() => this.CreateMembershipService().CreateAsync(Utilizer.GetSystemUtilizer(string.Empty), membership, TestContext.Current.CancellationToken));
		
		Assert.NotNull(exception.Errors);
		Assert.Contains(exception.Errors, x => x.Contains(membership.Name));
		this.AssertNoBlockingSlugLookup();
	}
	
	[Fact]
	public async Task UpdateAsync_KeepingOwnSlug_Succeeds()
	{
		var update = TestServiceFactory.CreateMembership("SHA2-256");
		update.ExpiresIn = 7200;
		
		await this.CreateMembershipService().UpdateAsync(Utilizer.GetSystemUtilizer(string.Empty), update, TestContext.Current.CancellationToken);
		
		Assert.Equal(7200, update.ExpiresIn);
		this.AssertNoBlockingSlugLookup();
	}
	
	#endregion
}
