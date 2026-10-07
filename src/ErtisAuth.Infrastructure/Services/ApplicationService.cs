using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Events;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Constants;
using ErtisAuth.Infrastructure.Helpers;
using Ertis.Core.Collections;
using ErtisAuth.Core.Extensions;
using ErtisAuth.Core.Models;
using ErtisAuth.Infrastructure.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Infrastructure.Services;

public class ApplicationService : MembershipBoundedCrudService<Application>, IApplicationService
{
	#region Constants
	
	private const string CACHE_KEY = "applications";
	
	private const string SECRET_HASH_FIELD = "secret_hash";
	
	protected override IReadOnlyCollection<string> HiddenFields => [SECRET_HASH_FIELD];
	
	#endregion
	
	#region Services
	
	private readonly IRoleService _roleService;
	private readonly IEventService _eventService;
	private readonly ILogger<ApplicationService> _logger;
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
	/// <param name="logger"></param>
	public ApplicationService(
		IMembershipService membershipService, 
		IRoleService roleService, 
		IEventService eventService,
		IMemoryCache memoryCache,
		IApplicationRepository applicationRepository,
		ILogger<ApplicationService> logger) : base(membershipService, applicationRepository)
	{
		this._roleService = roleService;
		this._eventService = eventService;
		this._logger = logger;
		this._memoryCache = memoryCache;
		
		this.OnCreated += this.ApplicationCreatedEventHandler;
		this.OnUpdated += this.ApplicationUpdatedEventHandler;
		this.OnDeleted += this.ApplicationDeletedEventHandler;
	}
	
	#endregion
	
	#region Event Handlers
	
	private async void ApplicationCreatedEventHandler(object? sender, CreateResourceEventArgs<Application> eventArgs)
	{
		try
		{
			await this._eventService.FireEventAsync(ErtisAuthEventType.ApplicationCreated, eventArgs.Utilizer, eventArgs.MembershipId, eventArgs.Resource);
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "ApplicationService.ApplicationCreatedEventHandler occured an error");
		}
	}
	
	private async void ApplicationUpdatedEventHandler(object? sender, UpdateResourceEventArgs<Application> eventArgs)
	{
		try
		{
			await this._eventService.FireEventAsync(ErtisAuthEventType.ApplicationUpdated, eventArgs.Utilizer, eventArgs.MembershipId, eventArgs.Updated, eventArgs.Prior);
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "ApplicationService.ApplicationUpdatedEventHandler occured an error");
		}
	}
	
	private async void ApplicationDeletedEventHandler(object? sender, DeleteResourceEventArgs<Application> eventArgs)
	{
		try
		{
			await this._eventService.FireEventAsync(ErtisAuthEventType.ApplicationDeleted, eventArgs.Utilizer, eventArgs.MembershipId, null, eventArgs.Resource);
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "ApplicationService.ApplicationDeletedEventHandler occured an error");
		}
	}
	
	#endregion
	
	#region Methods
	
	protected override async Task<IEnumerable<string>> ValidateModelAsync(Application model, CancellationToken cancellationToken = default)
	{
		var role = string.IsNullOrEmpty(model.Role) ? null : await this._roleService.GetBySlugAsync(model.Role, model.MembershipId, cancellationToken: cancellationToken);
		return ValidateModel(model, role, out var errors) ? [] : errors;
	}
	
	/// <param name="model"></param>
	/// <param name="role">The role named in the model, read by the caller (null when it does not exist)</param>
	/// <param name="errors"></param>
	private static bool ValidateModel(Application model, Role? role, out IEnumerable<string> errors)
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
		
		if (string.IsNullOrEmpty(model.Role))
		{
			errorList.Add("Role is required");
		}
		else if (role == null)
		{
			errorList.Add($"Role is invalid. There is no role with '{model.Role}' slug");
		}
		
		if (UbacExtensions.HasConflict(model.Permissions, model.Forbidden, out var conflict) && conflict != null)
		{
			errorList.Add($"Permitted and forbidden sets are conflicted. The same permission is there in the both set. ('{conflict}')");
		}
		
		errors = errorList;
		return !errors.Any();
	}
	
	protected override async Task<Application> TouchAsync(Application model, CrudOperation crudOperation, CancellationToken cancellationToken = default)
	{
		if (model.Permissions != null)
		{
			model.Permissions = model.Permissions.Distinct().Order().ToArray();
		}
		
		if (model.Forbidden != null)
		{
			model.Forbidden = model.Forbidden.Distinct().Order().ToArray();
		}
		
		return await Task.FromResult(model);
	}
	
	protected override void Overwrite(Application destination, Application source)
	{
		destination.Id = source.Id;
		destination.MembershipId = source.MembershipId;
		destination.Sys = source.Sys;
		
		// The secret is server-managed; it can only be changed by rotation
		destination.SecretHash = source.SecretHash;
		
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
	
	protected override async Task<bool> IsAlreadyExistAsync(Application model, string membershipId, Application? exclude = null, CancellationToken cancellationToken = default)
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
	
	protected override ErtisAuthException GetAlreadyExistError(Application model)
	{
		return ErtisAuthException.ApplicationAlreadyExists(model.Slug);
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
			this.PurgeCache(membershipId, application.Id);
		}
	}
	
	/// <summary>
	/// Removes both cache entries of the application; the membership-less one is used by Basic token verification.
	/// </summary>
	private void PurgeCache(string membershipId, string applicationId)
	{
		this._memoryCache.Remove(GetCacheKey(membershipId, applicationId));
		this._memoryCache.Remove(GetCacheKey("*", applicationId));
	}
	
	#endregion
	
	#region Query Methods
	
	public override async Task<IPaginationCollection<dynamic>> QueryAsync(
		string query, 
		string membershipId, 
		int? skip = null, 
		int? limit = null, 
		bool? withCount = null, 
		string? sortField = null,
		SortDirection? sortDirection = null, 
		IDictionary<string, bool>? projection = null, 
		CancellationToken cancellationToken = default)
	{
		return await base.QueryAsync(query, membershipId, skip, limit, withCount, sortField, sortDirection, ExcludeSecretHash(projection), cancellationToken: cancellationToken);
	}
	
	/// <summary>
	/// Query results are raw documents, so the secret hash is excluded by projection (JsonIgnore does not apply to them).
	/// An inclusion projection returns only the listed fields, so it is enough to drop the hash from it;
	/// otherwise the hash is excluded explicitly (MongoDB does not allow mixing inclusion and exclusion).
	/// </summary>
	private static IDictionary<string, bool> ExcludeSecretHash(IDictionary<string, bool>? projection)
	{
		var fields = projection != null ? new Dictionary<string, bool>(projection) : new Dictionary<string, bool>();
		var isInclusion = fields.Any(x => x.Key != "_id" && x.Value);
		if (isInclusion)
		{
			foreach (var key in fields.Keys.Where(x => x == SECRET_HASH_FIELD || x.StartsWith($"{SECRET_HASH_FIELD}.")).ToArray())
			{
				fields.Remove(key);
			}
		}
		else
		{
			fields[SECRET_HASH_FIELD] = false;
		}
		
		return fields;
	}
	
	#endregion
	
	#region Read Methods
	
	public override async Task<Application?> GetAsync(string id, string membershipId, CancellationToken cancellationToken = default)
	{
		var cacheKey = GetCacheKey(membershipId, id);
		if (!this._memoryCache.TryGetValue<Application>(cacheKey, out var application))
		{
			application = await base.GetAsync(id, membershipId, cancellationToken: cancellationToken);
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
	
	public async Task<Application?> GetBySlugAsync(string slug, string membershipId, CancellationToken cancellationToken = default)
	{
		return await this._repository.FindOneAsync(x => x.Slug == slug && x.MembershipId == membershipId, cancellationToken: cancellationToken);
	}
	
	#endregion
	
	#region Create Methods
	
	public override async Task<Application> CreateAsync(Application model, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default)
	{
		var created = await base.CreateAsync(model, membershipId, utilizer, cancellationToken);
		await this.PurgeAllCacheAsync(membershipId, cancellationToken: cancellationToken);
		return created;
	}
	
	public async Task<ApplicationWithSecret> CreateWithSecretAsync(Application model, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default)
	{
		var secret = ApplicationSecretHelper.GenerateSecret();
		model.SecretHash = ApplicationSecretHelper.HashSecret(secret);
		var created = await this.CreateAsync(model, membershipId, utilizer, cancellationToken: cancellationToken);
		return new ApplicationWithSecret(created, secret);
	}
	
	#endregion
	
	#region Update Methods
	
	public override async Task<Application> UpdateAsync(Application model, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default)
	{
		var updated = await base.UpdateAsync(model, membershipId, utilizer, cancellationToken);
		await this.PurgeAllCacheAsync(membershipId, cancellationToken: cancellationToken);
		return updated;
	}
	
	public async Task<ApplicationWithSecret> RotateSecretAsync(string id, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default)
	{
		var membership = await this._membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
		if (membership == null)
		{
			throw ErtisAuthException.MembershipNotFound(membershipId);
		}
		
		// Read from the database, not the cache, so that a cached instance is never modified
		var current = await this._repository.FindOneAsync(x => x.Id == id && x.MembershipId == membershipId, cancellationToken: cancellationToken);
		if (current == null)
		{
			throw this.GetNotFoundError(id);
		}
		
		var secret = ApplicationSecretHelper.GenerateSecret();
		var model = new Application
		{
			Id = current.Id,
			MembershipId = current.MembershipId,
			Name = current.Name,
			Slug = current.Slug,
			Role = current.Role,
			Permissions = current.Permissions,
			Forbidden = current.Forbidden,
			Sys = SysInfoHelper.Modified(current.Sys, utilizer),
			SecretHash = ApplicationSecretHelper.HashSecret(secret)
		};
		
		var updated = await this._repository.UpdateAsync(model, cancellationToken: cancellationToken);
		this.PurgeCache(membershipId, id);
		await this.PurgeAllCacheAsync(membershipId, cancellationToken: cancellationToken);
		
		await this._eventService.FireEventAsync(ErtisAuthEventType.ApplicationUpdated, utilizer, membershipId, updated, current, cancellationToken: cancellationToken);
		
		return new ApplicationWithSecret(updated, secret);
	}
	
	#endregion
	
	#region Delete Methods
	
	public override async Task<bool> DeleteAsync(string id, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default)
	{
		var isDeleted = await base.DeleteAsync(id, membershipId, utilizer, cancellationToken);
		if (isDeleted)
		{
			// The deleted application is no longer listed by PurgeAllCacheAsync, so its own entries are removed explicitly
			this.PurgeCache(membershipId, id);
			await this.PurgeAllCacheAsync(membershipId, cancellationToken: cancellationToken);	
		}
		
		return isDeleted;
	}
	
	public override async Task<bool?> BulkDeleteAsync(string[] ids, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default)
	{
		var result = await base.BulkDeleteAsync(ids, membershipId, utilizer, cancellationToken: cancellationToken);
		foreach (var id in ids)
		{
			this.PurgeCache(membershipId, id);
		}
		
		await this.PurgeAllCacheAsync(membershipId, cancellationToken: cancellationToken);
		return result;
	}
	
	#endregion
}