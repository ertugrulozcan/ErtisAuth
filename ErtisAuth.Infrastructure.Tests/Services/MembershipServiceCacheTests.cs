using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// Memberships are cached by id and by secret key; stale entries must not survive secret key changes or deletion.
/// </summary>
public class MembershipServiceCacheTests
{
	#region Constants
	
	private const string OldSecretKey = "old-secret-key-old-secret-key-old-secret-key";
	
	private const string NewSecretKey = "new-secret-key-new-secret-key-new-secret-key";
	
	#endregion
	
	#region Fields
	
	private readonly IMembershipRepository _repository = Substitute.For<IMembershipRepository>();
	
	private readonly Membership _membership;
	
	#endregion
	
	#region Constructors
	
	public MembershipServiceCacheTests()
	{
		var memberships = InMemoryRepository.Setup(this._repository);
		this._membership = this.NewMembership(OldSecretKey);
		memberships.Add(this._membership);
	}
	
	#endregion
	
	#region Helpers
	
	private MembershipService CreateMembershipService()
	{
		return new MembershipService(this._repository, new MemoryCache(new MemoryCacheOptions()));
	}
	
	private Membership NewMembership(string secretKey)
	{
		var membership = TestServiceFactory.CreateMembership("SHA2-256");
		membership.SecretKey = secretKey;
		return membership;
	}
	
	#endregion
	
	#region Update
	
	[Fact]
	public async Task UpdateAsync_WithNewSecretKey_DoesNotServeMembershipByOldSecretKey()
	{
		var membershipService = this.CreateMembershipService();
		Assert.NotNull(await membershipService.GetBySecretKeyAsync(OldSecretKey, TestContext.Current.CancellationToken));
		Assert.NotNull(await membershipService.GetAsync(this._membership.Id, TestContext.Current.CancellationToken));
		
		await membershipService.UpdateAsync(Utilizer.GetSystemUtilizer(string.Empty), this.NewMembership(NewSecretKey), TestContext.Current.CancellationToken);
		
		Assert.Null(await membershipService.GetBySecretKeyAsync(OldSecretKey, TestContext.Current.CancellationToken));
		Assert.Equal(this._membership.Id, (await membershipService.GetBySecretKeyAsync(NewSecretKey, TestContext.Current.CancellationToken))?.Id);
		Assert.Equal(NewSecretKey, (await membershipService.GetAsync(this._membership.Id, TestContext.Current.CancellationToken))?.SecretKey);
	}
	
	#endregion
	
	#region Delete
	
	[Fact]
	public async Task DeleteAsync_DoesNotServeDeletedMembershipFromCache()
	{
		var membershipService = this.CreateMembershipService();
		Assert.NotNull(await membershipService.GetAsync(this._membership.Id, TestContext.Current.CancellationToken));
		Assert.NotNull(await membershipService.GetBySecretKeyAsync(OldSecretKey, TestContext.Current.CancellationToken));
		
		Assert.True(await membershipService.DeleteAsync(this._membership.Id, TestContext.Current.CancellationToken));
		
		Assert.Null(await membershipService.GetAsync(this._membership.Id, TestContext.Current.CancellationToken));
		Assert.Null(await membershipService.GetBySecretKeyAsync(OldSecretKey, TestContext.Current.CancellationToken));
	}
	
	#endregion
	
	#region Cache Keys
	
	[Fact]
	public async Task GetAsync_WithSecretKeyAsId_DoesNotHitSecretKeyCacheEntry()
	{
		var membershipService = this.CreateMembershipService();
		Assert.NotNull(await membershipService.GetBySecretKeyAsync(OldSecretKey, TestContext.Current.CancellationToken));
		
		Assert.Null(await membershipService.GetAsync(OldSecretKey, TestContext.Current.CancellationToken));
	}
	
	[Fact]
	public async Task GetBySecretKeyAsync_WithIdAsSecretKey_DoesNotHitIdCacheEntry()
	{
		var membershipService = this.CreateMembershipService();
		Assert.NotNull(await membershipService.GetAsync(this._membership.Id, TestContext.Current.CancellationToken));
		
		Assert.Null(await membershipService.GetBySecretKeyAsync(this._membership.Id, TestContext.Current.CancellationToken));
	}
	
	#endregion
}
