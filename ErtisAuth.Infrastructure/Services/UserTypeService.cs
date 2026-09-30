using System.Collections.ObjectModel;
using Ertis.MongoDB.Queries;
using Ertis.Schema.Extensions;
using Ertis.Schema.Types;
using Ertis.Schema.Types.CustomTypes;
using Ertis.Schema.Types.Primitives;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Events;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Constants;
using Microsoft.Extensions.Caching.Memory;

namespace ErtisAuth.Infrastructure.Services;

public class UserTypeService : MembershipBoundedCrudService<UserType>, IUserTypeService
{
    #region Constants
	
    private const string CACHE_KEY = "user-types";
	
    #endregion
    
    #region Services
	
    private readonly IEventService _eventService;
    private readonly IUserRepository _userRepository;
    private readonly IUserUniqueIndexSynchronizer _uniqueIndexSynchronizer;
    private readonly IMemoryCache _memoryCache;
	
    #endregion
    
    #region Fields
	
	/// <summary>
	/// Serializes the user type changes (of this instance), so that the unique index synchronization of a change
	/// doesn't drop the index just created for another one.
	/// </summary>
	private readonly SemaphoreSlim _changeLock = new(1, 1);
	
    private static UserType? originUserType;
    private static ObjectFieldInfo? sysFieldInfo;
	
    #endregion
    
    #region Properties
	
    internal static UserType OriginUserType
    {
	    get
	    {
		    return originUserType ??= new UserType
		    {
			    Id = UserType.ORIGIN_USER_TYPE_SLUG,
			    Name = UserType.ORIGIN_USER_TYPE_NAME,
			    Description = "Origin User Type",
			    IsAbstract = true,
			    IsSealed = false,
			    AllowAdditionalProperties = false,
			    Properties = new IFieldInfo[]
			    {
				    new StringFieldInfo
				    {
					    Name = "firstname",
					    DisplayName = "First Name",
					    Description = "First Name",
					    IsRequired = true
				    },
				    new StringFieldInfo
				    {
					    Name = "lastname",
					    DisplayName = "Last Name",
					    Description = "Last Name",
					    IsRequired = false
				    },
				    new StringFieldInfo
				    {
					    Name = "username",
					    DisplayName = "Username",
					    Description = "Username",
					    IsRequired = true,
					    IsVirtual = true,
					    IsUnique = true
				    },
				    new EmailAddressFieldInfo
				    {
					    Name = "email_address",
					    DisplayName = "Email Address",
					    Description = "Email Address",
					    IsRequired = true,
					    IsUnique = true
				    },
				    new StringFieldInfo
				    {
					    Name = "role",
					    DisplayName = "Role",
					    Description = "Role",
					    IsRequired = true
				    },
				    new ArrayFieldInfo
				    {
					    Name = "permissions",
					    DisplayName = "User Permissions",
					    Description = "UBAC Permissions Array",
					    IsRequired = false,
					    ItemSchema = new StringFieldInfo { Name = "ubac" },
					    UniqueItems = true
				    },
				    new ArrayFieldInfo
				    {
					    Name = "forbidden",
					    DisplayName = "User Forbidden",
					    Description = "UBAC Forbidden Array",
					    IsRequired = false,
					    ItemSchema = new StringFieldInfo { Name = "ubac" },
					    UniqueItems = true
				    },
				    new StringFieldInfo
				    {
					    Name = "password_hash",
					    DisplayName = "Password",
					    Description = "Password Hash",
					    DefaultValue = null,
					    IsRequired = false,
					    IsHidden = true,
					    IsReadonly = true
				    },
				    new StringFieldInfo
				    {
					    Name = "user_type",
					    DisplayName = "User Type",
					    Description = "User Type",
					    DefaultValue = UserType.ORIGIN_USER_TYPE_SLUG,
					    IsRequired = true
				    },
				    new StringFieldInfo
				    {
					    Name = "source_provider",
					    DisplayName = "Source Provider",
					    Description = "Initial User Provider",
					    DefaultValue = "ErtisAuth",
					    IsRequired = true,
					    IsReadonly = true
				    },
				    new ArrayFieldInfo
				    {
					    Name = "connected_accounts",
					    DisplayName = "Connected Accounts",
					    Description = "Connected Provider Accounts",
					    IsRequired = false,
					    IsReadonly = true,
					    ItemSchema = new ObjectFieldInfo(new IFieldInfo[]
					    {
							new StringFieldInfo
						    {
							    Name = "Provider",
							    IsRequired = true,
							    IsVirtual = false
						    },
						    new StringFieldInfo
						    {
							    Name = "UserId",
							    IsRequired = true,
							    IsVirtual = false
						    },
						    new StringFieldInfo
						    {
							    Name = "Token",
							    IsRequired = false,
							    IsVirtual = false
						    }
					    })
						{
							Name = ""
						},
					    UniqueItems = true,
					    UniqueBy = new[] { "Provider" }
				    },
				    new BooleanFieldInfo
				    {
					    Name = "is_active",
					    DisplayName = "Is Active",
					    DefaultValue = false,
					    IsRequired = true,
					    IsReadonly = true
				    },
				    new StringFieldInfo
				    {
					    Name = "membership_id",
					    DisplayName = "Membership Id",
					    Description = "Membership Id",
					    IsRequired = true,
					    IsReadonly = true
				    },
				    SysFieldInfo
			    },
				MembershipId = string.Empty
		    };
	    }
    }
    
    private static ObjectFieldInfo SysFieldInfo
    {
	    get
	    {
		    return sysFieldInfo ??= new ObjectFieldInfo(new IFieldInfo[]
		    {
			    new StringFieldInfo
			    {
				    Name = "created_by",
				    IsRequired = true,
				    IsVirtual = false,
				    IsUnique = false
			    },
			    new DateTimeFieldInfo
			    {
				    Name = "created_at",
				    IsRequired = true,
				    IsVirtual = false,
				    IsUnique = false
			    },
			    new StringFieldInfo
			    {
				    Name = "modified_by",
				    IsRequired = false,
				    IsVirtual = false,
				    IsUnique = false
			    },
			    new DateTimeFieldInfo
			    {
				    Name = "modified_at",
				    IsRequired = false,
				    IsVirtual = false,
				    IsUnique = false
			    }
		    })
		    {
			    Name = "sys",
			    IsRequired = true,
			    IsVirtual = false,
			    IsReadonly = true
		    };
	    }
    }
	
    #endregion
    
    #region Constructors
	
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="membershipService"></param>
    /// <param name="eventService"></param>
    /// <param name="repository"></param>
    /// <param name="userRepository">Used directly instead of IUserService, which depends on this service</param>
    /// <param name="uniqueIndexSynchronizer"></param>
    /// <param name="memoryCache"></param>
    public UserTypeService(
        IMembershipService membershipService,
        IEventService eventService,
        IUserTypeRepository repository,
        IUserRepository userRepository,
        IUserUniqueIndexSynchronizer uniqueIndexSynchronizer,
        IMemoryCache memoryCache)
        : base(membershipService, repository)
    {
        this._eventService = eventService;
        this._userRepository = userRepository;
        this._uniqueIndexSynchronizer = uniqueIndexSynchronizer;
        this._memoryCache = memoryCache;
        
        this.OnCreated += this.UserTypeCreatedEventHandler;
        this.OnUpdated += this.UserTypeUpdatedEventHandler;
        this.OnDeleted += this.UserTypeDeletedEventHandler;
    }
	
    #endregion
    
    #region Event Handlers
	
    private void UserTypeCreatedEventHandler(object? sender, CreateResourceEventArgs<UserType> eventArgs)
    {
		this._eventService.FireEventAsync(ErtisAuthEventType.UserTypeCreated, eventArgs.Utilizer, eventArgs.MembershipId, eventArgs.Resource);
    }
	
    private void UserTypeUpdatedEventHandler(object? sender, UpdateResourceEventArgs<UserType> eventArgs)
    {
		this._eventService.FireEventAsync(ErtisAuthEventType.UserTypeUpdated, eventArgs.Utilizer, eventArgs.MembershipId, eventArgs.Updated, eventArgs.Prior);
    }
	
    private void UserTypeDeletedEventHandler(object? sender, DeleteResourceEventArgs<UserType> eventArgs)
    {
		this._eventService.FireEventAsync(ErtisAuthEventType.UserTypeDeleted, eventArgs.Utilizer, eventArgs.MembershipId, null, eventArgs.Resource);
    }
	
    #endregion
    
    #region Methods
	
    public async Task<Dictionary<string, List<string>>?> GetFieldInfoOwnerRelationsAsync(string id, string membershipId, CancellationToken cancellationToken = default)
    {
        var fieldInfoOwnerRelationDictionary = new Dictionary<string, List<string>>();
		
        UserType? userType = null;
        if (id == OriginUserType.Id)
        {
	        if (OriginUserType.Clone() is UserType userType_)
	        {
		        userType_.MembershipId = membershipId;
		        userType = userType_;
	        }
        }
        else
        {
	        userType = await base.GetAsync(id, membershipId, cancellationToken: cancellationToken);
        }
        
        if (userType == null)
        {
	        return null;
        }
        
        var ancestors = await this.GetGenealogyAsync(userType, cancellationToken: cancellationToken);
        foreach (var fieldInfo in userType.Properties)
        {
	        var ancestor = ancestors.LastOrDefault(x => x.Properties.Any(y => y.Name == fieldInfo.Name));
	        if (ancestor != null)
	        {
		        var declaringType = ancestor.Slug;
		        if (!fieldInfoOwnerRelationDictionary.ContainsKey(declaringType))
		        {
			        fieldInfoOwnerRelationDictionary.Add(declaringType, new List<string>());
		        }
				
		        fieldInfoOwnerRelationDictionary[declaringType].Add(fieldInfo.Name);
	        }
        }
        
        return fieldInfoOwnerRelationDictionary;
    }
    
    private async Task<List<UserType>> GetGenealogyAsync(UserType userType, CancellationToken cancellationToken = default)
    {
        var ancestors = new List<UserType> { userType };
        var pivotUserType = userType;
		
        do
        {
	        pivotUserType = pivotUserType.BaseUserType != null ? await this.GetBaseUserTypeAsync(pivotUserType.BaseUserType, userType.MembershipId, cancellationToken: cancellationToken) : null;
	        if (pivotUserType != null)
	        {
		        ancestors.Add(pivotUserType);
	        }
        } 
        while (
	        pivotUserType != null &&
	        !string.IsNullOrEmpty(pivotUserType.BaseUserType) &&
	        pivotUserType.BaseUserType != UserType.ORIGIN_USER_TYPE_SLUG
	    );
		
        return ancestors;
    }
	
    protected override void Overwrite(UserType destination, UserType source)
    {
        destination.Id = source.Id;
        destination.MembershipId = source.MembershipId;
        destination.Sys = source.Sys;
		
        if (this.IsIdentical(destination, source))
        {
	        throw ErtisAuthException.IdenticalDocument();
        }
    }
    
    protected override async Task<UserType> TouchAsync(UserType model, CrudOperation crudOperation, CancellationToken cancellationToken = default)
    {
        model = await this.EnsureBaseUserTypeAsync(model, crudOperation, cancellationToken: cancellationToken);
        return model;
    }
	
    private async Task<UserType> EnsureBaseUserTypeAsync(UserType model, CrudOperation crudOperation, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(model.BaseUserType))
        {
	        model.BaseUserType = OriginUserType.Slug;
        }
		
        var baseUserType = await this.GetBaseUserTypeAsync(model.BaseUserType, model.MembershipId, cancellationToken: cancellationToken);
        if (baseUserType == null)
        {
	        throw ErtisAuthException.InheritedTypeNotFound(model.BaseUserType);
        }
		
        if (baseUserType.IsSealed)
        {
	        throw ErtisAuthException.InheritedTypeIsSealed(model.BaseUserType);
        }
		
        model.Properties = new ReadOnlyCollection<IFieldInfo>(model.MergeTypeProperties(baseUserType, crudOperation is CrudOperation.Update or CrudOperation.Create).ToList());
		
        return model;
    }
	
    private async Task<UserType?> GetBaseUserTypeAsync(string baseUserTypeName, string membershipId, CancellationToken cancellationToken = default)
    {
        if (baseUserTypeName == OriginUserType.Name || baseUserTypeName == OriginUserType.Slug)
        {
	        if (OriginUserType.Clone() is UserType userType)
	        {
		        userType.MembershipId = membershipId;
		        return userType;
	        }
        }
		
        return await this.GetByNameOrSlugAsync(baseUserTypeName, membershipId, cancellationToken: cancellationToken);
    }
	
    public async Task<UserType?> GetByNameOrSlugAsync(string nameOrSlug, string membershipId, bool forceGetFreshData = false, CancellationToken cancellationToken = default)
	{
		if (nameOrSlug == OriginUserType.Name || nameOrSlug == OriginUserType.Slug)
		{
			if (OriginUserType.Clone() is UserType originUserType_)
			{
				originUserType_.MembershipId = membershipId;
				return originUserType_;
			}
		}
		
		if (forceGetFreshData)
		{
			return await this.GetAsync(x => x.Name == nameOrSlug || x.Slug == nameOrSlug, membershipId, cancellationToken: cancellationToken);
		}
		else
		{
			var cacheKey = GetCacheKey(membershipId, nameOrSlug);
			if (!this._memoryCache.TryGetValue<UserType>(cacheKey, out var userType))
			{
				userType = await this.GetAsync(x => x.Name == nameOrSlug || x.Slug == nameOrSlug, membershipId, cancellationToken: cancellationToken);
				if (userType == null)
				{
					return null;
				}
				
				this._memoryCache.Set(cacheKey, userType, GetCacheTTL());
			}
			
			return userType;
		}
	}
	
    public async Task<bool> IsInheritFromAsync(string childUserTypeName, string parentUserTypeName, string membershipId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(childUserTypeName))
        {
	        throw new ArgumentNullException(nameof(childUserTypeName), "ChildUserType name is null on IsInheritFromAsync()");
        }
        
        if (string.IsNullOrEmpty(parentUserTypeName))
        {
	        throw new ArgumentNullException(nameof(parentUserTypeName), "ParentUserType name is null on IsInheritFromAsync()");
        }
		
        if (childUserTypeName == parentUserTypeName)
        {
	        return true;
        }
		
        var allUserTypes = await this.GetAsync(membershipId, null, null, cancellationToken: cancellationToken);
        var childUserType = allUserTypes.Items.FirstOrDefault(x => x.Slug == childUserTypeName);
        if (childUserType == null)
        {
	        throw ErtisAuthException.UserTypeNotFound(childUserTypeName, "slug");
        }
        
        var parentUserType = allUserTypes.Items.FirstOrDefault(x => x.Slug == parentUserTypeName);
        if (parentUserType == null)
        {
	        throw ErtisAuthException.UserTypeNotFound(parentUserTypeName, "slug");
        }
		
        return IsInheritFrom(childUserType, parentUserType, allUserTypes.Items.ToArray());
    }
	
    private static bool IsInheritFrom(UserType? childUserType, UserType parentUserType, UserType[] allUserTypes)
    {
        while (true)
        {
	        if (string.IsNullOrEmpty(childUserType?.BaseUserType))
	        {
		        return false;
	        }
	        
	        if (childUserType.BaseUserType == parentUserType.Slug)
	        {
		        return true;
	        }
			
	        childUserType = allUserTypes.FirstOrDefault(x => x.Slug == childUserType.BaseUserType);
        }
    }
    
	protected override Task<IEnumerable<string>> ValidateModelAsync(UserType model, CancellationToken cancellationToken = default)
	{
		var errorList = new List<string>();
		
		try
		{
			if (string.IsNullOrEmpty(model.Name))
			{
				errorList.Add("Name is required");
			}
			
			if (!model.ValidateSchema(out var validationException) && validationException != null)
			{
				errorList.Add(validationException.Message);
			}
			
			if (errorList.Any())
			{
				return Task.FromResult<IEnumerable<string>>(errorList);
			}
		}
		catch (Exception ex)
		{
			errorList.Add(ex.Message);
		}
		
		return Task.FromResult<IEnumerable<string>>(errorList);
	}
	
	protected override async Task<bool> IsAlreadyExistAsync(UserType model, string membershipId, UserType? exclude = null, CancellationToken cancellationToken = default)
	{
		CheckReservedUserTypeName(model.Name);
		
		if (exclude == null)
		{
			return await this.GetByNameOrSlugAsync(model.Name, membershipId, cancellationToken: cancellationToken) != null;	
		}
		else
		{
			var current = await this.GetByNameOrSlugAsync(model.Name, membershipId, cancellationToken: cancellationToken);
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
	
	private static void CheckReservedUserTypeName(string name)
	{
		var reservedNames = new[]
		{
			OriginUserType.Name
		};
		
		foreach (var reservedName in reservedNames)
		{
			if (name == reservedName)
			{
				throw ErtisAuthException.ReservedUserTypeName(reservedName);
			}
		}
	}
	
	protected override ErtisAuthException GetAlreadyExistError(UserType model)
	{
		return ErtisAuthException.UserTypeAlreadyExists(model.Name);
	}
	
	protected override ErtisAuthException GetNotFoundError(string id)
	{
		return ErtisAuthException.UserTypeNotFound(id, "id");
	}
	
	#endregion
	
	#region Create Methods
	
	public override async Task<UserType> CreateAsync(UserType model, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default)
	{
		await this._changeLock.WaitAsync(cancellationToken);
		try
		{
			var userTypes = await this.GetAllAsync(membershipId, cancellationToken: cancellationToken);
			await this.EnsureUniqueIndexesAsync(userTypes.Append(model), membershipId, cancellationToken: cancellationToken);
			
			var created = await base.CreateAsync(model, membershipId, utilizer, cancellationToken);
			await this.PurgeAllCacheAsync(membershipId, cancellationToken: cancellationToken);
			return created;
		}
		finally
		{
			// Drops the indexes which are no longer needed after the change, or which were built for a change which failed afterward.
			// Never throws; runs even when the request is cancelled, so no index is left behind.
			await this._uniqueIndexSynchronizer.SynchronizeAsync(membershipId, CancellationToken.None);
			this._changeLock.Release();
		}
	}
	
	#endregion
	
	#region Update Methods
	
	public override async Task<UserType> UpdateAsync(UserType model, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default)
	{
		await this._changeLock.WaitAsync(cancellationToken);
		try
		{
			// The name or slug may change, so the entries of the prior version are removed explicitly
			var prior = await this.GetAsync(model.Id, membershipId, cancellationToken: cancellationToken);
			await this.KeepSlugIfInUseAsync(model, prior, membershipId, cancellationToken: cancellationToken);
			
			var userTypes = await this.GetAllAsync(membershipId, cancellationToken: cancellationToken);
			await this.EnsureUniqueIndexesAsync(userTypes.Where(x => x.Id != model.Id).Append(model), membershipId, cancellationToken: cancellationToken);
			
			var updated = await base.UpdateAsync(model, membershipId, utilizer, cancellationToken);
			this.PurgeCache(prior, membershipId);
			await this.PurgeAllCacheAsync(membershipId, cancellationToken: cancellationToken);
			return updated;
		}
		finally
		{
			// Drops the indexes which are no longer needed after the change, or which were built for a change which failed afterward.
			// Never throws; runs even when the request is cancelled, so no index is left behind.
			await this._uniqueIndexSynchronizer.SynchronizeAsync(membershipId, CancellationToken.None);
			this._changeLock.Release();
		}
	}
	
	/// <summary>
	/// Users (user_type) and inherited user types (baseType) refer to a user type by its slug, so the slug of a user type
	/// in use can not change; the name still can. A slug which is not sent is derived from the name, so the prior slug
	/// is kept instead of rejecting the update.
	/// </summary>
	private async Task KeepSlugIfInUseAsync(UserType model, UserType? prior, string membershipId, CancellationToken cancellationToken = default)
	{
		if (prior == null || model.Slug == prior.Slug)
		{
			return;
		}
		
		var usages = await this.CheckUsagesAsync(prior, membershipId, cancellationToken: cancellationToken);
		if (usages.Count > 0)
		{
			model.Slug = prior.Slug;
		}
	}
	
	#endregion
	
	#region Delete Methods
	
	public override async Task<bool> DeleteAsync(string id, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default)
	{
		// Is Deletable?
		var errors = await this.CheckDeletableAsync(id, membershipId, cancellationToken: cancellationToken);
		if (errors != null && errors.Any())
		{
			throw ErtisAuthException.UserTypeCanNotBeDelete();
		}
		
		await this._changeLock.WaitAsync(cancellationToken);
		try
		{
			// The deleted user type is no longer listed by PurgeAllCacheAsync, so its own entries are removed explicitly
			var prior = await this.GetAsync(id, membershipId, cancellationToken: cancellationToken);
			var isDeleted = await base.DeleteAsync(id, membershipId, utilizer, cancellationToken);
			this.PurgeCache(prior, membershipId);
			await this.PurgeAllCacheAsync(membershipId, cancellationToken: cancellationToken);
			return isDeleted;
		}
		finally
		{
			// Drops the indexes which are no longer needed after the change, or which were built for a change which failed afterward.
			// Never throws; runs even when the request is cancelled, so no index is left behind.
			await this._uniqueIndexSynchronizer.SynchronizeAsync(membershipId, CancellationToken.None);
			this._changeLock.Release();
		}
	}
	
	private async Task<IEnumerable<string>?> CheckDeletableAsync(string id, string membershipId, CancellationToken cancellationToken = default)
	{
		if (id == OriginUserType.Id)
		{
			return new [] { "Origin user-type is immutable, you can not delete it." };
		}
		
		var userType = await this.GetAsync(id, membershipId, cancellationToken: cancellationToken);
		if (userType == null)
		{
			throw ErtisAuthException.UserTypeNotFound(id, "_id");
		}
		
		var usages = await this.CheckUsagesAsync(userType, membershipId, cancellationToken: cancellationToken);
		return usages.Count > 0 ? usages : null;
	}
	
	/// <summary>
	/// Lists what refers to the user type by its slug: inherited user types (baseType) and users (user_type).
	/// </summary>
	private async Task<List<string>> CheckUsagesAsync(UserType userType, string membershipId, CancellationToken cancellationToken = default)
	{
		var usages = new List<string>();
		
		// Typed query: the field name comes from the BSON mapping of UserType.BaseUserType ("baseType")
		var inheritedUserTypes = await this._repository.FindAsync(x => x.MembershipId == membershipId && x.BaseUserType == userType.Slug, sorting: null, cancellationToken: cancellationToken);
		if (inheritedUserTypes.Items.Any())
		{
			usages.Add($"This user type is currently using as the base type of some other user types. ({string.Join(", ", inheritedUserTypes.Items.Select(x => x.Name))})");
		}
		
		var usersQuery = QueryBuilder.Where(QueryBuilder.Equals("membership_id", membershipId), QueryBuilder.Equals("user_type", userType.Slug));
		var userCount = await this._userRepository.CountAsync(usersQuery.ToString(), cancellationToken: cancellationToken);
		if (userCount > 0)
		{
			usages.Add($"This user type is currently used by {userCount} user(s).");
		}
		
		return usages;
	}
	
	#endregion
	
	#region Unique Index Methods
	
	private async Task<UserType[]> GetAllAsync(string membershipId, CancellationToken cancellationToken = default)
	{
		return (await this.GetAsync(membershipId, null, null, cancellationToken: cancellationToken)).Items.ToArray();
	}
	
	/// <summary>
	/// Builds the unique indexes needed by the user types after the change, before the change is saved:
	/// a field can not become unique while the users already have duplicate values for it.
	/// </summary>
	private async Task EnsureUniqueIndexesAsync(IEnumerable<UserType> userTypes, string membershipId, CancellationToken cancellationToken = default)
	{
		await this._uniqueIndexSynchronizer.EnsureIndexesAsync(userTypes, membershipId, cancellationToken: cancellationToken);
	}
	
	#endregion
	
	#region Cache Methods
	
	private static string GetCacheKey(string membershipId, string userTypeNameOrSlug)
	{
		return $"{CACHE_KEY}.{membershipId}.{userTypeNameOrSlug}";
	}
	
	private static MemoryCacheEntryOptions GetCacheTTL()
	{
		return new MemoryCacheEntryOptions().SetAbsoluteExpiration(CacheDefaults.UserTypesCacheTTL);
	}
	
	private async Task PurgeAllCacheAsync(string membershipId, CancellationToken cancellationToken = default)
	{
		var userTypes = await this.GetAsync(membershipId, null, null, cancellationToken: cancellationToken);
		foreach (var userType in userTypes.Items)
		{
			this.PurgeCache(userType, membershipId);
		}
	}
	
	/// <summary>
	/// User types are cached by both name and slug.
	/// </summary>
	private void PurgeCache(UserType? userType, string membershipId)
	{
		if (userType == null)
		{
			return;
		}
		
		this._memoryCache.Remove(GetCacheKey(membershipId, userType.Name));
		this._memoryCache.Remove(GetCacheKey(membershipId, userType.Slug));
	}
	
	#endregion
}