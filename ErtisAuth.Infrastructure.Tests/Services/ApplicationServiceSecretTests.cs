using System.Linq.Expressions;
using Ertis.Core.Exceptions;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Events;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Helpers;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;

namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// Application secret generation, rotation and protection of the stored hash, with an in-memory application store.
/// </summary>
public class ApplicationServiceSecretTests
{
	#region Fields
	
	private readonly IMembershipService _membershipService = Substitute.For<IMembershipService>();
	
	private readonly IRoleService _roleService = Substitute.For<IRoleService>();
	
	private readonly IEventService _eventService = Substitute.For<IEventService>();
	
	private readonly IApplicationRepository _repository = Substitute.For<IApplicationRepository>();
	
	private readonly List<Application> _applications = [];
	
	private readonly Membership _membership;
	
	private readonly Utilizer _utilizer;
	
	#endregion
	
	#region Constructors
	
	public ApplicationServiceSecretTests()
	{
		this._membership = TestServiceFactory.CreateMembership("SHA2-256");
		this._membershipService.Get(this._membership.Id).Returns(this._membership);
		this._membershipService.GetAsync(this._membership.Id, Arg.Any<CancellationToken>()).Returns(this._membership);
		var role = new Role
		{
			Id = "role-id",
			Name = "server",
			MembershipId = this._membership.Id
		};
		
		this._roleService.GetBySlug("server", this._membership.Id).Returns(role);
		this._roleService.GetBySlugAsync("server", this._membership.Id, Arg.Any<CancellationToken>()).Returns(role);
		
		this._utilizer = Utilizer.GetSystemUtilizer(this._membership.Id);
		
		this._repository
			.FindOneAsync(Arg.Any<Expression<Func<Application, bool>>>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => this.Find(callInfo.ArgAt<Expression<Func<Application, bool>>>(0)));
		
		this._repository
			.FindOneAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => this.Find(x => x.Id == callInfo.ArgAt<string>(0)));
		
		this._repository
			.InsertAsync(default!)
			.ReturnsForAnyArgs(callInfo => this.Store(callInfo.ArgAt<Application>(0)));
		
		this._repository
			.UpdateAsync(default!)
			.ReturnsForAnyArgs(callInfo => this.Store(callInfo.ArgAt<Application>(0)));
	}
	
	#endregion
	
	#region Helpers
	
	private ApplicationService CreateApplicationService()
	{
		return new ApplicationService(
			this._membershipService,
			this._roleService,
			this._eventService,
			new MemoryCache(new MemoryCacheOptions()),
			this._repository);
	}
	
	/// <summary>
	/// Returns a copy, as a database would, so that tests notice when a stored document is modified in place.
	/// </summary>
	private Application? Find(Expression<Func<Application, bool>> expression)
	{
		var application = this._applications.FirstOrDefault(expression.Compile());
		return application == null ? null : Copy(application);
	}
	
	private Application Store(Application application)
	{
		if (string.IsNullOrEmpty(application.Id))
		{
			application.Id = Guid.NewGuid().ToString("N")[..24];
		}
		
		this._applications.RemoveAll(x => x.Id == application.Id);
		this._applications.Add(Copy(application));
		return application;
	}
	
	private static Application Copy(Application application)
	{
		return new Application
		{
			Id = application.Id,
			MembershipId = application.MembershipId,
			Name = application.Name,
			Slug = application.Slug,
			Role = application.Role,
			Permissions = application.Permissions,
			Forbidden = application.Forbidden,
			Sys = application.Sys,
			SecretHash = application.SecretHash
		};
	}
	
	private Application NewApplicationModel(string name = "Server App")
	{
		return new Application
		{
			Name = name,
			Role = "server",
			MembershipId = this._membership.Id
		};
	}
	
	private async Task<ApplicationWithSecret> CreateWithSecretAsync(ApplicationService applicationService)
	{
		return await applicationService.CreateWithSecretAsync(this._utilizer, this._membership.Id, this.NewApplicationModel(), TestContext.Current.CancellationToken);
	}
	
	private string? StoredSecretHash(string applicationId)
	{
		return this._applications.Single(x => x.Id == applicationId).SecretHash;
	}
	
	#endregion
	
	#region Create
	
	[Fact]
	public async Task CreateWithSecretAsync_StoresOnlyTheHashOfTheReturnedSecret()
	{
		var applicationService = this.CreateApplicationService();
		
		var created = await this.CreateWithSecretAsync(applicationService);
		
		Assert.False(string.IsNullOrEmpty(created.Secret));
		var storedHash = this.StoredSecretHash(created.Id);
		Assert.Equal(ApplicationSecretHelper.HashSecret(created.Secret), storedHash);
		Assert.NotEqual(created.Secret, storedHash);
	}
	
	[Fact]
	public async Task CreateWithSecretAsync_DoesNotPassThePlainSecretToEvents()
	{
		var applicationService = this.CreateApplicationService();
		
		var created = await this.CreateWithSecretAsync(applicationService);
		
		var eventCall = this._eventService.ReceivedCalls().Single(x => x.GetArguments().Contains(ErtisAuthEventType.ApplicationCreated));
		Assert.DoesNotContain(eventCall.GetArguments(), x => x is ApplicationWithSecret);
		Assert.DoesNotContain(eventCall.GetArguments(), x => x is string value && value == created.Secret);
	}
	
	[Fact]
	public async Task CreateWithSecretAsync_ValidatesRoleWithoutBlockingCall()
	{
		await this.CreateWithSecretAsync(this.CreateApplicationService());
		
		await this._roleService.Received().GetBySlugAsync("server", this._membership.Id, Arg.Any<CancellationToken>());
		this._roleService.DidNotReceiveWithAnyArgs().GetBySlug(default!, default!);
	}
	
	[Fact]
	public async Task CreateWithSecretAsync_WithUnknownRole_IsRejected()
	{
		var model = this.NewApplicationModel();
		model.Role = "unknown-role";
		
		var exception = await Assert.ThrowsAsync<ValidationException>(() => this.CreateApplicationService().CreateWithSecretAsync(this._utilizer, this._membership.Id, model, TestContext.Current.CancellationToken));
		
		Assert.NotNull(exception.Errors);
		Assert.Contains("Role is invalid. There is no role named 'unknown-role'", exception.Errors);
	}
	
	#endregion
	
	#region Rotate
	
	[Fact]
	public async Task RotateSecretAsync_ReplacesHashWithNewSecret()
	{
		var applicationService = this.CreateApplicationService();
		var created = await this.CreateWithSecretAsync(applicationService);
		
		var rotated = await applicationService.RotateSecretAsync(this._utilizer, this._membership.Id, created.Id, TestContext.Current.CancellationToken);
		
		Assert.NotEqual(created.Secret, rotated.Secret);
		var storedHash = this.StoredSecretHash(created.Id);
		Assert.True(ApplicationSecretHelper.VerifySecret(rotated.Secret, storedHash));
		Assert.False(ApplicationSecretHelper.VerifySecret(created.Secret, storedHash));
	}
	
	[Fact]
	public async Task RotateSecretAsync_KeepsOtherApplicationFields()
	{
		var applicationService = this.CreateApplicationService();
		var created = await this.CreateWithSecretAsync(applicationService);
		
		var rotated = await applicationService.RotateSecretAsync(this._utilizer, this._membership.Id, created.Id, TestContext.Current.CancellationToken);
		
		Assert.Equal(created.Id, rotated.Id);
		Assert.Equal(created.Name, rotated.Name);
		Assert.Equal(created.Slug, rotated.Slug);
		Assert.Equal(created.Role, rotated.Role);
		Assert.Equal(created.MembershipId, rotated.MembershipId);
	}
	
	[Fact]
	public async Task RotateSecretAsync_InvalidatesCachedApplicationUsedByBasicTokenVerification()
	{
		var applicationService = this.CreateApplicationService();
		var created = await this.CreateWithSecretAsync(applicationService);
		var cached = await applicationService.GetByIdAsync(created.Id, TestContext.Current.CancellationToken);
		Assert.True(ApplicationSecretHelper.VerifySecret(created.Secret, cached?.SecretHash));
		
		var rotated = await applicationService.RotateSecretAsync(this._utilizer, this._membership.Id, created.Id, TestContext.Current.CancellationToken);
		
		var current = await applicationService.GetByIdAsync(created.Id, TestContext.Current.CancellationToken);
		Assert.True(ApplicationSecretHelper.VerifySecret(rotated.Secret, current?.SecretHash));
		Assert.False(ApplicationSecretHelper.VerifySecret(created.Secret, current?.SecretHash));
	}
	
	[Fact]
	public async Task RotateSecretAsync_FiresApplicationUpdatedEventWithoutPlainSecret()
	{
		var applicationService = this.CreateApplicationService();
		var created = await this.CreateWithSecretAsync(applicationService);
		
		var rotated = await applicationService.RotateSecretAsync(this._utilizer, this._membership.Id, created.Id, TestContext.Current.CancellationToken);
		
		var eventCall = this._eventService.ReceivedCalls().Single(x => x.GetArguments().Contains(ErtisAuthEventType.ApplicationUpdated));
		Assert.DoesNotContain(eventCall.GetArguments(), x => x is ApplicationWithSecret);
		Assert.DoesNotContain(eventCall.GetArguments(), x => x is string value && value == rotated.Secret);
	}
	
	[Fact]
	public async Task RotateSecretAsync_WithApplicationOfAnotherMembership_ThrowsApplicationNotFound()
	{
		var applicationService = this.CreateApplicationService();
		var created = await this.CreateWithSecretAsync(applicationService);
		this._applications.Single(x => x.Id == created.Id).MembershipId = "another-membership-id";
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => applicationService.RotateSecretAsync(this._utilizer, this._membership.Id, created.Id, TestContext.Current.CancellationToken));
		
		Assert.Equal("ApplicationNotFound", exception.ErrorCode);
	}
	
	#endregion
	
	#region Update
	
	[Fact]
	public async Task UpdateAsync_KeepsStoredSecretHash()
	{
		var applicationService = this.CreateApplicationService();
		var created = await this.CreateWithSecretAsync(applicationService);
		var storedHash = this.StoredSecretHash(created.Id);
		
		var update = this.NewApplicationModel("Renamed App");
		update.Id = created.Id;
		update.SecretHash = ApplicationSecretHelper.HashSecret("attacker-chosen-secret");
		await applicationService.UpdateAsync(this._utilizer, this._membership.Id, update, TestContext.Current.CancellationToken);
		
		Assert.Equal("Renamed App", this._applications.Single(x => x.Id == created.Id).Name);
		Assert.Equal(storedHash, this.StoredSecretHash(created.Id));
	}
	
	[Fact]
	public async Task UpdateAsync_WithoutChanges_IsStillReportedAsIdentical()
	{
		var applicationService = this.CreateApplicationService();
		var created = await this.CreateWithSecretAsync(applicationService);
		
		var update = this.NewApplicationModel();
		update.Id = created.Id;
		
		var exception = await Assert.ThrowsAsync<ValidationException>(() => applicationService.UpdateAsync(this._utilizer, this._membership.Id, update, TestContext.Current.CancellationToken));
		Assert.Equal("IdenticalDocumentError", exception.ErrorCode);
	}
	
	#endregion
	
	#region Delete
	
	[Fact]
	public async Task DeleteAsync_InvalidatesCachedApplicationUsedByBasicTokenVerification()
	{
		var applicationService = this.CreateApplicationService();
		var created = await this.CreateWithSecretAsync(applicationService);
		Assert.NotNull(await applicationService.GetByIdAsync(created.Id, TestContext.Current.CancellationToken));
		this._repository
			.DeleteAsync(created.Id, Arg.Any<CancellationToken>())
			.Returns(_ => this._applications.RemoveAll(x => x.Id == created.Id) > 0);
		
		await applicationService.DeleteAsync(this._utilizer, this._membership.Id, created.Id, TestContext.Current.CancellationToken);
		
		Assert.Null(await applicationService.GetByIdAsync(created.Id, TestContext.Current.CancellationToken));
	}
	
	[Fact]
	public async Task BulkDeleteAsync_InvalidatesCachedApplicationsUsedByBasicTokenVerification()
	{
		var applicationService = this.CreateApplicationService();
		var created = await this.CreateWithSecretAsync(applicationService);
		Assert.NotNull(await applicationService.GetByIdAsync(created.Id, TestContext.Current.CancellationToken));
		this._repository
			.DeleteAsync(created.Id, Arg.Any<CancellationToken>())
			.Returns(_ => this._applications.RemoveAll(x => x.Id == created.Id) > 0);
		
		await applicationService.BulkDeleteAsync(this._utilizer, this._membership.Id, [created.Id], TestContext.Current.CancellationToken);
		
		Assert.Null(await applicationService.GetByIdAsync(created.Id, TestContext.Current.CancellationToken));
	}
	
	#endregion
	
	#region Query
	
	private async Task<IDictionary<string, bool>?> QuerySelectFieldsAsync(ApplicationService applicationService, IDictionary<string, bool>? selectFields)
	{
		await applicationService.QueryAsync(this._membership.Id, "{}", selectFields: selectFields, cancellationToken: TestContext.Current.CancellationToken);
		var queryCall = this._repository.ReceivedCalls().Last(x => x.GetMethodInfo().Name == nameof(IApplicationRepository.QueryAsync));
		return queryCall.GetArguments().OfType<IDictionary<string, bool>>().SingleOrDefault();
	}
	
	[Fact]
	public async Task QueryAsync_WithoutSelectFields_ExcludesSecretHash()
	{
		var selectFields = await this.QuerySelectFieldsAsync(this.CreateApplicationService(), null);
		
		Assert.NotNull(selectFields);
		Assert.False(selectFields["secret_hash"]);
	}
	
	[Fact]
	public async Task QueryAsync_WithExclusionSelectFields_AlsoExcludesSecretHash()
	{
		var selectFields = await this.QuerySelectFieldsAsync(this.CreateApplicationService(), new Dictionary<string, bool> { { "role", false } });
		
		Assert.NotNull(selectFields);
		Assert.False(selectFields["role"]);
		Assert.False(selectFields["secret_hash"]);
	}
	
	[Fact]
	public async Task QueryAsync_WithInclusionSelectFields_DropsSecretHashFromIncludedFields()
	{
		var selectFields = await this.QuerySelectFieldsAsync(this.CreateApplicationService(), new Dictionary<string, bool>
		{
			{ "name", true },
			{ "secret_hash", true },
			{ "secret_hash.value", true },
			{ "_id", false }
		});
		
		Assert.NotNull(selectFields);
		Assert.True(selectFields["name"]);
		Assert.False(selectFields["_id"]);
		Assert.DoesNotContain(selectFields.Keys, x => x.StartsWith("secret_hash"));
	}
	
	#endregion
}
