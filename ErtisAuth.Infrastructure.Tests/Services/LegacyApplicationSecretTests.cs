using Ertis.Data.Models;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Infrastructure.Tests.Services;

// LEGACY-APP-SECRET: remove this file together with the legacy switch after all applications are migrated
/// <summary>
/// Temporary switch letting applications without their own secret authenticate with the membership secret key.
/// </summary>
public class LegacyApplicationSecretTests
{
	#region Constants
	
	private const string ApplicationId = "6a7b8c9d0e1f2a3b4c5d6e7f";
	
	#endregion
	
	#region Fields
	
	private readonly IMembershipService _membershipService = Substitute.For<IMembershipService>();
	
	private readonly IApplicationService _applicationService = Substitute.For<IApplicationService>();
	
	private readonly Membership _membership;
	
	#endregion
	
	#region Constructors
	
	public LegacyApplicationSecretTests()
	{
		this._membership = TestServiceFactory.CreateMembership("SHA2-256");
		this._membershipService.GetAsync(this._membership.Id, Arg.Any<CancellationToken>()).Returns(this._membership);
		
		// An application created before application secrets existed
		this._applicationService.GetByIdAsync(ApplicationId, Arg.Any<CancellationToken>()).Returns(new Application
		{
			Id = ApplicationId,
			Name = "legacy-app",
			Role = "server",
			MembershipId = this._membership.Id
		});
	}
	
	#endregion
	
	#region Helpers
	
	private TokenService CreateTokenService()
	{
		return new TokenService(
			this._membershipService,
			Substitute.For<IUserService>(),
			this._applicationService,
			Substitute.For<IRoleService>(),
			new JwtService(),
			Substitute.For<IEventService>(),
			Substitute.For<IActiveTokenService>(),
			Substitute.For<IRevokedTokenService>(),
			TestServiceFactory.CreateLegacyApplicationSecretVerifier(),
			NullLogger<TokenService>.Instance);
	}
	
	private async Task<BasicTokenValidationResultOrError> VerifyAsync(string secret)
	{
		try
		{
			var result = await this.CreateTokenService().VerifyBasicTokenAsync($"{ApplicationId}:{secret}", false, cancellationToken: TestContext.Current.CancellationToken);
			return new BasicTokenValidationResultOrError(result.IsValidated, null);
		}
		catch (ErtisAuthException ex)
		{
			return new BasicTokenValidationResultOrError(false, ex.ErrorCode);
		}
	}
	
	private record BasicTokenValidationResultOrError(bool IsValidated, string? ErrorCode);
	
	#endregion
	
	#region Token Verification
	
	[Theory]
	[InlineData(null)]
	[InlineData(true)]
	public async Task VerifyBasicTokenAsync_WhenSwitchIsMissingOrOn_AcceptsMembershipSecretForApplicationWithoutOwnSecret(bool? allowMembershipSecret)
	{
		this._membership.AllowMembershipSecretForApplications = allowMembershipSecret;
		
		var result = await this.VerifyAsync(this._membership.SecretKey);
		
		Assert.True(result.IsValidated);
	}
	
	[Fact]
	public async Task VerifyBasicTokenAsync_WhenSwitchIsOff_RejectsMembershipSecret()
	{
		this._membership.AllowMembershipSecretForApplications = false;
		
		var result = await this.VerifyAsync(this._membership.SecretKey);
		
		Assert.False(result.IsValidated);
		Assert.Equal("InvalidToken", result.ErrorCode);
	}
	
	[Fact]
	public async Task VerifyBasicTokenAsync_WhenSwitchIsOn_RejectsWrongSecret()
	{
		this._membership.AllowMembershipSecretForApplications = true;
		
		var result = await this.VerifyAsync("wrong-secret");
		
		Assert.False(result.IsValidated);
		Assert.Equal("InvalidToken", result.ErrorCode);
	}
	
	#endregion
	
	#region Membership Switch
	
	private readonly IMembershipRepository _membershipRepository = Substitute.For<IMembershipRepository>();
	
	private MembershipService CreateMembershipService()
	{
		return new MembershipService(this._membershipRepository, new MemoryCache(new MemoryCacheOptions()));
	}
	
	[Theory]
	[InlineData(null)]
	[InlineData(true)]
	[InlineData(false)]
	public async Task CreateAsync_NewMembership_StartsWithSwitchOff(bool? requested)
	{
		var membership = TestServiceFactory.CreateMembership("SHA2-256");
		membership.AllowMembershipSecretForApplications = requested;
		
		await this.CreateMembershipService().CreateAsync(membership, Utilizer.GetSystemUtilizer(string.Empty), TestContext.Current.CancellationToken);
		
		Assert.False(membership.AllowMembershipSecretForApplications);
		await this._membershipRepository.Received(1).InsertAsync(membership, Arg.Any<InsertOptions?>(), Arg.Any<CancellationToken>());
	}
	
	[Theory]
	[InlineData(null)]
	[InlineData(true)]
	[InlineData(false)]
	public async Task UpdateAsync_WithoutSwitch_KeepsStoredValue(bool? stored)
	{
		var current = TestServiceFactory.CreateMembership("SHA2-256");
		current.AllowMembershipSecretForApplications = stored;
		this._membershipRepository.FindOneAsync(current.Id, Arg.Any<CancellationToken>()).Returns(current);
		var update = TestServiceFactory.CreateMembership("SHA2-256");
		update.Name = "Renamed Membership";
		
		await this.CreateMembershipService().UpdateAsync(update, Utilizer.GetSystemUtilizer(string.Empty), TestContext.Current.CancellationToken);
		
		Assert.Equal(stored, update.AllowMembershipSecretForApplications);
	}
	
	[Fact]
	public async Task UpdateAsync_CanTurnSwitchOff()
	{
		var current = TestServiceFactory.CreateMembership("SHA2-256");
		this._membershipRepository.FindOneAsync(current.Id, Arg.Any<CancellationToken>()).Returns(current);
		var update = TestServiceFactory.CreateMembership("SHA2-256");
		update.AllowMembershipSecretForApplications = false;
		
		await this.CreateMembershipService().UpdateAsync(update, Utilizer.GetSystemUtilizer(string.Empty), TestContext.Current.CancellationToken);
		
		Assert.False(update.AllowMembershipSecretForApplications);
	}
	
	#endregion
}
