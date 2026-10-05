using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Events;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Extensions;
using ErtisAuth.Core.Helpers;
using ErtisAuth.Core.Models;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Constants;
using ErtisAuth.Infrastructure.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Infrastructure.Services;

public class RoleService : MembershipBoundedCrudService<Role>, IRoleService
{
	#region Constants
	
	private const string CACHE_KEY = "roles";
	
	#endregion
	
	#region Services
	
	private readonly IEventService _eventService;
	private readonly ILogger<RoleService> _logger;
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
	/// <param name="logger"></param>
	public RoleService(
		IMembershipService membershipService, 
		IEventService eventService, 
		IMemoryCache memoryCache,
		IRoleRepository roleRepository,
		ILogger<RoleService> logger) : base(membershipService, roleRepository)
	{
		this._eventService = eventService;
		this._logger = logger;
		this._memoryCache = memoryCache;
		
		this.OnCreated += this.RoleCreatedEventHandler;
		this.OnUpdated += this.RoleUpdatedEventHandler;
		this.OnDeleted += this.RoleDeletedEventHandler;
	}
	
	#endregion
	
	#region Administrator Role Methods
	
	public async Task EnsureAdministratorRolesAsync(CancellationToken cancellationToken = default)
	{
		var memberships = await this._membershipService.GetAsync(cancellationToken: cancellationToken);
		foreach (var membership in memberships.Items)
		{
			await this.EnsureAdministratorRoleAsync(membership, cancellationToken: cancellationToken);
		}
	}
	
	public async Task<Role> EnsureAdministratorRoleAsync(Membership membership, CancellationToken cancellationToken = default)
	{
		var adminRole = await this.GetBySlugAsync(ReservedRoles.Administrator.Slug, membership.Id, cancellationToken: cancellationToken);
		if (adminRole != null)
		{
			return adminRole;
		}
		
		var utilizer = Utilizer.GetSystemUtilizer(membership.Id);
		return await this.CreateAdministratorRoleAsync(membership, utilizer, cancellationToken: cancellationToken);
	}
	
	private async Task<Role> CreateAdministratorRoleAsync(Membership membership, Utilizer utilizer, CancellationToken cancellationToken = default)
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
			"mailhooks",
			"code-policies",
			"otp"
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
		
		return await this.CreateAsync(new Role
		{
			Name = ReservedRoles.Administrator.Name,
			Slug = ReservedRoles.Administrator.Slug,
			Description = ReservedRoles.Administrator.Description,
			MembershipId = membership.Id,
			Permissions = permissions
		}, membership.Id, utilizer, cancellationToken: cancellationToken);
	}
	
	#endregion
	
	#region Event Handlers
	
	private async void RoleCreatedEventHandler(object? sender, CreateResourceEventArgs<Role> eventArgs)
	{
		try
		{
			await this._eventService.FireEventAsync(ErtisAuthEventType.RoleCreated, eventArgs.Utilizer, eventArgs.MembershipId, eventArgs.Resource);
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "RoleService.RoleCreatedEventHandler occured an error");
		}
	}
	
	private async void RoleUpdatedEventHandler(object? sender, UpdateResourceEventArgs<Role> eventArgs)
	{
		try
		{
			await this._eventService.FireEventAsync(ErtisAuthEventType.RoleUpdated, eventArgs.Utilizer, eventArgs.MembershipId, eventArgs.Updated, eventArgs.Prior);
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "RoleService.RoleUpdatedEventHandler occured an error");
		}
	}
	
	private async void RoleDeletedEventHandler(object? sender, DeleteResourceEventArgs<Role> eventArgs)
	{
		try
		{
			await this._eventService.FireEventAsync(ErtisAuthEventType.RoleDeleted, eventArgs.Utilizer, eventArgs.MembershipId, null, eventArgs.Resource);
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "RoleService.RoleDeletedEventHandler occured an error");
		}
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
	
	private async Task RefreshCacheAsync(string membershipId)
	{
		var cacheKey = GetCacheKey(membershipId);
		this._memoryCache.Remove(cacheKey);
		var roles = await base.GetAsync(membershipId);
		this._memoryCache.Set(cacheKey, roles.Items, GetCacheTTL());
	}
	
	#endregion
	
	#region Methods
	
	protected override Task<IEnumerable<string>> ValidateModelAsync(Role model, CancellationToken cancellationToken = default)
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
		
		if (RbacExtensions.HasConflict(model.Permissions, model.Forbidden, out var conflict) && conflict != null)
		{
			errorList.Add($"Permitted and forbidden sets are conflicted. The same permission is there in the both set. ('{conflict}')");
		}
		
		return Task.FromResult<IEnumerable<string>>(errorList);
	}
	
	protected override async Task<Role> TouchAsync(Role model, CrudOperation crudOperation, CancellationToken cancellationToken = default)
	{
		if (model.Permissions != null)
		{
			model.Permissions = model.Permissions.Distinct().Order().ToArray();
		}
		
		if (model.Forbidden != null)
		{
			model.Forbidden = model.Forbidden.Distinct().Order().ToArray();
		}
		
		await Task.CompletedTask;
		return model;
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
		return ErtisAuthException.RoleAlreadyExists(model.Slug);
	}
	
	protected override ErtisAuthException GetNotFoundError(string id)
	{
		return ErtisAuthException.RoleNotFound(id);
	}
	
	#endregion
	
	#region Read Methods
	
	public override async Task<Role?> GetAsync(string id, string membershipId, CancellationToken cancellationToken = default)
	{
		var role = this.GetFromCacheById(membershipId, id);
		return role ?? await base.GetAsync(id, membershipId, cancellationToken: cancellationToken);
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
	
	public override async Task<Role> CreateAsync(Role model, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default)
	{
		if (ReservedRoles.IsReserved(model.Slug) && utilizer.Type != Utilizer.UtilizerType.System)
		{
			throw ErtisAuthException.ReservedRole(model.Slug);
		}
		
		var created = await base.CreateAsync(model, membershipId, utilizer, cancellationToken);
		await this.RefreshCacheAsync(membershipId);
		return created;
	}
	
	#endregion
	
	#region Update Methods
	
	public override async Task<Role> UpdateAsync(Role model, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default)
	{
		var updated = await base.UpdateAsync(model, membershipId, utilizer, cancellationToken);
		await this.RefreshCacheAsync(membershipId);
		return updated;
	}
	
	#endregion
	
	#region Delete Methods
	
	public override async Task<bool> DeleteAsync(string id, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default)
	{
		var role = await this.GetAsync(id, membershipId, cancellationToken: cancellationToken);
		if (role == null)
		{
			throw ErtisAuthException.RoleNotFound(id);
		}
		
		if (ReservedRoles.IsReserved(role.Slug))
		{
			throw ErtisAuthException.SystemRolesCannotBeDeleted(role.Slug);
		}
		
		var isDeleted = await base.DeleteAsync(id, membershipId, utilizer, cancellationToken);
		if (isDeleted)
		{
			await this.RefreshCacheAsync(membershipId);
		}
		
		return isDeleted;
	}
	
	public override async Task<bool?> BulkDeleteAsync(string[] ids, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default)
	{
		foreach (var id in ids)
		{
			var role = await this.GetAsync(id, membershipId, cancellationToken: cancellationToken);
			if (role != null && ReservedRoles.IsReserved(role.Slug))
			{
				throw ErtisAuthException.SystemRolesCannotBeDeleted(role.Slug);
			}
		}
		
		var result = await base.BulkDeleteAsync(ids, membershipId, utilizer, cancellationToken: cancellationToken);
		await this.RefreshCacheAsync(membershipId);
		return result;
	}
	
	#endregion
}