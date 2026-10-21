using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Extensions;
using ErtisAuth.Core.Models;
using ErtisAuth.Core.Models.Memberships;
using Ertis.MongoDB.Queries;
using ErtisAuth.Core.Constants;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Constants;
using Microsoft.Extensions.Caching.Memory;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Extensions.Mailing.MailChimp;
using ErtisAuth.Extensions.Mailing.SendGrid;
using ErtisAuth.Extensions.Mailing.SmtpServer;

namespace ErtisAuth.Infrastructure.Services;

public class MembershipService : GenericCrudService<Membership>, IMembershipService
{
	#region Constants
	
	private const string CACHE_KEY = "memberships";
	
	/// <summary>
	/// The tokens are signed with HMAC-SHA256 by the secret key, which requires a key of at least 256 bits
	/// (a shorter key can't sign any token: every login of the membership would fail)
	/// </summary>
	private const int MIN_SECRET_KEY_BYTE_COUNT = 32;
	
	#endregion
	
	#region Services
	
	private readonly List<IMembershipBoundedService> _membershipBoundedServices = new();
	private readonly IMailHookRepository _mailHookRepository;
	private readonly IUserRepository _userRepository;
	private readonly IMemoryCache _memoryCache;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="membershipRepository"></param>
	/// <param name="userRepository"></param>
	/// <param name="mailHookRepository"></param>
	/// <param name="memoryCache"></param>
	public MembershipService(
		IMembershipRepository membershipRepository, 
		IUserRepository userRepository,
		IMailHookRepository mailHookRepository,
		IMemoryCache memoryCache) : 
		base(membershipRepository)
	{
		this._mailHookRepository = mailHookRepository;
		this._userRepository = userRepository;
		this._memoryCache = memoryCache;
	}
	
	#endregion
	
	#region Methods
	
	public void RegisterService<T>(IMembershipBoundedService<T> service) where T : IHasMembership, IHasIdentifier
	{
		if (service is IMembershipBoundedService membershipBoundedResource)
		{
			this._membershipBoundedServices.Add(membershipBoundedResource);
		}
	}
	
	protected override async Task<IEnumerable<string>> ValidateModelAsync(Membership model, CancellationToken cancellationToken = default)
	{
		var current = await this.GetBySlugAsync(model.Slug, cancellationToken: cancellationToken);
		
		var errors = new List<string>();
		
		// Name
		if (string.IsNullOrEmpty(model.Name))
		{
			errors.Add("Name is required");
		}
		
		// Slug
		if (!model.Slug.IsValidSlug(out var error))
		{
			errors.Add(error!);
		}
		
		// Is already exist?
		if (current != null && current.Id != model.Id)
		{
			errors.Add(ErtisAuthException.MembershipAlreadyExists(model.Slug).Message);
		}
		
		// ExpiresIn
		if (model.ExpiresIn <= 0)
		{
			errors.Add("Expires-in is required");
		}
		
		// RefreshTokenExpiresIn
		if (model.RefreshTokenExpiresIn <= 0)
		{
			errors.Add("Refresh token expires-in is required");
		}
		
		// Secret Key
		if (string.IsNullOrEmpty(model.SecretKey))
		{
			errors.Add("Secret key is required");
		}
		else if (model.IsValidEncoding() && model.GetEncoding().GetByteCount(model.SecretKey) < MIN_SECRET_KEY_BYTE_COUNT)
		{
			// Measured in the encoding of the membership, the key is converted to bytes by it
			errors.Add($"Secret key must be at least {MIN_SECRET_KEY_BYTE_COUNT} bytes ({MIN_SECRET_KEY_BYTE_COUNT * 8} bits) in the encoding of the membership");
		}
		
		// Hash Algorithm
		if (string.IsNullOrEmpty(model.HashAlgorithm))
		{
			errors.Add(ErtisAuthException.HashAlgorithmRequired().Message);
		}
		else if (!model.TryGetHashAlgorithm(out _))
		{
			errors.Add(ErtisAuthException.UnsupportedHashAlgorithm(model.HashAlgorithm).Message);
		}
		
		// Encoding
		if (!model.IsValidEncoding())
		{
			errors.Add(ErtisAuthException.UnsupportedEncoding(model.DefaultEncoding!).Message);
		}
		
		// Default Language
		if (!string.IsNullOrEmpty(model.DefaultLanguage) && TextSearchLanguage.All.All(x => x.ISO6391Code != model.DefaultLanguage))
		{
			errors.Add(ErtisAuthException.UnsupportedLanguage(model.DefaultLanguage).Message);
		}
		
		// OtpSettings
		if (model.OtpSettings != null)
		{
			if (string.IsNullOrEmpty(model.OtpSettings.Host))
			{
				errors.Add(ErtisAuthException.OtpHostRequired().Message);
			}
			
			if (model.OtpSettings.Policy is { MaxAttempts: < 1 })
			{
				errors.Add("otp_settings.policy.max_attempts must be greater than zero");
			}
		}
		
		// MailProviders
		if (model.MailProviders != null)
		{
			foreach (var mailProvider in model.MailProviders)
			{
				if (mailProvider.Type == MailProviderType.SmtpServer)
		        {
		            if (mailProvider is SmtpServerProvider provider)
		            {
		                if (string.IsNullOrWhiteSpace(provider.Host))
		                {
							errors.Add("Host is required");
		                }
		                
		                if (provider.Port is < 1 or > 65535)
		                {
							errors.Add("Port must be between 1 and 65535");
		                }
		                
		                var hasUsername = !string.IsNullOrWhiteSpace(provider.Username);
		                var hasPassword = !string.IsNullOrEmpty(provider.Password);
		                if (hasUsername != hasPassword)
		                {
							errors.Add("Username and password must be provided together");
		                }
		            }
		            else
		            {
						errors.Add("The mail provider type value and the instance type are incompatible");
		            }
		        }
		        else if (mailProvider.Type == MailProviderType.SendGrid)
		        {
		            if (mailProvider is SendGridProvider provider)
		            {
		                if (string.IsNullOrWhiteSpace(provider.ApiKey))
		                {
							errors.Add("Api key is required");
		                }
		            }
		            else
		            {
						errors.Add("The mail provider type value and the instance type are incompatible");
		            }
		        }
		        else if (mailProvider.Type == MailProviderType.MailChimp)
		        {
		            if (mailProvider is MailChimpProvider provider)
		            {
		                if (string.IsNullOrWhiteSpace(provider.ApiKey))
		                {
							errors.Add("Api key is required");
		                }
		            }
		            else
		            {
						errors.Add("The mail provider type value and the instance type are incompatible");
		            }
		        }
		        else
		        {
					errors.Add($"Unknown mail provider type: '{mailProvider.Type.ToString()}'");
		        }
			}
		}
		
		// User Activation
		if (model.UserActivation == Status.Active)
		{
			var mailhooks = await this._mailHookRepository.FindAsync(x =>
					x.Slug == MailHookService.USER_ACTIVATION_MAIL_HOOK_SLUG &&
					x.MembershipId == model.Id &&
					x.Event == ErtisAuthEventType.UserCreated.ToString() &&
					x.Status == "active",
				sorting: null,
				cancellationToken: cancellationToken);
			
			var activationMailHook = mailhooks.Items.FirstOrDefault(x => x.MembershipId == model.Id);
			if (activationMailHook == null)
			{
				errors.Add(ErtisAuthException.ActivationMailHookWasNotDefined().Message);
			}
			else if (!activationMailHook.IsActive)
			{
				errors.Add(ErtisAuthException.ActivationMailHookDisabled().Message);
			}
			else if (string.IsNullOrEmpty(activationMailHook.MailProvider) || model.MailProviders == null || model.MailProviders.All(x => x.Slug != activationMailHook.MailProvider))
			{
				errors.Add(ErtisAuthException.ActivationMailHookProviderNotConfigured().Message);
			}
		}
		
		return errors;
	}
	
	protected override void Overwrite(Membership destination, Membership source)
	{
		destination.Id = source.Id;
		destination.Sys = source.Sys;
		
		if (string.IsNullOrEmpty(destination.Name))
		{
			destination.Name = source.Name;
		}
		
		if (string.IsNullOrEmpty(destination.SecretKey))
		{
			destination.SecretKey = source.SecretKey;
		}
		
		destination.AllowMembershipSecretForApplications ??= source.AllowMembershipSecretForApplications; // LEGACY-APP-SECRET
		
		if (string.IsNullOrEmpty(destination.HashAlgorithm))
		{
			destination.HashAlgorithm = source.HashAlgorithm;
		}
		
		if (string.IsNullOrEmpty(destination.DefaultEncoding))
		{
			destination.DefaultEncoding = source.DefaultEncoding;
		}
		
		if (destination.ExpiresIn == 0)
		{
			destination.ExpiresIn = source.ExpiresIn;
		}
		
		if (destination.RefreshTokenExpiresIn == 0)
		{
			destination.RefreshTokenExpiresIn = source.RefreshTokenExpiresIn;
		}
	}
	
	protected override async Task<bool> IsAlreadyExistAsync(Membership model, Membership? exclude = null)
	{
		if (exclude == null)
		{
			return await this.GetAsync(model.Id) != null;	
		}
		else
		{
			var current = await this.GetAsync(model.Id);
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
	
	protected override ErtisAuthException GetAlreadyExistError(Membership model)
	{
		return ErtisAuthException.MembershipAlreadyExists(model.Slug);
	}
	
	protected override ErtisAuthException GetNotFoundError(string id)
	{
		return ErtisAuthException.MembershipNotFound(id);
	}
	
	private async Task<bool> HasAnyResourceAsync(string membershipId, CancellationToken cancellationToken = default)
	{
		var query = QueryBuilder.Equals("membership_id", membershipId);
		var userCount = await this._userRepository.CountAsync(query.ToString(), cancellationToken: cancellationToken);
		if (userCount > 0)
		{
			return true;
		}
		
		foreach (var service in this._membershipBoundedServices)
		{
			var count = await service.CountAsync(membershipId, cancellationToken: cancellationToken);
			if (count > 0)
			{
				return true;
			}
		}
		
		return false;
	}
	
	#endregion
	
	#region Cache Methods
	
	// Ids and secret keys are kept under separate prefixes, so that a lookup by one can never hit an entry of the other
	private static string GetCacheKey(string membershipId)
	{
		return $"{CACHE_KEY}.id.{membershipId}";
	}
	
	private static string GetSecretKeyCacheKey(string secretKey)
	{
		return $"{CACHE_KEY}.secret-key.{secretKey}";
	}
	
	private static MemoryCacheEntryOptions GetCacheTTL()
	{
		return new MemoryCacheEntryOptions().SetAbsoluteExpiration(CacheDefaults.MembershipsCacheTTL);
	}
	
	private async Task PurgeAllCacheAsync(CancellationToken cancellationToken = default)
	{
		var memberships = await this.GetAsync(cancellationToken: cancellationToken);
		foreach (var membership in memberships.Items)
		{
			this.PurgeCache(membership);
		}
	}
	
	private void PurgeCache(Membership? membership)
	{
		if (membership == null)
		{
			return;
		}
		
		this._memoryCache.Remove(GetCacheKey(membership.Id));
		if (!string.IsNullOrEmpty(membership.SecretKey))
		{
			this._memoryCache.Remove(GetSecretKeyCacheKey(membership.SecretKey));
		}
	}
	
	#endregion
	
	#region Read Methods
	
	public override async Task<Membership?> GetAsync(string id, CancellationToken cancellationToken = default)
	{
		var cacheKey = GetCacheKey(id);
		if (!this._memoryCache.TryGetValue<Membership>(cacheKey, out var membership))
		{
			membership = await base.GetAsync(id, cancellationToken);
			if (membership == null)
			{
				return null;
			}
			
			this._memoryCache.Set(cacheKey, membership, GetCacheTTL());
		}
		
		return membership;
	}
	
	public async Task<Membership?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
	{
		return await this._repository.FindOneAsync(x => x.Slug == slug.Trim(), cancellationToken: cancellationToken);
	}
	
	public async Task<Membership?> GetBySecretKeyAsync(string secretKey, CancellationToken cancellationToken = default)
	{
		var cacheKey = GetSecretKeyCacheKey(secretKey);
		if (!this._memoryCache.TryGetValue<Membership>(cacheKey, out var membership))
		{
			membership = await this._repository.FindOneAsync(x => x.SecretKey == secretKey, cancellationToken: cancellationToken);
			if (membership == null)
			{
				return null;
			}
			
			this._memoryCache.Set(cacheKey, membership, GetCacheTTL());
		}
		
		return membership;
	}
	
	#endregion
	
	#region Create Methods
	
	public override async Task<Membership> CreateAsync(Membership model, Utilizer utilizer, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(model.HashAlgorithm))
		{
			model.HashAlgorithm = Defaults.GetDefaultHashAlgorithm();
		}
		
		if (string.IsNullOrEmpty(model.DefaultEncoding))
		{
			model.DefaultEncoding = Defaults.GetDefaultEncodingName();
		}
		
		if (string.IsNullOrEmpty(model.DefaultLanguage))
		{
			model.DefaultLanguage = Defaults.GetDefaultLocale();
		}
		
		// LEGACY-APP-SECRET: new memberships have no legacy applications
		model.AllowMembershipSecretForApplications = false;
		
		var created = await base.CreateAsync(model, utilizer, cancellationToken: cancellationToken);
		await this.PurgeAllCacheAsync(cancellationToken: cancellationToken);
		return created;
	}
	
	#endregion
	
	#region Update Methods
	
	public override async Task<Membership> UpdateAsync(Membership model, Utilizer utilizer, CancellationToken cancellationToken = default)
	{
		// The secret key may change, so the entries of the prior version are removed explicitly (read from the database, not the cache)
		var prior = await base.GetAsync(model.Id, cancellationToken: cancellationToken);
		var updated = await base.UpdateAsync(model, utilizer, cancellationToken: cancellationToken);
		this.PurgeCache(prior);
		await this.PurgeAllCacheAsync(cancellationToken: cancellationToken);
		return updated;
	}
	
	#endregion
	
	#region Delete Methods
	
	public override async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
	{
		if (await this.HasAnyResourceAsync(id, cancellationToken: cancellationToken))
		{
			throw ErtisAuthException.MembershipCouldNotDeleted($"This membership is already using by some membership related resources, it's could not be deleted ({id})");
		}
		
		// The deleted membership is no longer listed by PurgeAllCacheAsync, so its own entries are removed explicitly
		var prior = await base.GetAsync(id, cancellationToken: cancellationToken);
		var isDeleted = await base.DeleteAsync(id, cancellationToken: cancellationToken);
		if (isDeleted)
		{
			this.PurgeCache(prior);
			await this.PurgeAllCacheAsync(cancellationToken: cancellationToken);	
		}
		
		return isDeleted;
	}
	
	#endregion
}