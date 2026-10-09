using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ertis.MongoDB.Queries;
using Ertis.Schema.Dynamics;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Helpers;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Providers;
using ErtisAuth.Events.EventArgs;
using ErtisAuth.Infrastructure.Constants;
using ErtisAuth.Infrastructure.Mapping;
using ErtisAuth.Integrations.OAuth.Core;
using ErtisAuth.Integrations.OAuth.Extensions;
using Microsoft.Extensions.Caching.Memory;

namespace ErtisAuth.Infrastructure.Services
{
	public class ProviderService : MembershipBoundedCrudService<Provider, ProviderDto>, IProviderService
	{
		#region Constants

		private const string CACHE_KEY = "providers";

		#endregion
		
		#region Services
		
		private readonly IUserService userService;
		private readonly ITokenService tokenService;
		private readonly IEventService eventService;
		private readonly IMemoryCache _memoryCache;

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="membershipService"></param>
		/// <param name="userService"></param>
		/// <param name="tokenService"></param>
		/// <param name="eventService"></param>
		/// <param name="providerRepository"></param>
		/// <param name="memoryCache"></param>
		public ProviderService(
			IMembershipService membershipService,
			IUserService userService,
			ITokenService tokenService,
			IEventService eventService, 
			IProviderRepository providerRepository, 
			IMemoryCache memoryCache) : base(membershipService, providerRepository)
		{
			this.userService = userService;
			this.tokenService = tokenService;
			this.eventService = eventService;
			this._memoryCache = memoryCache;

			this.OnCreated += this.ProviderCreatedEventHandler;
			this.OnUpdated += this.ProviderUpdatedEventHandler;
			this.OnDeleted += this.ProviderDeletedEventHandler;
		}

		#endregion
	
		#region Event Handlers

		private void ProviderCreatedEventHandler(object sender, CreateResourceEventArgs<Provider> eventArgs)
		{
			this.eventService.FireEventAsync(this, new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.ProviderCreated,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Resource,
				MembershipId = eventArgs.MembershipId
			});
		}
		
		private void ProviderUpdatedEventHandler(object sender, UpdateResourceEventArgs<Provider> eventArgs)
		{
			this.eventService.FireEventAsync(this, new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.ProviderUpdated,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Updated,
				Prior = eventArgs.Prior,
				MembershipId = eventArgs.MembershipId
			});
		}
		
		private void ProviderDeletedEventHandler(object sender, DeleteResourceEventArgs<Provider> eventArgs)
		{
			this.eventService.FireEventAsync(this, new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.ProviderDeleted,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Resource,
				MembershipId = eventArgs.MembershipId
			});
		}

		#endregion

		#region Base Methods
		
		protected override bool ValidateModel(Provider model, out IEnumerable<string> errors)
		{
			var errorList = new List<string>();
			if (string.IsNullOrEmpty(model.Name))
			{
				errorList.Add("name is a required field");
			}

			if (string.IsNullOrEmpty(model.MembershipId))
			{
				errorList.Add("membership_id is a required field");
			}

			if (model.IsActive != null && model.IsActive.Value)
			{
				if (string.IsNullOrEmpty(model.AppClientId))
				{
					errorList.Add("AppClientId is a required field");
				}
				
				if (string.IsNullOrEmpty(model.DefaultRole))
				{
					errorList.Add("DefaultRole is a required field");
				}
				
				if (string.IsNullOrEmpty(model.DefaultUserType))
				{
					errorList.Add("DefaultUserType is a required field");
				}
				
				if (model.Name == KnownProviders.Apple.ToString())
				{
					if (string.IsNullOrEmpty(model.TeamId))
					{
						errorList.Add("TeamId is a required field");
					}
				
					if (string.IsNullOrEmpty(model.PrivateKey))
					{
						errorList.Add("PrivateKey is a required field");
					}
				
					if (string.IsNullOrEmpty(model.PrivateKeyId))
					{
						errorList.Add("PrivateKeyId is a required field");
					}
				
					if (string.IsNullOrEmpty(model.RedirectUri))
					{
						errorList.Add("RedirectUri is a required field");
					}
				}
			}
			
			errors = errorList;
			return !errors.Any();
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
			destination.AppClientId ??= source.AppClientId;
			destination.TenantId ??= source.TenantId;
			destination.DefaultRole ??= source.DefaultRole;
			destination.DefaultUserType ??= source.DefaultUserType;
			destination.IsActive ??= source.IsActive;
		}

		protected override bool IsAlreadyExist(Provider model, string membershipId, Provider exclude = default)
		{
			if (exclude == null)
			{
				return this.GetByName(model.Name, membershipId) != null;	
			}
			else
			{
				var current = this.GetByName(model.Name, membershipId);
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

		protected override async Task<bool> IsAlreadyExistAsync(Provider model, string membershipId, Provider exclude = default)
		{
			if (exclude == null)
			{
				return await this.GetByNameAsync(model.Name, membershipId) != null;	
			}
			else
			{
				var current = await this.GetByNameAsync(model.Name, membershipId);
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
			return ErtisAuthException.ProviderWithSameNameAlreadyExists(model.Name);
		}
		
		protected override ErtisAuthException GetNotFoundError(string id)
		{
			return ErtisAuthException.ProviderNotFound(id);
		}
		
		private Provider GetByName(string name, string membershipId)
		{
			var dto = this.repository.FindOne(x => x.Name == name && x.MembershipId == membershipId);
			return dto == null ? null : Mapper.Current.Map<ProviderDto, Provider>(dto);
		}
		
		private async Task<Provider> GetByNameAsync(string name, string membershipId, CancellationToken cancellationToken = default)
		{
			var dto = await this.repository.FindOneAsync(x => x.Name == name && x.MembershipId == membershipId, cancellationToken: cancellationToken);
			return dto == null ? null : Mapper.Current.Map<ProviderDto, Provider>(dto);
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

		public async Task<IEnumerable<Provider>> GetProvidersAsync(string membershipId, CancellationToken cancellationToken = default)
		{
			var cacheKey = GetCacheKey(membershipId);
			if (this._memoryCache.TryGetValue<IEnumerable<Provider>>(cacheKey, out var cacheResults))
			{
				return cacheResults;
			}
			
			var providerList = new List<Provider>();
			var providers = await this.GetAsync(membershipId, null, null, false, null, null, cancellationToken: cancellationToken);
			
			var utilizer = new Utilizer
			{
				Role = ReservedRoles.Administrator,
				Type = Utilizer.UtilizerType.System,
				MembershipId = membershipId
			};

			var providerTypes = Enum.GetValues<KnownProviders>();
			foreach (var providerType in providerTypes)
			{
				if (providerType == KnownProviders.ErtisAuth)
				{
					continue;
				}
				
				try
				{
					if (providers.Items.All(x => x.Name != providerType.ToString()))
					{
						var provider = await this.CreateAsync(utilizer, membershipId, new Provider(providerType)
						{
							MembershipId = membershipId,
							Description = null,
							AppClientId = null,
							TenantId = null,
							DefaultRole = null,
							DefaultUserType = null,
							IsActive = false
						}, cancellationToken: cancellationToken);
				
						providerList.Add(provider);
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
				}
			}
			
			providerList.AddRange(providers.Items.Where(x => x.Name != KnownProviders.ErtisAuth.ToString()));

			this._memoryCache.Set(cacheKey, providerList, GetCacheTTL());
			return providerList;
		}

		#endregion
		
		#region Create Methods

		public override Provider Create(Utilizer utilizer, string membershipId, Provider model)
		{
			var created = base.Create(utilizer, membershipId, model);
			this.PurgeAllCache(membershipId);
			return created;
		}

		public override async ValueTask<Provider> CreateAsync(Utilizer utilizer, string membershipId, Provider model, CancellationToken cancellationToken = default)
		{
			var created = await base.CreateAsync(utilizer, membershipId, model, cancellationToken);
			this.PurgeAllCache(membershipId);
			return created;
		}

		#endregion

		#region Update Methods

		public override Provider Update(Utilizer utilizer, string membershipId, Provider model)
		{
			var updated = base.Update(utilizer, membershipId, model);
			this.PurgeAllCache(membershipId);
			return updated;
		}

		public override async ValueTask<Provider> UpdateAsync(Utilizer utilizer, string membershipId, Provider model, CancellationToken cancellationToken = default)
		{
			var updated = await base.UpdateAsync(utilizer, membershipId, model, cancellationToken);
			this.PurgeAllCache(membershipId);
			return updated;
		}

		#endregion
		
		#region Delete Methods

		public override bool Delete(Utilizer utilizer, string membershipId, string id)
		{
			var isDeleted = base.Delete(utilizer, membershipId, id);
			if (isDeleted)
			{
				this.PurgeAllCache(membershipId);	
			}
			
			return isDeleted;
		}

		public override async ValueTask<bool> DeleteAsync(Utilizer utilizer, string membershipId, string id, CancellationToken cancellationToken = default)
		{
			var isDeleted = await base.DeleteAsync(utilizer, membershipId, id, cancellationToken);
			if (isDeleted)
			{
				this.PurgeAllCache(membershipId);	
			}
			
			return isDeleted;
		}
		
		#endregion

		#region Authentication Methods

		public async ValueTask<BearerToken> LoginAsync(IProviderLoginRequest request, string membershipId, string ipAddress = null, string userAgent = null, CancellationToken cancellationToken = default)
		{
			var provider = await this.GetAsync(membershipId, (x) => x.MembershipId == membershipId && x.Name == request.Provider.ToString(), cancellationToken: cancellationToken);
			if (provider != null)
			{
				if (provider.IsActive != null && provider.IsActive.Value)
				{
					var providerAuthenticator = provider.GetAuthenticator();
					var isVerified = await providerAuthenticator.VerifyTokenAsync(request, provider, cancellationToken: cancellationToken);
					if (isVerified)
					{
						var utilizer = new Utilizer
						{
							Role = ReservedRoles.Administrator,
							Type = Utilizer.UtilizerType.System,
							MembershipId = membershipId
						};
						
						var user = await this.FindUserAsync(request, provider, membershipId, cancellationToken: cancellationToken);
						var isNewUser = user == null;
						if (isNewUser)
						{
							user = request.ToUser(membershipId, provider.DefaultRole, provider.DefaultUserType) as User;
						}
						else if (!user.IsActive)
						{
							throw ErtisAuthException.UserInactive(user.Id);
						}
						
						this.EnsureConnectedAccounts(user, request, provider);
						var dynamicUser = new DynamicObject(user);
						this.SetAvatar(dynamicUser, request);
						
						var upsertedUser = isNewUser ?
							await this.userService.CreateAsync(utilizer, membershipId, dynamicUser, cancellationToken: cancellationToken) :
							await this.userService.UpdateAsync(utilizer, membershipId, user.Id, dynamicUser, false, cancellationToken: cancellationToken);
						
						return await this.tokenService.GenerateTokenAsync(upsertedUser.Deserialize<User>(), membershipId, ipAddress, userAgent, cancellationToken: cancellationToken);
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
				var user = await this.tokenService.GetTokenOwnerUserAsync(token, cancellationToken: cancellationToken);
				if (user is { ConnectedAccounts: not null })
				{
					var needUserUpdate = false;
					var connectedAccounts = new List<ProviderAccountInfo>();
					foreach (var accountInfo in user.ConnectedAccounts)
					{
						if (!string.IsNullOrEmpty(accountInfo.Token))
						{
							var provider = await this.GetAsync(user.MembershipId, (x) => x.MembershipId == user.MembershipId && x.Name == accountInfo.Provider, cancellationToken: cancellationToken);
							if (provider is { IsActive: not null } && provider.IsActive.Value)
							{
								var providerAuthenticator = provider.GetAuthenticator();
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
						var utilizer = new Utilizer
						{
							Role = ReservedRoles.Administrator,
							Type = Utilizer.UtilizerType.System,
							MembershipId = user.MembershipId
						};

						user.ConnectedAccounts = connectedAccounts.ToArray();
						var dynamicUser = new DynamicObject(user);
						await this.userService.UpdateAsync(utilizer, user.MembershipId, user.Id, dynamicUser, false, cancellationToken: cancellationToken);	
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}

		private async Task<User> FindUserAsync(IProviderLoginRequest request, Provider provider, string membershipId, CancellationToken cancellationToken = default)
		{
			var query = QueryBuilder.Where(
				QueryBuilder.Equals("membership_id", membershipId), 
				QueryBuilder.Equals("connected_accounts.Provider", provider.Name), 
				QueryBuilder.Equals("connected_accounts.UserId", request.UserId)).ToString();
			var queryUsersResult = await this.userService.QueryAsync(membershipId, query, 0, 1, cancellationToken: cancellationToken);
			if (queryUsersResult.Items.Any())
			{
				var dynamicUser = queryUsersResult.Items.First();
				return dynamicUser.Deserialize<User>();
			}
			else
			{
				var query2 = QueryBuilder.Where(
					QueryBuilder.Equals("membership_id", membershipId), 
					QueryBuilder.Equals("email_address", request.EmailAddress)).ToString();
				var queryUsers2Result = await this.userService.QueryAsync(membershipId, query2, 0, 1, cancellationToken: cancellationToken);
				if (queryUsers2Result.Items.Any())
				{
					var dynamicUser = queryUsers2Result.Items.First();
					return dynamicUser.Deserialize<User>();
				}
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

			var account = connectedAccounts.FirstOrDefault(x => x.Provider == provider.Name);
			if (account != null)
			{
				connectedAccounts.Remove(account);
			}
							
			connectedAccounts.Add(new ProviderAccountInfo
			{
				Provider = provider.Name,
				UserId = request.UserId,
				Token = request.AccessToken
			});

			user.ConnectedAccounts = connectedAccounts.ToArray();
		}

		private void SetAvatar(DynamicObject dynamicUser, IProviderLoginRequest request)
		{
			if (!string.IsNullOrEmpty(request.AvatarUrl))
			{
				dynamicUser.SetValue("avatar", new Dictionary<string, object> { { "url", request.AvatarUrl } }, true);	
			}
		}

		#endregion
	}
}