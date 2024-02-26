using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Identity;
using ErtisAuth.Infrastructure.Constants;
using ErtisAuth.Infrastructure.Extensions;
using Microsoft.Extensions.Caching.Memory;

namespace ErtisAuth.Infrastructure.Services
{
	public class RevokedTokenService : MembershipBoundedService<RevokedToken, RevokedTokenDto>, IRevokedTokenService
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
		/// <param name="repository"></param>
		/// <param name="memoryCache"></param>
		public RevokedTokenService(IMembershipService membershipService, IRevokedTokensRepository repository, IMemoryCache memoryCache) : base(membershipService, repository)
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
		
		public async Task<RevokedToken> GetByAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default)
		{
			var cacheKey = GetCacheKey(accessToken);
			if (!this._memoryCache.TryGetValue<RevokedToken>(cacheKey, out var revokedToken))
			{
				var dto = await this.repository.FindOneAsync(x => x.Token.AccessToken == accessToken, cancellationToken: cancellationToken);
				revokedToken = dto?.ToModel();
				if (revokedToken != null)
				{
					this._memoryCache.Set(cacheKey, revokedToken, GetCacheTTL());	
				}
			}

			return revokedToken;
		}

		public async Task RevokeAsync(ActiveToken activeToken, User user, bool isRefreshToken, CancellationToken cancellationToken = default)
		{
			var dto = new RevokedTokenDto
			{
				Token = activeToken.ToDto(),
				RevokedAt = DateTime.Now,
				UserId = user.Id,
				UserName = user.Username,
				EmailAddress = user.EmailAddress,
				FirstName = user.FirstName,
				LastName = user.LastName,
				MembershipId = user.MembershipId,
				TokenType = isRefreshToken ? "refresh_token" : "bearer_token"
			};
			
			await this.repository.InsertAsync(dto, cancellationToken: cancellationToken);
			var cacheKey = GetCacheKey(activeToken.AccessToken);
			this._memoryCache.Set(cacheKey, dto.ToModel(), GetCacheTTL());
		}
		
		public async ValueTask ClearRevokedTokens(string membershipId, CancellationToken cancellationToken = default)
		{
			try
			{
				var revokedTokensResult = await this.repository.FindAsync(x => x.MembershipId == membershipId && x.RevokedAt < DateTime.Now.AddHours(24), sorting: null, cancellationToken: cancellationToken);
				var revokedTokens = revokedTokensResult.Items.ToArray();
				if (revokedTokens.Any())
				{
					var isDeleted = await this.repository.BulkDeleteAsync(revokedTokens, cancellationToken: cancellationToken);
					if (isDeleted)
					{
						Console.WriteLine($"{revokedTokens.Length} revoked token cleared");
					}

					foreach (var revokedToken in revokedTokens)
					{
						var cacheKey = GetCacheKey(revokedToken.Token.AccessToken);
						this._memoryCache.Remove(cacheKey);	
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}
		
		#endregion
	}
}