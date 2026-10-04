using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Extensions;
using ErtisAuth.Core.Models;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Constants;
using Microsoft.Extensions.Caching.Memory;
using ErtisAuth.Core.Models.Identity;

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
	
	private readonly IMemoryCache _memoryCache;
	
	#endregion
	
	#region Properties
	
	private List<IMembershipBoundedService> MembershipBoundedServiceCollection { get; }
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="membershipRepository"></param>
	/// <param name="memoryCache"></param>
	public MembershipService(IMembershipRepository membershipRepository, IMemoryCache memoryCache) : base(membershipRepository)
	{
		this.MembershipBoundedServiceCollection = new List<IMembershipBoundedService>();
		this._memoryCache = memoryCache;
	}
	
	#endregion
	
	#region Methods
	
	public void RegisterService<T>(IMembershipBoundedService<T> service) where T : IHasMembership, IHasIdentifier
	{
		if (service is IMembershipBoundedService membershipBoundedResource)
		{
			this.MembershipBoundedServiceCollection.Add(membershipBoundedResource);	
		}
	}
	
	protected override async Task<IEnumerable<string>> ValidateModelAsync(Membership model, CancellationToken cancellationToken = default)
	{
		var membershipWithSameSlug = await this.GetBySlugAsync(model.Slug, cancellationToken: cancellationToken);
		return ValidateModel(model, membershipWithSameSlug, out var errors) ? [] : errors;
	}
	
	/// <param name="model"></param>
	/// <param name="membershipWithSameSlug">The membership having the slug of the model, read by the caller</param>
	/// <param name="errors"></param>
	private static bool ValidateModel(Membership model, Membership? membershipWithSameSlug, out IEnumerable<string> errors)
	{
		var errorList = new List<string>();
		
		if (string.IsNullOrEmpty(model.Name))
		{
			errorList.Add("name is a required field");
		}
		
		if (!model.Slug.IsValidSlug(out var error))
		{
			errorList.Add(error!);
		}
		
		if (model.ExpiresIn <= 0)
		{
			errorList.Add("expires_in is a required field");
		}
		
		if (model.RefreshTokenExpiresIn <= 0)
		{
			errorList.Add("refresh_token_expires_in is a required field");
		}
		
		if (string.IsNullOrEmpty(model.SecretKey))
		{
			errorList.Add("secret_key is a required field");
		}
		else if (model.IsEncodingValid() && model.GetEncoding().GetByteCount(model.SecretKey) < MIN_SECRET_KEY_BYTE_COUNT)
		{
			// Measured in the encoding of the membership, the key is converted to bytes by it
			errorList.Add($"secret_key must be at least {MIN_SECRET_KEY_BYTE_COUNT} bytes ({MIN_SECRET_KEY_BYTE_COUNT * 8} bits) in the encoding of the membership");
		}
		
		if (string.IsNullOrEmpty(model.HashAlgorithm))
		{
			errorList.Add(ErtisAuthException.HashAlgorithmRequired().Message);
		}
		else if (!model.TryGetHashAlgorithm(out _))
		{
			errorList.Add(ErtisAuthException.UnsupportedHashAlgorithm(model.HashAlgorithm).Message);
		}
		
		if (!model.IsEncodingValid())
		{
			errorList.Add(ErtisAuthException.UnsupportedEncoding(model.DefaultEncoding!).Message);
		}
		
		if (membershipWithSameSlug != null && membershipWithSameSlug.Id != model.Id)
		{
			errorList.Add(ErtisAuthException.MembershipAlreadyExists(model.Name).Message);
		}
		
		if (model.OtpSettings != null)
		{
			if (string.IsNullOrEmpty(model.OtpSettings.Host))
			{
				errorList.Add(ErtisAuthException.OtpHostRequired().Message);
			}
			
			if (model.OtpSettings.Policy is { MaxAttempts: < 1 })
			{
				errorList.Add("otp_settings.policy.max_attempts must be greater than zero");
			}
		}
		
		errors = errorList;
		return !errors.Any();
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
		return ErtisAuthException.MembershipAlreadyExists(model.Id);
	}
	
	protected override ErtisAuthException GetNotFoundError(string id)
	{
		return ErtisAuthException.MembershipNotFound(id);
	}
	
	private async Task<IEnumerable<MembershipBoundedResource>> GetMembershipBoundedResourcesAsync(string membershipId, int limit = 10, CancellationToken cancellationToken = default)
	{
		var tasks = this.MembershipBoundedServiceCollection.Select(service => 
			service.GetAsync<MembershipBoundedResource>(membershipId, 0, limit, false, null, null, cancellationToken: cancellationToken))
			.ToArray();
		
		await Task.WhenAll(tasks);
		
		var cumulativeList = new List<MembershipBoundedResource>();
		foreach (var task in tasks)
		{
			var items = (await task).Items;
			cumulativeList.AddRange(items);
		}
		
		return cumulativeList.Take(limit);
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
		var membershipBoundedResources = await this.GetMembershipBoundedResourcesAsync(id, cancellationToken: cancellationToken);
		if (membershipBoundedResources.Any())
		{
			throw ErtisAuthException.MembershipCouldNotDeleted(id);
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