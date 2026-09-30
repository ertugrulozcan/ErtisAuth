using Ertis.Core.Exceptions;
using Ertis.Schema.Types;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
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
/// Users (user_type) and inherited user types (baseType) refer to a user type by its slug:
/// a user type in use can not be deleted and its slug can not change.
/// </summary>
public class UserTypeServiceUsageTests
{
	#region Constants
	
	private const string UserTypeId = "5f8a1b2c3d4e5f6a7b8c9d01";
	
	#endregion
	
	#region Fields
	
	private readonly IMembershipService _membershipService = Substitute.For<IMembershipService>();
	
	private readonly IUserTypeRepository _repository = Substitute.For<IUserTypeRepository>();
	
	private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
	
	private readonly List<UserType> _userTypes;
	
	private readonly Membership _membership;
	
	private readonly Utilizer _utilizer;
	
	private long _userCount;
	
	#endregion
	
	#region Constructors
	
	public UserTypeServiceUsageTests()
	{
		this._membership = TestServiceFactory.CreateMembership("SHA2-256");
		this._membershipService.GetAsync(this._membership.Id, Arg.Any<CancellationToken>()).Returns(this._membership);
		this._utilizer = Utilizer.GetSystemUtilizer(this._membership.Id);
		
		this._userTypes = InMemoryRepository.Setup(this._repository);
		this._userTypes.Add(this.NewUserType("Customer"));
		
		// Only users of this membership with the slug of the user type are counted
		this._userRepository
			.CountAsync(default(string)!)
			.ReturnsForAnyArgs(callInfo =>
			{
				var query = callInfo.ArgAt<string>(0);
				return query.Contains(this._membership.Id) && query.Contains("\"user_type\"") && query.Contains("\"customer\"") ? this._userCount : 0;
			});
	}
	
	#endregion
	
	#region Helpers
	
	private UserTypeService CreateUserTypeService()
	{
		return new UserTypeService(this._membershipService, Substitute.For<IEventService>(), this._repository, this._userRepository, Substitute.For<IUserUniqueIndexSynchronizer>(), new MemoryCache(new MemoryCacheOptions()));
	}
	
	private UserType NewUserType(string name, string? slug = null)
	{
		var userType = new UserType
		{
			Id = UserTypeId,
			Name = name,
			Properties = Array.Empty<IFieldInfo>(),
			IsAbstract = false,
			AllowAdditionalProperties = false,
			BaseUserType = UserType.ORIGIN_USER_TYPE_SLUG,
			MembershipId = this._membership.Id
		};
		
		if (slug != null)
		{
			userType.Slug = slug;
		}
		
		return userType;
	}
	
	private void SetupInheritedUserType()
	{
		this._userTypes.Add(new UserType
		{
			Id = "vip-customer-id",
			Name = "VIP Customer",
			Properties = Array.Empty<IFieldInfo>(),
			BaseUserType = "customer",
			MembershipId = this._membership.Id
		});
	}
	
	private async Task<UserType> UpdateAsync(UserType model)
	{
		return await this.CreateUserTypeService().UpdateAsync(model, this._membership.Id, this._utilizer, TestContext.Current.CancellationToken);
	}
	
	private UserType StoredUserType => this._userTypes.Single(x => x.Id == UserTypeId);
	
	#endregion
	
	#region Delete
	
	[Fact]
	public async Task DeleteAsync_WhenUsersHaveTheUserType_IsRejected()
	{
		this._userCount = 3;
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.CreateUserTypeService().DeleteAsync(UserTypeId, this._membership.Id, this._utilizer, TestContext.Current.CancellationToken));
		
		Assert.Equal("UserTypeCanNotBeDelete", exception.ErrorCode);
		Assert.Contains(this._userTypes, x => x.Id == UserTypeId);
	}
	
	[Fact]
	public async Task DeleteAsync_WhenAnotherUserTypeInheritsFromIt_IsRejected()
	{
		this.SetupInheritedUserType();
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.CreateUserTypeService().DeleteAsync(UserTypeId, this._membership.Id, this._utilizer, TestContext.Current.CancellationToken));
		
		Assert.Equal("UserTypeCanNotBeDelete", exception.ErrorCode);
		Assert.Contains(this._userTypes, x => x.Id == UserTypeId);
	}
	
	[Fact]
	public async Task DeleteAsync_WhenNotInUse_DeletesUserType()
	{
		Assert.True(await this.CreateUserTypeService().DeleteAsync(UserTypeId, this._membership.Id, this._utilizer, TestContext.Current.CancellationToken));
		
		Assert.DoesNotContain(this._userTypes, x => x.Id == UserTypeId);
	}
	
	#endregion
	
	#region Slug
	
	[Fact]
	public async Task UpdateAsync_RenamingUserTypeInUse_KeepsSlug()
	{
		this._userCount = 3;
		
		await this.UpdateAsync(this.NewUserType("Client"));
		
		Assert.Equal("Client", this.StoredUserType.Name);
		Assert.Equal("customer", this.StoredUserType.Slug);
	}
	
	[Fact]
	public async Task UpdateAsync_ChangingOnlySlugOfUserTypeInUse_IsReportedAsIdentical()
	{
		this._userCount = 3;
		
		// The slug is kept, so nothing is left to change
		var exception = await Assert.ThrowsAsync<ValidationException>(() => this.UpdateAsync(this.NewUserType("Customer", slug: "buyer")));
		
		Assert.Equal("IdenticalDocumentError", exception.ErrorCode);
		Assert.Equal("customer", this.StoredUserType.Slug);
	}
	
	[Fact]
	public async Task UpdateAsync_RenamingUserTypeInheritedByAnother_KeepsSlug()
	{
		this.SetupInheritedUserType();
		
		await this.UpdateAsync(this.NewUserType("Client"));
		
		Assert.Equal("Client", this.StoredUserType.Name);
		Assert.Equal("customer", this.StoredUserType.Slug);
	}
	
	[Fact]
	public async Task UpdateAsync_RenamingUserTypeNotInUse_ChangesSlug()
	{
		await this.UpdateAsync(this.NewUserType("Client"));
		
		Assert.Equal("Client", this.StoredUserType.Name);
		Assert.Equal("client", this.StoredUserType.Slug);
	}
	
	[Fact]
	public async Task UpdateAsync_ChangingSlugOfUserTypeNotInUse_ChangesSlug()
	{
		await this.UpdateAsync(this.NewUserType("Customer", slug: "buyer"));
		
		Assert.Equal("buyer", this.StoredUserType.Slug);
	}
	
	#endregion
}
