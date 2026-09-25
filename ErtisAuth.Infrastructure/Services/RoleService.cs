using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Events;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Helpers;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Constants;
using Microsoft.Extensions.Caching.Memory;

namespace ErtisAuth.Infrastructure.Services;

public class RoleService : MembershipBoundedCrudService<Role>, IRoleService
{
	#region Constants
	
	private const string CACHE_KEY = "roles";
	
	#endregion
	
	#region Services
	
	private readonly IEventService _eventService;
	private readonly IMemoryCache _memoryCache;
	
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
		this._eventService = eventService;
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
		var memberships = await this._membershipService.GetAsync();
		foreach (var membership in memberships.Items)
		{
			var adminRole = await this.GetBySlugAsync(ReservedRoles.Administrator, membership.Id);
			if (adminRole == null)
			{
				var utilizer = Utilizer.GetSystemUtilizer(membership.Id);
				await this.CreateAdministratorRoleAsync(membership, utilizer);
			}
		}
	}
	
	private async Task CreateAdministratorRoleAsync(Membership membership, Utilizer utilizer, CancellationToken cancellationToken = default)
	{
		string[] reservedResources = {
			"memberships",
			"users",
			"user-types",
			"applications",
			"roles",
			"sessions",
			"events",
			"providers",
			"tokens",
			"webhooks",
			"mailhooks"
		};
		
		RbacSegment[] adminPrivileges =
		{
			Rbac.CrudActionSegments.Create,
			Rbac.CrudActionSegments.Read,
			Rbac.CrudActionSegments.Update,
			Rbac.CrudActionSegments.Delete
		};
		
		var permissions = new List<string>();
		foreach (var resource in reservedResources)
		{
			var resourceSegment = new RbacSegment(resource);
			foreach (var privilege in adminPrivileges)
			{
				var rbac = new Rbac(RbacSegment.All, resourceSegment, privilege, RbacSegment.All);
				permissions.Add(rbac.ToString());
			}
		}
		
		await this.CreateAsync(utilizer, membership.Id, new Role
		{
			Name = "Administrator",
			Slug = ReservedRoles.Administrator,
			Description = "Administrator",
			MembershipId = membership.Id,
			Permissions = permissions
		}, cancellationToken: cancellationToken);
	}
	
	#endregion
	
	#region Event Handlers
	
	private void RoleCreatedEventHandler(object? sender, CreateResourceEventArgs<Role> eventArgs)
	{
		this._eventService.FireEventAsync(ErtisAuthEventType.RoleCreated, eventArgs.Utilizer, eventArgs.MembershipId, eventArgs.Resource);
	}
	
	private void RoleUpdatedEventHandler(object? sender, UpdateResourceEventArgs<Role> eventArgs)
	{
		this._eventService.FireEventAsync(ErtisAuthEventType.RoleUpdated, eventArgs.Utilizer, eventArgs.MembershipId, eventArgs.Updated, eventArgs.Prior);
	}
	
	private void RoleDeletedEventHandler(object? sender, DeleteResourceEventArgs<Role> eventArgs)
	{
		this._eventService.FireEventAsync(ErtisAuthEventType.RoleDeleted, eventArgs.Utilizer, eventArgs.MembershipId, null, eventArgs.Resource);
	}
	
	#endregion
	
	#region Cache Methods
	
	private static string GetCacheKey(string membershipId)
	{
		return $"{CACHE_KEY}.{membershipId}.roles";
	}
	
	private static MemoryCacheEntryOptions GetCacheTTL()
	{
		return new MemoryCacheEntryOptions().SetAbsoluteExpiration(CacheDefaults.RolesCacheTTL);
	}
	
	private Role? GetFromCacheById(string membershipId, string id)
	{
		var cacheKey = GetCacheKey(membershipId);
		if (this._memoryCache.TryGetValue<Role[]>(cacheKey, out var roles) && roles != null)
		{
			return roles.FirstOrDefault(x => x.Id == id);
		}
		
		return null;
	}
	
	private Role? GetFromCacheBySlug(string membershipId, string slug)
	{
		var cacheKey = GetCacheKey(membershipId);
		if (this._memoryCache.TryGetValue<Role[]>(cacheKey, out var roles) && roles != null)
		{
			return roles.FirstOrDefault(x => x.Slug == slug);
		}
		
		return null;
	}
	
	private void RefreshCache(string membershipId)
	{
		var cacheKey = GetCacheKey(membershipId);
		this._memoryCache.Remove(cacheKey);
		var roles = base.Get(membershipId);
		this._memoryCache.Set(cacheKey, roles.Items, GetCacheTTL());
	}
	
	private async Task RefreshCacheAsync(string membershipId)
	{
		var cacheKey = GetCacheKey(membershipId);
		this._memoryCache.Remove(cacheKey);
		var roles = await base.GetAsync(membershipId);
		this._memoryCache.Set(cacheKey, roles.Items, GetCacheTTL());
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
		
		destination.Description ??= source.Description;
		destination.Permissions ??= source.Permissions;
		destination.Forbidden ??= source.Forbidden;
	}
	
	protected override bool IsAlreadyExist(Role model, string membershipId, Role? exclude = null) =>
		this.IsAlreadyExistAsync(model, membershipId, exclude).ConfigureAwait(false).GetAwaiter().GetResult();
	
	protected override async Task<bool> IsAlreadyExistAsync(Role model, string membershipId, Role? exclude = null, CancellationToken cancellationToken = default)
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
	
	public override Role? Get(string membershipId, string id)
	{
		var role = this.GetFromCacheById(membershipId, id);
		return role ?? base.Get(membershipId, id);
	}
	
	public override async ValueTask<Role?> GetAsync(string membershipId, string id, CancellationToken cancellationToken = default)
	{
		var role = this.GetFromCacheById(membershipId, id);
		return role ?? await base.GetAsync(membershipId, id, cancellationToken: cancellationToken);
	}
	
	public Role? GetBySlug(string slug, string membershipId)
	{
		var role = this.GetFromCacheBySlug(membershipId, slug);
		if (role != null)
		{
			return role;
		}
		
		return this._repository.FindOne(x => x.Slug == slug && x.MembershipId == membershipId);
	}
	
	public async ValueTask<Role?> GetBySlugAsync(string slug, string membershipId, CancellationToken cancellationToken = default)
	{
		var role = this.GetFromCacheBySlug(membershipId, slug);
		if (role != null)
		{
			return role;
		}
		
		return await this._repository.FindOneAsync(x => x.Slug == slug && x.MembershipId == membershipId, cancellationToken: cancellationToken);
	}
	
	#endregion
	
	#region Create Methods
	
	public override Role Create(Utilizer utilizer, string membershipId, Role model)
	{
		if (model.Slug is ReservedRoles.Administrator && utilizer.Type != Utilizer.UtilizerType.System)
		{
			throw ErtisAuthException.ReservedRoleName(model.Slug);
		}
		
		var created = base.Create(utilizer, membershipId, model);
		this.RefreshCache(membershipId);
		return created;
	}
	
	public override async ValueTask<Role> CreateAsync(Utilizer utilizer, string membershipId, Role model, CancellationToken cancellationToken = default)
	{
		if (model.Slug is ReservedRoles.Administrator && utilizer.Type != Utilizer.UtilizerType.System)
		{
			throw ErtisAuthException.ReservedRoleName(model.Slug);
		}
		
		var created = await base.CreateAsync(utilizer, membershipId, model, cancellationToken);
		await this.RefreshCacheAsync(membershipId);
		return created;
	}
	
	#endregion
	
	#region Update Methods
	
	public override Role Update(Utilizer utilizer, string membershipId, Role model)
	{
		var updated = base.Update(utilizer, membershipId, model);
		this.RefreshCache(membershipId);
		return updated;
	}
	
	public override async ValueTask<Role> UpdateAsync(Utilizer utilizer, string membershipId, Role model, CancellationToken cancellationToken = default)
	{
		var updated = await base.UpdateAsync(utilizer, membershipId, model, cancellationToken);
		await this.RefreshCacheAsync(membershipId);
		return updated;
	}
	
	#endregion
	
	#region Delete Methods
	
	public override bool Delete(Utilizer utilizer, string membershipId, string id)
	{
		var isDeleted = base.Delete(utilizer, membershipId, id);
		if (isDeleted)
		{
			this.RefreshCache(membershipId);
		}
		
		return isDeleted;
	}
	
	public override async ValueTask<bool> DeleteAsync(Utilizer utilizer, string membershipId, string id, CancellationToken cancellationToken = default)
	{
		var isDeleted = await base.DeleteAsync(utilizer, membershipId, id, cancellationToken);
		if (isDeleted)
		{
			await this.RefreshCacheAsync(membershipId);	
		}
		
		return isDeleted;
	}
	
	#endregion
}