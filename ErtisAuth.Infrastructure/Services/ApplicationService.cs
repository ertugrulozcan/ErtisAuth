using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Events;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Constants;
using Microsoft.Extensions.Caching.Memory;

namespace ErtisAuth.Infrastructure.Services;

public class ApplicationService : MembershipBoundedCrudService<Application>, IApplicationService
{
	#region Constants
	
	private const string CACHE_KEY = "applications";
	
	#endregion
	
	#region Services
	
	private readonly IRoleService _roleService;
	private readonly IEventService _eventService;
	private readonly IMemoryCache _memoryCache;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="membershipService"></param>
	/// <param name="roleService"></param>
	/// <param name="eventService"></param>
	/// <param name="memoryCache"></param>
	/// <param name="applicationRepository"></param>
	public ApplicationService(
		IMembershipService membershipService, 
		IRoleService roleService, 
		IEventService eventService,
		IMemoryCache memoryCache,
		IApplicationRepository applicationRepository) : base(membershipService, applicationRepository)
	{
		this._roleService = roleService;
		this._eventService = eventService;
		this._memoryCache = memoryCache;
		
		this.OnCreated += this.ApplicationCreatedEventHandler;
		this.OnUpdated += this.ApplicationUpdatedEventHandler;
		this.OnDeleted += this.ApplicationDeletedEventHandler;
	}
	
	#endregion
	
	#region Event Handlers
	
	private void ApplicationCreatedEventHandler(object? sender, CreateResourceEventArgs<Application> eventArgs)
	{
		this._eventService.FireEventAsync(ErtisAuthEventType.ApplicationCreated, eventArgs.Utilizer, eventArgs.MembershipId, eventArgs.Resource);
	}
	
	private void ApplicationUpdatedEventHandler(object? sender, UpdateResourceEventArgs<Application> eventArgs)
	{
		this._eventService.FireEventAsync(ErtisAuthEventType.ApplicationUpdated, eventArgs.Utilizer, eventArgs.MembershipId, eventArgs.Updated, eventArgs.Prior);
	}
	
	private void ApplicationDeletedEventHandler(object? sender, DeleteResourceEventArgs<Application> eventArgs)
	{
		this._eventService.FireEventAsync(ErtisAuthEventType.ApplicationDeleted, eventArgs.Utilizer, eventArgs.MembershipId, null, eventArgs.Resource);
	}
	
	#endregion
	
	#region Methods
	
	protected override bool ValidateModel(Application model, out IEnumerable<string> errors)
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
		
		if (string.IsNullOrEmpty(model.Role))
		{
			errorList.Add("role is a required field");
		}
		else
		{
			var role = this._roleService.GetBySlug(model.Role, model.MembershipId);
			if (role == null)
			{
				errorList.Add($"Role is invalid. There is no role named '{model.Role}'");
			}
		}
		
		try
		{
			var permissionList = new List<Ubac>();
			if (model.Permissions != null)
			{
				foreach (var permission in model.Permissions)
				{
					var ubac = Ubac.Parse(permission);
					permissionList.Add(ubac);
				}
			}
			
			var forbiddenList = new List<Ubac>();
			if (model.Forbidden != null)
			{
				foreach (var forbidden in model.Forbidden)
				{
					var ubac = Ubac.Parse(forbidden);
					forbiddenList.Add(ubac);
				}
			}
			
			// Is there any conflict?
			foreach (var permissionUbac in permissionList)
			{
				foreach (var forbiddenUbac in forbiddenList)
				{
					if (permissionUbac == forbiddenUbac)
					{
						errorList.Add($"Permitted and forbidden sets are conflicted. The same permission is there in the both set. ('{permissionUbac}')");
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
	
	protected override void Overwrite(Application destination, Application source)
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
		
		if (string.IsNullOrEmpty(destination.Role))
		{
			destination.Role = source.Role;
		}
		
		destination.Permissions ??= source.Permissions;
		destination.Forbidden ??= source.Forbidden;
	}
	
	protected override bool IsAlreadyExist(Application model, string membershipId, Application? exclude = null)
	{
		if (exclude == null)
		{
			return this.GetApplicationBySlug(model.Slug, membershipId) != null;	
		}
		else
		{
			var current = this.GetApplicationBySlug(model.Slug, membershipId);
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
	
	protected override async Task<bool> IsAlreadyExistAsync(Application model, string membershipId, Application? exclude = null, CancellationToken cancellationToken = default)
	{
		if (exclude == null)
		{
			return await this.GetApplicationBySlugAsync(model.Slug, membershipId, cancellationToken: cancellationToken) != null;	
		}
		else
		{
			var current = await this.GetApplicationBySlugAsync(model.Slug, membershipId, cancellationToken: cancellationToken);
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
	
	protected override ErtisAuthException GetAlreadyExistError(Application model)
	{
		return ErtisAuthException.ApplicationWithSameNameAlreadyExists($"'{model.Name}'");
	}
	
	protected override ErtisAuthException GetNotFoundError(string id)
	{
		return ErtisAuthException.ApplicationNotFound(id);
	}
	
	#endregion
	
	#region Cache Methods
	
	private static string GetCacheKey(string membershipId, string applicationId)
	{
		return $"{CACHE_KEY}.{membershipId}.{applicationId}";
	}
	
	private static MemoryCacheEntryOptions GetCacheTTL()
	{
		return new MemoryCacheEntryOptions().SetAbsoluteExpiration(CacheDefaults.ApplicationsCacheTTL);
	}
	
	private void PurgeAllCache(string membershipId) => this.PurgeAllCacheAsync(membershipId).ConfigureAwait(false).GetAwaiter().GetResult();
	
	private async Task PurgeAllCacheAsync(string membershipId, CancellationToken cancellationToken = default)
	{
		var applications = await this.GetAsync(
			membershipId, 
			skip: null, 
			limit: null, 
			withCount: false, 
			orderBy: null,
			cancellationToken: cancellationToken);
		
		foreach (var application in applications.Items)
		{
			var cacheKey = GetCacheKey(membershipId, application.Id);
			this._memoryCache.Remove(cacheKey);
		}
	}
	
	#endregion
	
	#region Read Methods
	
	public override Application? Get(string membershipId, string id)
	{
		var cacheKey = GetCacheKey(membershipId, id);
		if (!this._memoryCache.TryGetValue<Application>(cacheKey, out var application))
		{
			application = base.Get(membershipId, id);
			if (application == null)
			{
				return null;
			}
			
			this._memoryCache.Set(cacheKey, application, GetCacheTTL());
		}
		
		return application;
	}
	
	public override async Task<Application?> GetAsync(string membershipId, string id, CancellationToken cancellationToken = default)
	{
		var cacheKey = GetCacheKey(membershipId, id);
		if (!this._memoryCache.TryGetValue<Application>(cacheKey, out var application))
		{
			application = await base.GetAsync(membershipId, id, cancellationToken: cancellationToken);
			if (application == null)
			{
				return null;
			}
			
			this._memoryCache.Set(cacheKey, application, GetCacheTTL());
		}
		
		return application;
	}
	
	public Application? GetById(string id)
	{
		var cacheKey = GetCacheKey("*", id);
		if (!this._memoryCache.TryGetValue<Application>(cacheKey, out var application))
		{
			application = this._repository.FindOne(x => x.Id == id);
			if (application == null)
			{
				return null;
			}
			
			this._memoryCache.Set(cacheKey, application, GetCacheTTL());
		}
		
		return application;
	}
	
	public async ValueTask<Application?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
	{
		var cacheKey = GetCacheKey("*", id);
		if (!this._memoryCache.TryGetValue<Application>(cacheKey, out var application))
		{
			application = await this._repository.FindOneAsync(x => x.Id == id, cancellationToken);
			if (application == null)
			{
				return null;
			}
			
			this._memoryCache.Set(cacheKey, application, GetCacheTTL());
		}
		
		return application;
	}
	
	private Application? GetApplicationBySlug(string slug, string membershipId)
	{
		return this._repository.FindOne(x => x.Slug == slug && x.MembershipId == membershipId);
	}
	
	private async Task<Application?> GetApplicationBySlugAsync(string slug, string membershipId, CancellationToken cancellationToken = default)
	{
		return await this._repository.FindOneAsync(x => x.Slug == slug && x.MembershipId == membershipId, cancellationToken: cancellationToken);
	}
	
	#endregion
	
	#region Create Methods
	
	public override Application Create(Utilizer utilizer, string membershipId, Application model)
	{
		var created = base.Create(utilizer, membershipId, model);
		this.PurgeAllCache(membershipId);
		return created;
	}
	
	public override async Task<Application> CreateAsync(Utilizer utilizer, string membershipId, Application model, CancellationToken cancellationToken = default)
	{
		var created = await base.CreateAsync(utilizer, membershipId, model, cancellationToken);
		await this.PurgeAllCacheAsync(membershipId, cancellationToken: cancellationToken);
		return created;
	}
	
	#endregion
	
	#region Update Methods
	
	public override Application Update(Utilizer utilizer, string membershipId, Application model)
	{
		var updated = base.Update(utilizer, membershipId, model);
		this.PurgeAllCache(membershipId);
		return updated;
	}
	
	public override async Task<Application> UpdateAsync(Utilizer utilizer, string membershipId, Application model, CancellationToken cancellationToken = default)
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
	
	public override async Task<bool> DeleteAsync(Utilizer utilizer, string membershipId, string id, CancellationToken cancellationToken = default)
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