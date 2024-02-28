using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Memberships;
using ErtisAuth.Infrastructure.Constants;
using ErtisAuth.Infrastructure.Mapping;
using Microsoft.Extensions.Caching.Memory;

namespace ErtisAuth.Infrastructure.Services
{
	public class MembershipService : GenericCrudService<Membership, MembershipDto>, IMembershipService
	{
		#region Constants

		private const string CACHE_KEY = "memberships";

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
		
		protected override bool ValidateModel(Membership model, out IEnumerable<string> errors)
		{
			var errorList = new List<string>();
			
			if (string.IsNullOrEmpty(model.Name))
			{
				errorList.Add("name is a required field");
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

			var current = this.GetBySlug(model.Slug);
			if (current != null && current.Id != model.Id)
			{
				errorList.Add(ErtisAuthException.MembershipAlreadyExists(model.Name).Message);
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

		protected override bool IsAlreadyExist(Membership model, Membership exclude = default)
		{
			if (exclude == null)
			{
				return this.Get(model.Id) != null;	
			}
			else
			{
				var current = this.Get(model.Id);
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

		protected override async Task<bool> IsAlreadyExistAsync(Membership model, Membership exclude = default)
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
		
		private IEnumerable<MembershipBoundedResource> GetMembershipBoundedResources(string membershipId, int limit = 10) =>
			this.GetMembershipBoundedResourcesAsync(membershipId, limit).ConfigureAwait(false).GetAwaiter().GetResult();

		private async Task<IEnumerable<MembershipBoundedResource>> GetMembershipBoundedResourcesAsync(string membershipId, int limit = 10, CancellationToken cancellationToken = default)
		{
			var tasks = this.MembershipBoundedServiceCollection.Select(service => 
				service.GetAsync<MembershipBoundedResource>(membershipId, 0, limit, false, null, null, cancellationToken: cancellationToken).AsTask())
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

		private static string GetCacheKey(string membershipId)
		{
			return $"{CACHE_KEY}.{membershipId}";
		}
		
		private static MemoryCacheEntryOptions GetCacheTTL()
		{
			return new MemoryCacheEntryOptions().SetAbsoluteExpiration(CacheDefaults.MembershipsCacheTTL);
		}

		private void PurgeAllCache() => this.PurgeAllCacheAsync().ConfigureAwait(false).GetAwaiter().GetResult();
		
		private async ValueTask PurgeAllCacheAsync(CancellationToken cancellationToken = default)
		{
			var memberships = await this.GetAsync(cancellationToken: cancellationToken);
			foreach (var membership in memberships.Items)
			{
				var cacheKey1 = GetCacheKey(membership.Id);
				this._memoryCache.Remove(cacheKey1);
				
				var cacheKey2 = GetCacheKey(membership.SecretKey);
				this._memoryCache.Remove(cacheKey2);
			}
		}

		#endregion

		#region Read Methods

		public override Membership Get(string id)
		{
			var cacheKey = GetCacheKey(id);
			if (!this._memoryCache.TryGetValue<Membership>(cacheKey, out var membership))
			{
				membership = base.Get(id);
				if (membership == null)
				{
					return null;
				}
				
				this._memoryCache.Set(cacheKey, membership, GetCacheTTL());
			}
			
			return membership;
		}

		public override async ValueTask<Membership> GetAsync(string id, CancellationToken cancellationToken = default)
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

		private Membership GetBySlug(string slug)
		{
			var dto = this.repository.FindOne(x => x.Slug == slug.Trim());
			return dto == null ? null : Mapper.Current.Map<MembershipDto, Membership>(dto);
		}
		
		public Membership GetBySecretKey(string secretKey)
		{
			var cacheKey = GetCacheKey(secretKey);
			if (!this._memoryCache.TryGetValue<Membership>(cacheKey, out var membership))
			{
				var dto = this.repository.FindOne(x => x.SecretKey == secretKey);
				if (dto == null)
				{
					return null;
				}

				membership = Mapper.Current.Map<MembershipDto, Membership>(dto);
				this._memoryCache.Set(cacheKey, membership, GetCacheTTL());
			}
			
			return membership;
		}

		public async Task<Membership> GetBySecretKeyAsync(string secretKey, CancellationToken cancellationToken = default)
		{
			var cacheKey = GetCacheKey(secretKey);
			if (!this._memoryCache.TryGetValue<Membership>(cacheKey, out var membership))
			{
				var dto = await this.repository.FindOneAsync(x => x.SecretKey == secretKey, cancellationToken: cancellationToken);
				if (dto == null)
				{
					return null;
				}

				membership = Mapper.Current.Map<MembershipDto, Membership>(dto);
				this._memoryCache.Set(cacheKey, membership, GetCacheTTL());
			}
			
			return membership;
		}

		#endregion

		#region Create Methods

		public override Membership Create(Membership model)
		{
			var created = base.Create(model);
			this.PurgeAllCache();
			return created;
		}

		public override async ValueTask<Membership> CreateAsync(Membership model, CancellationToken cancellationToken = default)
		{
			var created = await base.CreateAsync(model, cancellationToken: cancellationToken);
			await this.PurgeAllCacheAsync(cancellationToken: cancellationToken);
			return created;
		}

		#endregion

		#region Update Methods

		public override Membership Update(Membership model)
		{
			var updated = base.Update(model);
			this.PurgeAllCache();
			return updated;
		}

		public override async ValueTask<Membership> UpdateAsync(Membership model, CancellationToken cancellationToken = default)
		{
			var updated = await base.UpdateAsync(model, cancellationToken: cancellationToken);
			await this.PurgeAllCacheAsync(cancellationToken: cancellationToken);
			return updated;
		}

		#endregion
		
		#region Delete Methods

		public override bool Delete(string id)
		{
			var membershipBoundedResources = this.GetMembershipBoundedResources(id);
			if (membershipBoundedResources.Any())
			{
				throw ErtisAuthException.MembershipCouldNotDeleted(id);
			}

			var isDeleted = base.Delete(id);
			if (isDeleted)
			{
				this.PurgeAllCache();	
			}
			
			return isDeleted;
		}
		
		public override async ValueTask<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
		{
			var membershipBoundedResources = await this.GetMembershipBoundedResourcesAsync(id, cancellationToken: cancellationToken);
			if (membershipBoundedResources.Any())
			{
				throw ErtisAuthException.MembershipCouldNotDeleted(id);
			}
			
			var isDeleted = await base.DeleteAsync(id, cancellationToken: cancellationToken);
			if (isDeleted)
			{
				await this.PurgeAllCacheAsync(cancellationToken: cancellationToken);	
			}

			return isDeleted;
		}
		
		#endregion
	}
}