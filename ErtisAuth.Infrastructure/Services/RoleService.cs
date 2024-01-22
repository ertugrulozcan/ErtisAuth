using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Models.Resources;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Helpers;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Roles;
using ErtisAuth.Events.EventArgs;
using ErtisAuth.Infrastructure.Constants;
using ErtisAuth.Infrastructure.Helpers;
using ErtisAuth.Infrastructure.Mapping;
using Microsoft.Extensions.Caching.Memory;

namespace ErtisAuth.Infrastructure.Services
{
	public class RoleService : MembershipBoundedCrudService<Role, RoleDto>, IRoleService
	{
		#region Constants

		private const string CACHE_KEY = "roles";

		#endregion
		
		#region Services

		private readonly IEventService eventService;
		private readonly IMemoryCache _memoryCache;

		#endregion

		#region Properties

		private Dictionary<string, Role> ServerRoleDictionary { get; } = new();

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="membershipService"></param>
		/// <param name="eventService"></param>
		/// <param name="memoryCache"></param>
		/// <param name="roleRepository"></param>
		public RoleService(
			IMembershipService membershipService, 
			IEventService eventService, 
			IMemoryCache memoryCache,
			IRoleRepository roleRepository) : base(membershipService, roleRepository)
		{
			this.eventService = eventService;
			this._memoryCache = memoryCache;

			this.Initialize();
			
			this.OnCreated += this.RoleCreatedEventHandler;
			this.OnUpdated += this.RoleUpdatedEventHandler;
			this.OnDeleted += this.RoleDeletedEventHandler;
		}

		#endregion
		
		#region Initialize Methods

		private void Initialize()
		{
			this.InitializeAsync().AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
		}
		
		private async ValueTask InitializeAsync()
		{
			var memberships = await this.membershipService.GetAsync();
			if (memberships?.Items != null)
			{
				foreach (var membership in memberships.Items)
				{
					var utilizer = new Utilizer
					{
						Role = ReservedRoles.Administrator,
						Type = Utilizer.UtilizerType.System,
						MembershipId = membership.Id
					};
					
					var role = await this.GetByNameAsync(ReservedRoles.Administrator, membership.Id);
					if (role == null)
					{
						await this.CreateAsync(utilizer, membership.Id, new Role
						{
							Name = ReservedRoles.Administrator,
							Description = "Administrator",
							MembershipId = membership.Id,
							Permissions = RoleHelper.AssertAdminPermissionsForReservedResources()
						});
					}
				}	
			}
		}

		#endregion
		
		#region Event Handlers

		private void RoleCreatedEventHandler(object sender, CreateResourceEventArgs<Role> eventArgs)
		{
			this.eventService.FireEventAsync(this, new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.RoleCreated,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Resource,
				MembershipId = eventArgs.MembershipId
			});
		}
		
		private void RoleUpdatedEventHandler(object sender, UpdateResourceEventArgs<Role> eventArgs)
		{
			this.eventService.FireEventAsync(this, new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.RoleUpdated,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Updated,
				Prior = eventArgs.Prior,
				MembershipId = eventArgs.MembershipId
			});
		}
		
		private void RoleDeletedEventHandler(object sender, DeleteResourceEventArgs<Role> eventArgs)
		{
			this.eventService.FireEventAsync(this, new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.RoleDeleted,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Resource,
				MembershipId = eventArgs.MembershipId
			});
		}

		#endregion
		
		#region Cache Methods

		private static string GetCacheKey(string membershipId, string roleId)
		{
			return $"{CACHE_KEY}.{membershipId}.{roleId}";
		}
		
		private static MemoryCacheEntryOptions GetCacheTTL()
		{
			return new MemoryCacheEntryOptions().SetSlidingExpiration(CacheDefaults.RolesCacheTTL);
		}

		private void PurgeAllCache(string membershipId) => this.PurgeAllCacheAsync(membershipId).ConfigureAwait(false).GetAwaiter().GetResult();
		
		private async ValueTask PurgeAllCacheAsync(string membershipId, CancellationToken cancellationToken = default)
		{
			var roles = await this.GetAsync(
				membershipId, 
				null, null, false, null, null,
				cancellationToken: cancellationToken);
			
			foreach (var role in roles.Items)
			{
				var cacheKey1 = GetCacheKey(membershipId, role.Id);
				this._memoryCache.Remove(cacheKey1);
				
				var cacheKey2 = GetCacheKey(membershipId, role.Name);
				this._memoryCache.Remove(cacheKey2);
			}
		}

		#endregion

		#region Methods
		
		protected override bool ValidateModel(Role model, out IEnumerable<string> errors)
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

			try
			{
				var permissionList = new List<Rbac>();
				if (model.Permissions != null)
				{
					foreach (var permission in model.Permissions)
					{
						var rbac = Rbac.Parse(permission);
						permissionList.Add(rbac);
					}
				}
				
				var forbiddenList = new List<Rbac>();
				if (model.Forbidden != null)
				{
					foreach (var forbidden in model.Forbidden)
					{
						var rbac = Rbac.Parse(forbidden);
						forbiddenList.Add(rbac);
					}
				}
				
				// Is there any conflict?
				foreach (var permissionRbac in permissionList)
				{
					foreach (var forbiddenRbac in forbiddenList)
					{
						if (permissionRbac == forbiddenRbac)
						{
							errorList.Add($"Permitted and forbidden sets are conflicted. The same permission is there in the both set. ('{permissionRbac}')");
						}
					}	
				}
			}
			catch (Exception ex)
			{
				errorList.Add(ex.Message);
			}

			errors = errorList;
			return !errors.Any();
		}

		[SuppressMessage("ReSharper", "ConvertIfStatementToNullCoalescingAssignment")]
		protected override void Overwrite(Role destination, Role source)
		{
			destination.Id = source.Id;
			destination.MembershipId = source.MembershipId;
			destination.Sys = source.Sys;
			
			if (this.IsIdentical(destination, source))
			{
				throw ErtisAuthException.IdenticalDocument();
			}
			
			if (string.IsNullOrEmpty(destination.Name))
			{
				destination.Name = source.Name;
			}
			
			if (destination.Description == null)
			{
				destination.Description = source.Description;
			}
			
			if (destination.Permissions == null)
			{
				destination.Permissions = source.Permissions;
			}
			
			if (destination.Forbidden == null)
			{
				destination.Forbidden = source.Forbidden;
			}
		}

		protected override bool IsAlreadyExist(Role model, string membershipId, Role exclude = default) =>
			this.IsAlreadyExistAsync(model, membershipId, exclude).ConfigureAwait(false).GetAwaiter().GetResult();

		protected override async Task<bool> IsAlreadyExistAsync(Role model, string membershipId, Role exclude = default)
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
					return current.Name != exclude.Name;	
				}
				else
				{
					return false;
				}
			}
		}
		
		protected override ErtisAuthException GetAlreadyExistError(Role model)
		{
			return ErtisAuthException.RoleWithSameNameAlreadyExists(model.Name);
		}
		
		protected override ErtisAuthException GetNotFoundError(string id)
		{
			return ErtisAuthException.RoleNotFound(id);
		}

		#endregion

		#region Read Methods

		public override Role Get(string membershipId, string id)
		{
			if (id == ReservedRoles.Server)
			{
				return this.GetServerRole(membershipId);
			}

			var cacheKey = GetCacheKey(membershipId, id);
			if (!this._memoryCache.TryGetValue<Role>(cacheKey, out var role))
			{
				role = base.Get(membershipId, id);
				if (role == null)
				{
					return null;
				}

				this._memoryCache.Set(cacheKey, role, GetCacheTTL());
			}
			
			return role;
		}

		public override async ValueTask<Role> GetAsync(string membershipId, string id, CancellationToken cancellationToken = default)
		{
			if (id == ReservedRoles.Server)
			{
				return await this.GetServerRoleAsync(membershipId, cancellationToken: cancellationToken);
			}
			
			var cacheKey = GetCacheKey(membershipId, id);
			if (!this._memoryCache.TryGetValue<Role>(cacheKey, out var role))
			{
				role = await base.GetAsync(membershipId, id, cancellationToken: cancellationToken);
				if (role == null)
				{
					return null;
				}

				this._memoryCache.Set(cacheKey, role, GetCacheTTL());
			}
			
			return role;
		}

		public Role GetByName(string name, string membershipId)
		{
			if (name == ReservedRoles.Server)
			{
				return this.GetServerRole(membershipId);
			}
			
			var cacheKey = GetCacheKey(membershipId, name);
			if (!this._memoryCache.TryGetValue<Role>(cacheKey, out var role))
			{
				var dto = this.repository.FindOne(x => x.Name == name && x.MembershipId == membershipId);
				if (dto == null)
				{
					return null;
				}
				
				role = Mapper.Current.Map<RoleDto, Role>(dto);
				this._memoryCache.Set(cacheKey, role, GetCacheTTL());
			}
			
			return role;
		}
		
		public async ValueTask<Role> GetByNameAsync(string name, string membershipId, CancellationToken cancellationToken = default)
		{
			if (name == ReservedRoles.Server)
			{
				return await this.GetServerRoleAsync(membershipId, cancellationToken: cancellationToken);
			}
			
			var cacheKey = GetCacheKey(membershipId, name);
			if (!this._memoryCache.TryGetValue<Role>(cacheKey, out var role))
			{
				var dto = await this.repository.FindOneAsync(x => x.Name == name && x.MembershipId == membershipId, cancellationToken: cancellationToken);
				if (dto == null)
				{
					return null;
				}
				
				role = Mapper.Current.Map<RoleDto, Role>(dto);
				this._memoryCache.Set(cacheKey, role, GetCacheTTL());
			}
			
			return role;
		}

		private Role GetServerRole(string membershipId) =>
			this.GetServerRoleAsync(membershipId).ConfigureAwait(false).GetAwaiter().GetResult();
		
		private async Task<Role> GetServerRoleAsync(string membershipId, CancellationToken cancellationToken = default)
		{
			if (this.ServerRoleDictionary.TryGetValue(membershipId, out var role))
			{
				return role;
			}
			
			// Check membership
			var membership = await this.membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			
			var serverRole = new Role
			{
				Id = "server",
				Name = "server",
				Description = "An on the fly role instance required for password set/reset etc operations",
				Permissions = new[]
				{
					"*.users.reset-password.*",
					"*.users.set-password.*"
				},
				Forbidden = null,
				MembershipId = membershipId,
				Sys = new SysModel
				{
					CreatedAt = DateTime.Now,
					CreatedBy = "system"
				}
			};
			
			this.ServerRoleDictionary.Add(membershipId, serverRole);
			return serverRole;
		}

		#endregion
		
		#region Create Methods

		public override Role Create(Utilizer utilizer, string membershipId, Role model)
		{
			if (model.Name is ReservedRoles.Administrator or ReservedRoles.Server && utilizer.Type != Utilizer.UtilizerType.System)
			{
				throw ErtisAuthException.ReservedRoleName(model.Name);
			}
			
			var created = base.Create(utilizer, membershipId, model);
			this.PurgeAllCache(membershipId);
			return created;
		}

		public override async ValueTask<Role> CreateAsync(Utilizer utilizer, string membershipId, Role model, CancellationToken cancellationToken = default)
		{
			if (model.Name is ReservedRoles.Administrator or ReservedRoles.Server && utilizer.Type != Utilizer.UtilizerType.System)
			{
				throw ErtisAuthException.ReservedRoleName(model.Name);
			}
			
			var created = await base.CreateAsync(utilizer, membershipId, model, cancellationToken);
			await this.PurgeAllCacheAsync(membershipId, cancellationToken: cancellationToken);
			return created;
		}

		#endregion

		#region Update Methods

		public override Role Update(Utilizer utilizer, string membershipId, Role model)
		{
			var updated = base.Update(utilizer, membershipId, model);
			this.PurgeAllCache(membershipId);
			return updated;
		}

		public override async ValueTask<Role> UpdateAsync(Utilizer utilizer, string membershipId, Role model, CancellationToken cancellationToken = default)
		{
			var updated = await base.UpdateAsync(utilizer, membershipId, model, cancellationToken);
			await this.PurgeAllCacheAsync(membershipId, cancellationToken: cancellationToken);
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
				await this.PurgeAllCacheAsync(membershipId, cancellationToken: cancellationToken);	
			}
			
			return isDeleted;
		}
		
		#endregion
	}
}