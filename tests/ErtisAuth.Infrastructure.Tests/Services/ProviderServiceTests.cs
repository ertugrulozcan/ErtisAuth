using System.Text.Json;
using Ertis.Core.Exceptions;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.Infrastructure.Tests.Helpers;
using ErtisAuth.Integrations.OAuth;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// ProviderService create/update: the validation and the merge with the stored provider, per provider type.
/// </summary>
public class ProviderServiceTests
{
	#region Constants
	
	private const string MembershipId = "5f8a1b2c3d4e5f6a7b8c9d00";
	private const string ProviderId = "5f8a1b2c3d4e5f6a7b8c9d10";
	
	#endregion
	
	#region Fields
	
	private readonly IMembershipService _membershipService = Substitute.For<IMembershipService>();
	
	private readonly IProviderRepository _repository = Substitute.For<IProviderRepository>();
	
	private readonly IEventService _eventService = Substitute.For<IEventService>();
	
	private readonly List<Provider> _providers;
	
	#endregion
	
	#region Constructors
	
	public ProviderServiceTests()
	{
		// ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
		this._providers = InMemoryRepository.Setup(this._repository, x => x.Id ??= Guid.NewGuid().ToString("N")[..24]);
		this._membershipService.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(callInfo => CreateMembership(callInfo.ArgAt<string>(0)));
	}
	
	#endregion
	
	#region Helpers
	
	private static Membership CreateMembership(string id)
	{
		var membership = TestServiceFactory.CreateMembership();
		membership.Id = id;
		return membership;
	}
	
	private ProviderService CreateService()
	{
		return new ProviderService(
			this._membershipService,
			Substitute.For<IUserService>(),
			Substitute.For<IUserTypeService>(),
			Substitute.For<ITokenService>(),
			this._eventService,
			Substitute.For<IAuthenticatorFactory>(),
			new MemoryCache(new MemoryCacheOptions()),
			this._repository,
			NullLogger<ProviderService>.Instance);
	}
	
	private static Utilizer Utilizer => Utilizer.GetSystemUtilizer(MembershipId);
	
	private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;
	
	/// <summary>
	/// An active provider of the given type with every setting its type requires.
	/// </summary>
	private static Provider CreateConfiguredProvider(string type)
	{
		Provider provider = type switch
		{
			nameof(AppleProvider) => new AppleProvider { MembershipId = MembershipId },
			nameof(AppleNativeProvider) => new AppleNativeProvider { MembershipId = MembershipId },
			nameof(FacebookProvider) => new FacebookProvider { MembershipId = MembershipId, AppClientId = "facebook-app-id" },
			nameof(GoogleProvider) => new GoogleProvider { MembershipId = MembershipId, AppClientId = "google-client-id" },
			nameof(MicrosoftProvider) => new MicrosoftProvider { MembershipId = MembershipId, AppClientId = "microsoft-client-id", TenantId = "tenant-id" },
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
		};
		
		if (provider is BaseAppleProvider appleProvider)
		{
			appleProvider.AppClientId = "com.example.app";
			appleProvider.TeamId = "TEAM123456";
			appleProvider.PrivateKey = "-----BEGIN PRIVATE KEY-----stored";
			appleProvider.PrivateKeyId = "KEY1234567";
			appleProvider.RedirectUri = "https://app.example.com/apple/callback";
		}
		
		provider.IsActive = true;
		provider.DefaultRole = "user";
		provider.DefaultUserType = "base-user";
		return provider;
	}
	
	private Provider AddConfiguredProvider(string type)
	{
		var provider = CreateConfiguredProvider(type);
		provider.Id = ProviderId;
		this._providers.Add(provider);
		return provider;
	}
	
	/// <summary>
	/// An update of the stored provider as the controller builds it when only the type's own fields are sent.
	/// </summary>
	private static Provider CreateUpdate(Provider stored, Action<Provider> change)
	{
		var update = (Provider) Activator.CreateInstance(stored.GetType())!;
		update.Id = stored.Id;
		update.MembershipId = stored.MembershipId;
		update.Name = stored.Name;
		update.Slug = stored.Slug;
		update.IsActive = stored.IsActive;
		update.TrustEmail = stored.TrustEmail;
		change(update);
		return update;
	}
	
	private static string? AppClientIdOf(Provider provider)
	{
		return provider switch
		{
			BaseAppleProvider appleProvider => appleProvider.AppClientId,
			FacebookProvider facebookProvider => facebookProvider.AppClientId,
			GoogleProvider googleProvider => googleProvider.AppClientId,
			MicrosoftProvider microsoftProvider => microsoftProvider.AppClientId,
			_ => null
		};
	}
	
	private static void SetAppClientId(Provider provider, string? appClientId)
	{
		switch (provider)
		{
			case BaseAppleProvider appleProvider:
				appleProvider.AppClientId = appClientId;
				break;
			case FacebookProvider facebookProvider:
				facebookProvider.AppClientId = appClientId;
				break;
			case GoogleProvider googleProvider:
				googleProvider.AppClientId = appClientId;
				break;
			case MicrosoftProvider microsoftProvider:
				microsoftProvider.AppClientId = appClientId;
				break;
		}
	}
	
	public static TheoryData<string> ProviderTypes =>
	[
		nameof(AppleProvider),
		nameof(AppleNativeProvider),
		nameof(FacebookProvider),
		nameof(GoogleProvider),
		nameof(MicrosoftProvider)
	];
	
	#endregion
	
	#region Create
	
	[Theory]
	[MemberData(nameof(ProviderTypes))]
	public async Task CreateAsync_WithEveryRequiredSetting_CreatesTheProvider(string type)
	{
		var provider = CreateConfiguredProvider(type);
		
		var created = await this.CreateService().CreateAsync(provider, MembershipId, Utilizer, CancellationToken);
		
		Assert.IsType(provider.GetType(), Assert.Single(this._providers));
		Assert.Equal(provider.Type.ToString(), created.Name);
		Assert.Equal(provider.Type.ToString().ToLowerInvariant(), created.Slug);
	}
	
	[Theory]
	[MemberData(nameof(ProviderTypes))]
	public async Task CreateAsync_InactiveProvider_NeedsNoSettings(string type)
	{
		var provider = (Provider) Activator.CreateInstance(CreateConfiguredProvider(type).GetType())!;
		provider.MembershipId = MembershipId;
		
		await this.CreateService().CreateAsync(provider, MembershipId, Utilizer, CancellationToken);
		
		Assert.Single(this._providers);
	}
	
	[Theory]
	[MemberData(nameof(ProviderTypes))]
	public async Task CreateAsync_ActiveProviderWithoutAppClientId_IsRejected(string type)
	{
		var provider = CreateConfiguredProvider(type);
		SetAppClientId(provider, null);
		
		var exception = await Assert.ThrowsAsync<ValidationException>(() => this.CreateService().CreateAsync(provider, MembershipId, Utilizer, CancellationToken));
		
		Assert.Equal("ModelValidationError", exception.ErrorCode);
		Assert.Contains("App client id is required", exception.Errors!);
		Assert.Empty(this._providers);
	}
	
	[Theory]
	[InlineData(nameof(AppleProvider), "Team id is required")]
	[InlineData(nameof(AppleProvider), "Private key is required")]
	[InlineData(nameof(AppleProvider), "Private key id is required")]
	[InlineData(nameof(AppleProvider), "Redirect uri is required")]
	[InlineData(nameof(AppleNativeProvider), "Team id is required")]
	[InlineData(nameof(AppleNativeProvider), "Private key is required")]
	[InlineData(nameof(AppleNativeProvider), "Private key id is required")]
	[InlineData(nameof(AppleNativeProvider), "Redirect uri is required")]
	public async Task CreateAsync_ActiveAppleProviderWithoutAnAppleSetting_IsRejected(string type, string expectedError)
	{
		var provider = (BaseAppleProvider) CreateConfiguredProvider(type);
		switch (expectedError)
		{
			case "Team id is required":
				provider.TeamId = null;
				break;
			case "Private key is required":
				provider.PrivateKey = null;
				break;
			case "Private key id is required":
				provider.PrivateKeyId = null;
				break;
			case "Redirect uri is required":
				provider.RedirectUri = null;
				break;
		}
		
		var exception = await Assert.ThrowsAsync<ValidationException>(() => this.CreateService().CreateAsync(provider, MembershipId, Utilizer, CancellationToken));
		
		Assert.Equal([expectedError], exception.Errors);
	}
	
	[Fact]
	public async Task CreateAsync_WithTheSlugOfAnotherProvider_IsRejected()
	{
		this.AddConfiguredProvider(nameof(GoogleProvider));
		var provider = CreateConfiguredProvider(nameof(FacebookProvider));
		provider.Slug = "google";
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.CreateService().CreateAsync(provider, MembershipId, Utilizer, CancellationToken));
		
		Assert.Equal("ProviderAlreadyExists", exception.ErrorCode);
	}
	
	#endregion
	
	#region Update
	
	[Theory]
	[MemberData(nameof(ProviderTypes))]
	public async Task UpdateAsync_ChangesTheAppClientId(string type)
	{
		// Regression: Overwrite assigned the stored app client id over the incoming one, so it could never be changed
		var stored = this.AddConfiguredProvider(type);
		var update = CreateUpdate(stored, x => SetAppClientId(x, "changed-client-id"));
		
		var updated = await this.CreateService().UpdateAsync(update, MembershipId, Utilizer, CancellationToken);
		
		Assert.Equal("changed-client-id", AppClientIdOf(updated));
		Assert.Equal("changed-client-id", AppClientIdOf(Assert.Single(this._providers)));
	}
	
	[Theory]
	[MemberData(nameof(ProviderTypes))]
	public async Task UpdateAsync_OmittedSettingsKeepTheirValues(string type)
	{
		var stored = this.AddConfiguredProvider(type);
		var appClientId = AppClientIdOf(stored);
		var update = CreateUpdate(stored, x => x.Description = "changed");
		
		var updated = await this.CreateService().UpdateAsync(update, MembershipId, Utilizer, CancellationToken);
		
		Assert.Equal("changed", updated.Description);
		Assert.Equal(appClientId, AppClientIdOf(updated));
		Assert.Equal("user", updated.DefaultRole);
		Assert.Equal("base-user", updated.DefaultUserType);
		if (updated is BaseAppleProvider appleProvider)
		{
			Assert.Equal("TEAM123456", appleProvider.TeamId);
			Assert.Equal("-----BEGIN PRIVATE KEY-----stored", appleProvider.PrivateKey);
			Assert.Equal("KEY1234567", appleProvider.PrivateKeyId);
			Assert.Equal("https://app.example.com/apple/callback", appleProvider.RedirectUri);
		}
		else if (updated is MicrosoftProvider microsoftProvider)
		{
			Assert.Equal("tenant-id", microsoftProvider.TenantId);
		}
	}
	
	[Fact]
	public async Task UpdateAsync_ChangesTheMicrosoftTenant()
	{
		var stored = this.AddConfiguredProvider(nameof(MicrosoftProvider));
		var update = CreateUpdate(stored, x => ((MicrosoftProvider) x).TenantId = "another-tenant");
		
		var updated = await this.CreateService().UpdateAsync(update, MembershipId, Utilizer, CancellationToken);
		
		Assert.Equal("another-tenant", ((MicrosoftProvider) updated).TenantId);
	}
	
	[Fact]
	public async Task UpdateAsync_WithOnlyATypeSpecificChange_IsNotAnIdenticalDocument()
	{
		// Regression: IsIdentical compared only the properties of Provider, so a change of a subclass field answered 409
		var stored = this.AddConfiguredProvider(nameof(AppleNativeProvider));
		var update = (AppleNativeProvider) CreateUpdate(stored, x =>
		{
			x.DefaultRole = stored.DefaultRole;
			x.DefaultUserType = stored.DefaultUserType;
		});
		
		update.AppClientId = "com.example.other";
		update.TeamId = ((BaseAppleProvider) stored).TeamId;
		update.PrivateKey = ((BaseAppleProvider) stored).PrivateKey;
		update.PrivateKeyId = ((BaseAppleProvider) stored).PrivateKeyId;
		update.RedirectUri = ((BaseAppleProvider) stored).RedirectUri;
		
		var updated = await this.CreateService().UpdateAsync(update, MembershipId, Utilizer, CancellationToken);
		
		Assert.Equal("com.example.other", ((AppleNativeProvider) updated).AppClientId);
	}
	
	[Fact]
	public async Task UpdateAsync_WithAnotherSlug_ThrowsProviderSlugCannotBeChanged()
	{
		var stored = this.AddConfiguredProvider(nameof(GoogleProvider));
		var update = CreateUpdate(stored, x => x.Slug = "another-slug");
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.CreateService().UpdateAsync(update, MembershipId, Utilizer, CancellationToken));
		
		Assert.Equal("ProviderSlugCannotBeChanged", exception.ErrorCode);
		Assert.Equal("google", Assert.Single(this._providers).Slug);
	}
	
	[Fact]
	public async Task UpdateAsync_WithoutAnyChange_ThrowsIdenticalDocument()
	{
		this.AddConfiguredProvider(nameof(MicrosoftProvider));
		var update = CreateConfiguredProvider(nameof(MicrosoftProvider));
		update.Id = ProviderId;
		
		var exception = await Assert.ThrowsAsync<ValidationException>(() => this.CreateService().UpdateAsync(update, MembershipId, Utilizer, CancellationToken));
		
		Assert.Equal("IdenticalDocumentError", exception.ErrorCode);
	}
	
	[Theory]
	[InlineData(false, true)]
	[InlineData(true, false)]
	public async Task UpdateAsync_ChangesTheActivation(bool isActive, bool newIsActive)
	{
		// Regression: Overwrite copied the stored IsActive over the incoming one, so a provider could never be (de)activated
		var stored = this.AddConfiguredProvider(nameof(FacebookProvider));
		stored.IsActive = isActive;
		var update = CreateUpdate(stored, x => x.IsActive = newIsActive);
		
		var updated = await this.CreateService().UpdateAsync(update, MembershipId, Utilizer, CancellationToken);
		
		Assert.Equal(newIsActive, updated.IsActive);
		Assert.Equal(newIsActive, Assert.Single(this._providers).IsActive);
	}
	
	#endregion
	
	#region Events
	
	[Theory]
	[InlineData(nameof(AppleProvider))]
	[InlineData(nameof(AppleNativeProvider))]
	public async Task UpdateAsync_EventHasTheTypeAndSettingsButNotThePrivateKey(string type)
	{
		// Events are readable (events.read) and forwarded to webhooks: the type's settings, but not the Apple signing key
		var stored = this.AddConfiguredProvider(type);
		object? document = null;
		object? prior = null;
		this._eventService
			.When(x => x.FireEventAsync(ErtisAuthEventType.ProviderUpdated, Arg.Any<Utilizer>(), Arg.Any<string?>(), Arg.Any<object?>(), Arg.Any<object?>(), Arg.Any<CancellationToken>()))
			.Do(x =>
			{
				document = x.ArgAt<object?>(3);
				prior = x.ArgAt<object?>(4);
			});
		
		var update = CreateUpdate(stored, x =>
		{
			SetAppClientId(x, "new-app-id");
			((BaseAppleProvider) x).PrivateKey = "-----BEGIN PRIVATE KEY-----updated";
		});
		
		await this.CreateService().UpdateAsync(update, MembershipId, Utilizer, CancellationToken);
		
		var json = JsonSerializer.Serialize(new { document, prior });
		Assert.DoesNotContain("BEGIN PRIVATE KEY", json);
		Assert.Contains("\"appClientId\":\"new-app-id\"", json);
		Assert.Contains("\"teamId\":\"TEAM123456\"", json);
		Assert.Contains($"\"type\":\"{stored.Type}\"", json);
	}
	
	#endregion
}
