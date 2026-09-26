using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Constants;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Infrastructure.Services;

public class RevokedTokenService : MembershipBoundedService<RevokedToken>, IRevokedTokenService
{
	#region Constants
	
	private const string CACHE_KEY = "revoked-tokens";
	
	#endregion
	
	#region Services
	
	private readonly IMemoryCache _memoryCache;
	private readonly ILogger<RevokedTokenService> _logger;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="membershipService"></param>
	/// <param name="memoryCache"></param>
	/// <param name="repository"></param>
	/// <param name="logger"></param>
	public RevokedTokenService(
		IMembershipService membershipService, 
		IMemoryCache memoryCache,
		IRevokedTokensRepository repository,
		ILogger<RevokedTokenService> logger) : base(membershipService, repository)
	{
		this._memoryCache = memoryCache;
		this._logger = logger;
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
	
	public async Task RevokeAsync(ActiveToken activeToken, User user, bool isRefreshToken, CancellationToken cancellationToken = default)
	{
		var revokedToken = new RevokedToken
		{
			Token = activeToken.AccessToken,
			RevokedAt = DateTime.UtcNow,
			UserId = user.Id,
			UserName = user.Username,
			EmailAddress = user.EmailAddress,
			FirstName = user.FirstName,
			LastName = user.LastName,
			MembershipId = user.MembershipId,
			TokenType = isRefreshToken ? "refresh_token" : "bearer_token"
		};
		
		await this._repository.InsertAsync(revokedToken, cancellationToken: cancellationToken);
		var cacheKey = GetCacheKey(activeToken.AccessToken);
		this._memoryCache.Set(cacheKey, revokedToken, GetCacheTTL());
	}
	
	public async Task ClearRevokedTokens(string membershipId, CancellationToken cancellationToken = default)
	{
		try
		{
			var revokedTokensResult = await this._repository.FindAsync(x => x.MembershipId == membershipId && x.RevokedAt < DateTime.UtcNow.AddHours(24), sorting: null, cancellationToken: cancellationToken);
			var revokedTokens = revokedTokensResult.Items.ToArray();
			if (revokedTokens.Any())
			{
				var isDeleted = await this._repository.BulkDeleteAsync(revokedTokens, cancellationToken: cancellationToken);
				if (isDeleted)
				{
					this._logger.LogInformation("{Count} revoked token cleared", revokedTokens.Length);
				}
				
				foreach (var revokedToken in revokedTokens)
				{
					var cacheKey = GetCacheKey(revokedToken.Token!);
					this._memoryCache.Remove(cacheKey);	
				}
			}
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "RevokedTokenService.ClearRevokedTokens occured an error");
		}
	}
	
	#endregion
}