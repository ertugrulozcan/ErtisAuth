using Ertis.Core.Collections;
using Ertis.Schema.Types;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;

namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// User types are cached by name and slug; renamed or deleted user types must not be served from the cache.
/// </summary>
public class UserTypeServiceCacheTests
{
	#region Constants
	
	private const string UserTypeId = "5f8a1b2c3d4e5f6a7b8c9d01";
	
	#endregion
	
	#region Fields
	
	private readonly IMembershipService _membershipService = Substitute.For<IMembershipService>();
	
	private readonly IUserTypeRepository _repository = Substitute.For<IUserTypeRepository>();
	
	private readonly List<UserType> _userTypes;
	
	private readonly Membership _membership;
	
	private readonly Utilizer _utilizer;
	
	#endregion
	
	#region Constructors
	
	public UserTypeServiceCacheTests()
	{
		this._membership = TestServiceFactory.CreateMembership("SHA2-256");
		this._membershipService.Get(this._membership.Id).Returns(this._membership);
		this._membershipService.GetAsync(this._membership.Id, Arg.Any<CancellationToken>()).Returns(this._membership);
		this._utilizer = Utilizer.GetSystemUtilizer(this._membership.Id);
		
		this._userTypes = InMemoryRepository.Setup(this._repository);
		this._userTypes.Add(this.NewUserType("Customer"));
		
		// No user type inherits from another one, so every user type is deletable
		this._repository
			.QueryAsync(default(string)!, orderBy: default)
			.ReturnsForAnyArgs(_ => new PaginationCollection<dynamic> { Count = 0, Items = [] });
	}
	
	#endregion
	
	#region Helpers
	
	private UserTypeService CreateUserTypeService()
	{
		return new UserTypeService(this._membershipService, Substitute.For<IEventService>(), this._repository, new MemoryCache(new MemoryCacheOptions()));
	}
	
	private UserType NewUserType(string name)
	{
		return new UserType
		{
			Id = UserTypeId,
			Name = name,
			Properties = Array.Empty<IFieldInfo>(),
			IsAbstract = false,
			AllowAdditionalProperties = false,
			BaseUserType = UserType.ORIGIN_USER_TYPE_SLUG,
			MembershipId = this._membership.Id
		};
	}
	
	private async Task<UserType?> GetByNameOrSlugAsync(UserTypeService userTypeService, string nameOrSlug)
	{
		return await userTypeService.GetByNameOrSlugAsync(this._membership.Id, nameOrSlug, cancellationToken: TestContext.Current.CancellationToken);
	}
	
	#endregion
	
	#region Update
	
	[Fact]
	public async Task UpdateAsync_WithNewName_DoesNotServeUserTypeByOldNameOrSlug()
	{
		var userTypeService = this.CreateUserTypeService();
		Assert.NotNull(await this.GetByNameOrSlugAsync(userTypeService, "Customer"));
		Assert.NotNull(await this.GetByNameOrSlugAsync(userTypeService, "customer"));
		
		await userTypeService.UpdateAsync(this._utilizer, this._membership.Id, this.NewUserType("Client"), TestContext.Current.CancellationToken);
		
		Assert.Null(await this.GetByNameOrSlugAsync(userTypeService, "Customer"));
		Assert.Null(await this.GetByNameOrSlugAsync(userTypeService, "customer"));
		Assert.Equal("Client", (await this.GetByNameOrSlugAsync(userTypeService, "client"))?.Name);
	}
	
	#endregion
	
	#region Delete
	
	[Fact]
	public async Task DeleteAsync_DoesNotServeDeletedUserTypeFromCache()
	{
		var userTypeService = this.CreateUserTypeService();
		Assert.NotNull(await this.GetByNameOrSlugAsync(userTypeService, "Customer"));
		Assert.NotNull(await this.GetByNameOrSlugAsync(userTypeService, "customer"));
		
		Assert.True(await userTypeService.DeleteAsync(this._utilizer, this._membership.Id, UserTypeId, TestContext.Current.CancellationToken));
		
		Assert.Null(await this.GetByNameOrSlugAsync(userTypeService, "Customer"));
		Assert.Null(await this.GetByNameOrSlugAsync(userTypeService, "customer"));
	}
	
	#endregion
}
