using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Constants;
using Microsoft.Extensions.Caching.Memory;

namespace ErtisAuth.Infrastructure.Services;

public class RevokedTokenService : MembershipBoundedService<RevokedToken>, IRevokedTokenService
{
	#region Constants
	
	private const string CACHE_KEY = "revoked-tokens";
	
	#endregion
	
	#region Services
	
	private readonly IMemoryCache _memoryCache;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="membershipService"></param>
	/// <param name="memoryCache"></param>
	/// <param name="repository"></param>
	public RevokedTokenService(
		IMembershipService membershipService, 
		IMemoryCache memoryCache,
		IRevokedTokensRepository repository) : base(membershipService, repository)
	{
		this._memoryCache = memoryCache;
	}
	
	#endregion
	
	#region Cache Methods
	
	private static string GetCacheKey(string token)
	{
		return $"{CACHE_KEY}.{token}";
	}
	
	private static MemoryCacheEntryOptions GetCacheTTL()
	{
		return new MemoryCacheEntryOptions().SetAbsoluteExpiration(CacheDefaults.RevokedTokensCacheTTL);
	}
	
	#endregion
	
	#region Methods
	
	public async Task<RevokedToken?> GetByAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default)
	{
		var cacheKey = GetCacheKey(accessToken);
		if (!this._memoryCache.TryGetValue<RevokedToken>(cacheKey, out var revokedToken))
		{
			revokedToken = await this._repository.FindOneAsync(x => x.Token == accessToken, cancellationToken: cancellationToken);
			if (revokedToken != null)
			{
				this._memoryCache.Set(cacheKey, revokedToken, GetCacheTTL());	
			}
		}
		
		return revokedToken;
	}
	
	public async Task RevokeAsync(string token, User user, bool isRefreshToken, DateTime retainUntil, CancellationToken cancellationToken = default)
	{
		var revokedToken = new RevokedToken
		{
			Token = token,
			RevokedAt = DateTime.UtcNow,
			RetainUntil = retainUntil,
			UserId = user.Id,
			UserName = user.Username,
			EmailAddress = user.EmailAddress,
			FirstName = user.FirstName,
			LastName = user.LastName,
			MembershipId = user.MembershipId,
			TokenType = isRefreshToken ? "refresh_token" : "bearer_token"
		};
		
		await this._repository.InsertAsync(revokedToken, cancellationToken: cancellationToken);
		var cacheKey = GetCacheKey(token);
		this._memoryCache.Set(cacheKey, revokedToken, GetCacheTTL());
	}
	
	#endregion
}