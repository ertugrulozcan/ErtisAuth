using System.Text.Json;
using System.Text.Json.Nodes;
using Ertis.MongoDB.Queries;
using Ertis.Schema.Dynamics;
using Ertis.Schema.Types;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Core.Events;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Extensions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Constants;
using ErtisAuth.Integrations.OAuth.Core;
using ErtisAuth.Integrations.OAuth;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Infrastructure.Services;

public class ProviderService : MembershipBoundedCrudService<Provider>, IProviderService
{
	#region Constants
	
	private const string CACHE_KEY = "providers";
	
	#endregion
	
	#region Services
	
	private readonly IUserService _userService;
	private readonly IUserTypeService _userTypeService;
	private readonly ITokenService _tokenService;
	private readonly IEventService _eventService;
	private readonly IAuthenticatorFactory _authenticatorFactory;
	private readonly IMemoryCache _memoryCache;
	private readonly ILogger<ProviderService> _logger;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="membershipService"></param>
	/// <param name="userService"></param>
	/// <param name="userTypeService"></param>
	/// <param name="tokenService"></param>
	/// <param name="eventService"></param>
	/// <param name="authenticatorFactory"></param>
	/// <param name="memoryCache"></param>
	/// <param name="repository"></param>
	/// <param name="logger"></param>
	public ProviderService(
		IMembershipService membershipService,
		IUserService userService,
		IUserTypeService userTypeService,
		ITokenService tokenService,
		IEventService eventService,
		IAuthenticatorFactory authenticatorFactory,
		IMemoryCache memoryCache,
		IProviderRepository repository, 
		ILogger<ProviderService> logger) : base(membershipService, repository)
	{
		this._userService = userService;
		this._userTypeService = userTypeService;
		this._tokenService = tokenService;
		this._eventService = eventService;
		this._authenticatorFactory = authenticatorFactory;
		this._memoryCache = memoryCache;
		this._logger = logger;
		
		this.OnCreated += this.ProviderCreatedEventHandler;
		this.OnUpdated += this.ProviderUpdatedEventHandler;
		this.OnDeleted += this.ProviderDeletedEventHandler;
	}
	
	#endregion
	
	#region Event Handlers
	
	private async void ProviderCreatedEventHandler(object? sender, CreateResourceEventArgs<Provider> eventArgs)
	{
		try
		{
			await this._eventService.FireEventAsync(ErtisAuthEventType.ProviderCreated, eventArgs.Utilizer, eventArgs.MembershipId, ToEventDocument(eventArgs.Resource));
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "ProviderService.ProviderCreatedEventHandler occured an error");
		}
	}
	
	private async void ProviderUpdatedEventHandler(object? sender, UpdateResourceEventArgs<Provider> eventArgs)
	{
		try
		{
			await this._eventService.FireEventAsync(ErtisAuthEventType.ProviderUpdated, eventArgs.Utilizer, eventArgs.MembershipId, ToEventDocument(eventArgs.Updated), ToEventDocument(eventArgs.Prior));
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "ProviderService.ProviderUpdatedEventHandler occured an error");
		}
	}
	
	private async void ProviderDeletedEventHandler(object? sender, DeleteResourceEventArgs<Provider> eventArgs)
	{
		try
		{
			await this._eventService.FireEventAsync(ErtisAuthEventType.ProviderDeleted, eventArgs.Utilizer, eventArgs.MembershipId, null, ToEventDocument(eventArgs.Resource));
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "ProviderService.ProviderDeletedEventHandler occured an error");
		}
	}
	
	/// <summary>
	/// Events are readable (events.read) and forwarded to webhooks: the provider without its private key (Apple).
	/// </summary>
	private static JsonObject? ToEventDocument(Provider? provider)
	{
		if (provider == null)
		{
			return null;
		}
		
		var document = JsonSerializer.SerializeToNode(provider)?.AsObject();
		document?.Remove("privateKey");
		return document;
	}
	
	#endregion
	
	#region Methods
	
	protected override Task<IEnumerable<string>> ValidateModelAsync(Provider model, CancellationToken cancellationToken = default)
	{
		var errorList = new List<string>();
		if (string.IsNullOrEmpty(model.Name))
		{
			errorList.Add("Name is required");
		}
		
		if (!model.Slug.IsValidSlug(out var error))
		{
			errorList.Add(error!);
		}
		
		if (string.IsNullOrEmpty(model.MembershipId))
		{
			errorList.Add("Membership id is required");
		}
		
		if (model.IsActive)
		{
			if (string.IsNullOrEmpty(model.DefaultRole))
			{
				errorList.Add("Default role is required");
			}
			
			if (string.IsNullOrEmpty(model.DefaultUserType))
			{
				errorList.Add("Default user type is required");
			}
			
			if (model is AppleProvider appleProvider)
			{
				if (string.IsNullOrEmpty(appleProvider.AppClientId))
				{
					errorList.Add("App client id is required");
				}
				
				if (string.IsNullOrEmpty(appleProvider.TeamId))
				{
					errorList.Add("Team id is required");
				}
				
				if (string.IsNullOrEmpty(appleProvider.PrivateKey))
				{
					errorList.Add("Private key is required");
				}
				
				if (string.IsNullOrEmpty(appleProvider.PrivateKeyId))
				{
					errorList.Add("Private key id is required");
				}
				
				if (string.IsNullOrEmpty(appleProvider.RedirectUri))
				{
					errorList.Add("Redirect uri is required");
				}
			}
			else if (model is AppleNativeProvider appleNativeProvider)
			{
				if (string.IsNullOrEmpty(appleNativeProvider.AppClientId))
				{
					errorList.Add("App client id is required");
				}
				
				if (string.IsNullOrEmpty(appleNativeProvider.TeamId))
				{
					errorList.Add("Team id is required");
				}
				
				if (string.IsNullOrEmpty(appleNativeProvider.PrivateKey))
				{
					errorList.Add("Private key is required");
				}
				
				if (string.IsNullOrEmpty(appleNativeProvider.PrivateKeyId))
				{
					errorList.Add("Private key id is required");
				}
				
				if (string.IsNullOrEmpty(appleNativeProvider.RedirectUri))
				{
					errorList.Add("Redirect uri is required");
				}
			}
			else if (model is FacebookProvider facebookProvider)
			{
				if (string.IsNullOrEmpty(facebookProvider.AppClientId))
				{
					errorList.Add("App client id is required");
				}
			}
			else if (model is GoogleProvider googleProvider)
			{
				if (string.IsNullOrEmpty(googleProvider.AppClientId))
				{
					errorList.Add("App client id is required");
				}
			}
			else if (model is MicrosoftProvider microsoftProvider)
			{
				if (string.IsNullOrEmpty(microsoftProvider.AppClientId))
				{
					errorList.Add("App client id is required");
				}
			}
		}
		
		return Task.FromResult<IEnumerable<string>>(errorList);
	}
	
	protected override void Overwrite(Provider destination, Provider source)
	{
		destination.Id = source.Id;
		destination.MembershipId = source.MembershipId;
		destination.Sys = source.Sys;
		
		if (this.IsIdentical(destination, source))
		{
			throw ErtisAuthException.IdenticalDocument();
		}
		
		destination.Description ??= source.Description;
		destination.DefaultRole ??= source.DefaultRole;
		destination.DefaultUserType ??= source.DefaultUserType;
		
		if (destination is AppleProvider appleDestination && source is AppleProvider appleSource)
		{
			appleDestination.AppClientId ??= appleSource.AppClientId;
			appleDestination.TeamId ??= appleSource.TeamId;
			appleDestination.PrivateKey ??= appleSource.PrivateKey;
			appleDestination.PrivateKeyId ??= appleSource.PrivateKeyId;
			appleDestination.RedirectUri ??= appleSource.RedirectUri;
		}
		else if (destination is AppleNativeProvider appleNativeDestination && source is AppleNativeProvider appleNativeSource)
		{
			appleNativeDestination.AppClientId = appleNativeSource.AppClientId;
			appleNativeDestination.TeamId ??= appleNativeSource.TeamId;
			appleNativeDestination.PrivateKey ??= appleNativeSource.PrivateKey;
			appleNativeDestination.PrivateKeyId ??= appleNativeSource.PrivateKeyId;
			appleNativeDestination.RedirectUri ??= appleNativeSource.RedirectUri;
		}
		else if (destination is FacebookProvider facebookDestination && source is FacebookProvider facebookSource)
		{
			facebookDestination.AppClientId = facebookSource.AppClientId;
		}
		else if (destination is GoogleProvider googleDestination && source is GoogleProvider googleSource)
		{
			googleDestination.AppClientId = googleSource.AppClientId;
		}
		else if (destination is MicrosoftProvider microsoftDestination && source is MicrosoftProvider microsoftSource)
		{
			microsoftDestination.AppClientId = microsoftSource.AppClientId;
		}
	}
	
	protected override async Task<bool> IsAlreadyExistAsync(Provider model, string membershipId, Provider? exclude = null, CancellationToken cancellationToken = default)
	{
		if (exclude == null)
		{
			return await this.GetBySlugAsync(model.Slug, membershipId, cancellationToken: cancellationToken) != null;	
		}
		else
		{
			var current = await this.GetBySlugAsync(model.Slug, membershipId, cancellationToken: cancellationToken);
			if (current != null)
			{
				return current.Id != exclude.Id;	
			}
			else
			{
				return false;
			}
		}
	}
	
	protected override ErtisAuthException GetAlreadyExistError(Provider model)
	{
		return ErtisAuthException.ProviderAlreadyExists(model.Slug);
	}
	
	protected override ErtisAuthException GetNotFoundError(string id)
	{
		return ErtisAuthException.ProviderNotFound(id);
	}
	
	#endregion
	
	#region Cache Methods
	
	private static string GetCacheKey(string membershipId)
	{
		return $"{CACHE_KEY}.{membershipId}";
	}
	
	private static MemoryCacheEntryOptions GetCacheTTL()
	{
		return new MemoryCacheEntryOptions().SetAbsoluteExpiration(CacheDefaults.ProvidersCacheTTL);
	}
	
	private void PurgeAllCache(string membershipId)
	{
		this._memoryCache.Remove(GetCacheKey(membershipId));
	}
	
	#endregion
	
	#region Read Methods
	
	public async Task<Provider?> GetByTypeAsync(ProviderType type, string membershipId, CancellationToken cancellationToken = default)
	{
		return await this._repository.FindOneAsync(x => x.Type == type && x.MembershipId == membershipId, cancellationToken: cancellationToken);
	}
	
	public async Task<Provider?> GetBySlugAsync(string slug, string membershipId, CancellationToken cancellationToken = default)
	{
		return await this._repository.FindOneAsync(x => x.Slug == slug && x.MembershipId == membershipId, cancellationToken: cancellationToken);
	}
	
	public async Task<IEnumerable<Provider>> GetProvidersAsync(string membershipId, CancellationToken cancellationToken = default)
	{
		var cacheKey = GetCacheKey(membershipId);
		if (this._memoryCache.TryGetValue<IEnumerable<Provider>>(cacheKey, out var cacheResults))
		{
			return cacheResults ?? Enumerable.Empty<Provider>();
		}
		
		var providers = await this.GetAsync(membershipId, null, null, cancellationToken: cancellationToken);
		this._memoryCache.Set(cacheKey, providers.Items, GetCacheTTL());
		return providers.Items;
	}
	
	#endregion
	
	#region Create Methods
	
	public override async Task<Provider> CreateAsync(Provider model, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default)
	{
		var created = await base.CreateAsync(model, membershipId, utilizer, cancellationToken);
		this.PurgeAllCache(membershipId);
		return created;
	}
	
	#endregion
	
	#region Update Methods
	
	public override async Task<Provider> UpdateAsync(Provider model, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default)
	{
		var updated = await base.UpdateAsync(model, membershipId, utilizer, cancellationToken);
		this.PurgeAllCache(membershipId);
		return updated;
	}
	
	#endregion
	
	#region Delete Methods
	
	public override async Task<bool> DeleteAsync(string id, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default)
	{
		var isDeleted = await base.DeleteAsync(id, membershipId, utilizer, cancellationToken);
		if (isDeleted)
		{
			this.PurgeAllCache(membershipId);	
		}
		
		return isDeleted;
	}
	
	#endregion
	
	#region Authentication Methods
	
	public async Task<BearerToken> LoginAsync(IProviderLoginRequest request, string membershipId, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default)
	{
		var provider = await this.GetByTypeAsync(request.Provider, membershipId, cancellationToken: cancellationToken);
		if (provider != null)
		{
			if (provider.IsActive)
			{
				var providerAuthenticator = this._authenticatorFactory.GetAuthenticator(provider);
				var isVerified = await providerAuthenticator.VerifyTokenAsync(request, provider, cancellationToken: cancellationToken);
				
				// The authenticator sets the identity from verified provider data; without it the login can't be matched safely
				if (isVerified && !string.IsNullOrEmpty(request.UserId))
				{
					var user = await this.FindUserAsync(request, provider, membershipId, cancellationToken: cancellationToken);
					var isNewUser = user == null;
					if (isNewUser)
					{
						user = request.ToUser(membershipId, provider.DefaultRole, provider.DefaultUserType) as User;
					}
					else if (user is not { IsActive: true })
					{
						throw ErtisAuthException.UserInactive(user?.Id ?? string.Empty);
					}
					
					var userType = await this._userTypeService.GetBySlugAsync((isNewUser ? provider.DefaultUserType : user?.UserType)!, membershipId, cancellationToken: cancellationToken);
					
					this.EnsureConnectedAccounts(user!, request, provider);
					var dynamicUser = new DynamicObject(user!);
					this.SetAvatar(dynamicUser, request, userType);
					
					var utilizer = Utilizer.GetSystemUtilizer(membershipId);
					var upsertedUser = isNewUser ?
						await this._userService.CreateAsync(dynamicUser, membershipId, utilizer, cancellationToken: cancellationToken) :
						await this._userService.UpdateAsync(dynamicUser, user!.Id, membershipId, utilizer, false, cancellationToken: cancellationToken);
					
					return await this._tokenService.GenerateTokenAsync(upsertedUser?.Deserialize<User>()!, membershipId, ipAddress, userAgent, cancellationToken: cancellationToken);
				}
				else
				{
					throw ErtisAuthException.Unauthorized("Token was not verified by provider");
				}
			}
			else
			{
				throw ErtisAuthException.ProviderIsDisable();
			}
		}
		else
		{
			throw ErtisAuthException.ProviderNotConfigured();
		}
	}
	
	public async Task LogoutAsync(string token, CancellationToken cancellationToken = default)
	{
		try
		{
			var user = await this._tokenService.GetTokenOwnerUserAsync(token, cancellationToken: cancellationToken);
			if (user is { ConnectedAccounts: not null })
			{
				var needUserUpdate = false;
				var connectedAccounts = new List<ProviderAccountInfo>();
				foreach (var accountInfo in user.ConnectedAccounts)
				{
					if (!string.IsNullOrEmpty(accountInfo.Token) && Enum.TryParse<ProviderType>(accountInfo.Provider, true, out var providerType))
					{
						var provider = await this.GetByTypeAsync(providerType, user.MembershipId, cancellationToken: cancellationToken);
						if (provider is { IsActive: true })
						{
							var providerAuthenticator = this._authenticatorFactory.GetAuthenticator(provider);
							await providerAuthenticator.RevokeTokenAsync(accountInfo.Token, provider, cancellationToken: cancellationToken);
							
							connectedAccounts.Add(new ProviderAccountInfo
							{
								Provider = accountInfo.Provider,
								UserId = accountInfo.UserId,
								Token = null
							});
							
							needUserUpdate = true;
						}
						else
						{
							connectedAccounts.Add(accountInfo);
						}
					}
					else
					{
						connectedAccounts.Add(accountInfo);
					}
				}
				
				if (needUserUpdate)
				{
					user.ConnectedAccounts = connectedAccounts.ToArray();
					var dynamicUser = new DynamicObject(user);
					
					var utilizer = Utilizer.GetSystemUtilizer(user.MembershipId);
					await this._userService.UpdateAsync(dynamicUser, user.Id, user.MembershipId, utilizer, false, cancellationToken: cancellationToken);	
				}
			}
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "ProviderService.LogoutAsync occured an error");
		}
	}
	
	private async Task<User?> FindUserAsync(IProviderLoginRequest request, Provider provider, string membershipId, CancellationToken cancellationToken = default)
	{
		// The provider and its user id must match in the same connected account (not in two different array elements)
		var query = QueryBuilder.Where(
			QueryBuilder.Equals("membership_id", membershipId), 
			QueryBuilder.ElemMatch(
				"connected_accounts",
				QueryBuilder.Equals("Provider", provider.Type.ToString()),
				QueryBuilder.Equals("UserId", request.UserId))).ToString();
		
		var queryUsersResult = await this._userService.QueryAsync(query, membershipId, 0, 1, cancellationToken: cancellationToken);
		if (queryUsersResult.Items.Any())
		{
			var dynamicUser = queryUsersResult.Items.First();
			return dynamicUser.Deserialize<User>();
		}
		
		if (string.IsNullOrEmpty(request.EmailAddress))
		{
			return null;
		}
		
		var query2 = QueryBuilder.Where(
			QueryBuilder.Equals("membership_id", membershipId), 
			QueryBuilder.Equals("email_address", request.EmailAddress)).ToString();
		
		var queryUsers2Result = await this._userService.QueryAsync(query2, membershipId, 0, 1, cancellationToken: cancellationToken);
		if (queryUsers2Result.Items.Any())
		{
			// Linking to an existing account by email is safe only if the provider verified the email or it's trusted explicitly
			if (!request.IsEmailVerified && !provider.TrustEmail)
			{
				throw ErtisAuthException.ProviderEmailNotTrusted(provider.Name);
			}
			
			var dynamicUser = queryUsers2Result.Items.First();
			return dynamicUser.Deserialize<User>();
		}
		
		return null;
	}
	
	private void EnsureConnectedAccounts(User user, IProviderLoginRequest request, Provider provider)
	{
		var connectedAccounts = new List<ProviderAccountInfo>();
		if (user.ConnectedAccounts != null)
		{
			connectedAccounts.AddRange(user.ConnectedAccounts);
		}
		
		var account = connectedAccounts.FirstOrDefault(x => x.Provider == provider.Type.ToString());
		if (account != null)
		{
			connectedAccounts.Remove(account);
		}
		
		connectedAccounts.Add(new ProviderAccountInfo
		{
			Provider = provider.Type.ToString(),
			UserId = request.UserId,
			Token = request.AccessToken
		});
		
		user.ConnectedAccounts = connectedAccounts.ToArray();
	}
	
	private void SetAvatar(DynamicObject dynamicUser, IProviderLoginRequest request, UserType? userType)
	{
		if (userType != null)
		{
			if (userType.Properties.Any(x => x is { Type: FieldType.@object, Name: "avatar" }) && !string.IsNullOrEmpty(request.AvatarUrl))
			{
				dynamicUser.SetValue("avatar", new Dictionary<string, object> { { "url", request.AvatarUrl } }, true);	
			}
		}
	}
	
	#endregion
}