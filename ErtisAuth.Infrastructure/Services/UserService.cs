using Ertis.Core.Collections;
using Ertis.Core.Models.Resources;
using Ertis.MongoDB.Queries;
using Ertis.Schema.Dynamics;
using Ertis.Schema.Exceptions;
using Ertis.Schema.Extensions;
using Ertis.Schema.Types;
using Ertis.Schema.Types.CustomTypes;
using Ertis.Schema.Types.Primitives;
using Ertis.Schema.Validation;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Constants;
using ErtisAuth.Core.Events;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Extensions;
using ErtisAuth.Core.Models;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Helpers;
using ErtisAuth.Integrations.OAuth.Core;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Infrastructure.Services;

public class UserService : DynamicObjectCrudService, IUserService
{
    #region Services
    
    private readonly IUserTypeService _userTypeService;
    private readonly IMembershipService _membershipService;
    private readonly IRoleService _roleService;
    private readonly IAccessControlService _accessControlService;
    private readonly IEventService _eventService;
    private readonly IJwtService _jwtService;
    private readonly IMailHookService _mailHookService;
    private readonly ILogger<UserService> _logger;
	
    #endregion
    
    #region Constructors
    
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="userTypeService"></param>
    /// <param name="membershipService"></param>
    /// <param name="roleService"></param>
    /// <param name="accessControlService"></param>
    /// <param name="eventService"></param>
    /// <param name="jwtService"></param>
    /// <param name="mailHookService"></param>
    /// <param name="repository"></param>
    /// <param name="logger"></param>
    public UserService(
        IUserTypeService userTypeService, 
        IMembershipService membershipService, 
        IRoleService roleService,
        IAccessControlService accessControlService,
        IEventService eventService,
        IJwtService jwtService,
        IMailHookService mailHookService,
        IUserRepository repository,
        ILogger<UserService> logger) : base(repository)
    {
        this._userTypeService = userTypeService;
        this._membershipService = membershipService;
        this._roleService = roleService;
        this._accessControlService = accessControlService;
        this._eventService = eventService;
        this._jwtService = jwtService;
        this._mailHookService = mailHookService;
        this._logger = logger;
    }
	
    #endregion
	
    #region Events
	
    public event EventHandler<CreateResourceEventArgs<DynamicObject>>? OnCreated;
    public event EventHandler<UpdateResourceEventArgs<DynamicObject>>? OnUpdated;
    public event EventHandler<DeleteResourceEventArgs<DynamicObject>>? OnDeleted;
	
    #endregion
	
    #region Event Methods
	
    private async Task FireOnCreatedEvent(string membershipId, Utilizer utilizer, DynamicObject inserted)
	{
		await this._eventService.FireEventAsync(ErtisAuthEventType.UserCreated, utilizer, membershipId, inserted);
        this.OnCreated?.Invoke(this, new CreateResourceEventArgs<DynamicObject>(utilizer, inserted, membershipId));
    }
	
    private async Task FireOnUpdatedEvent(string membershipId, Utilizer utilizer, DynamicObject prior, DynamicObject updated)
    {
		await this._eventService.FireEventAsync(ErtisAuthEventType.UserUpdated, utilizer, membershipId, updated, prior);
        this.OnUpdated?.Invoke(this, new UpdateResourceEventArgs<DynamicObject>(utilizer, prior, updated, membershipId));
    }
	
    private async Task FireOnDeletedEvent(string membershipId, Utilizer utilizer, DynamicObject deleted)
    {
		await this._eventService.FireEventAsync(ErtisAuthEventType.UserDeleted, utilizer, membershipId, null, deleted);
        this.OnDeleted?.Invoke(this, new DeleteResourceEventArgs<DynamicObject>(utilizer, deleted, membershipId));
    }
	
    #endregion
	
    #region Id Methods
	
    private void EnsureId(DynamicObject model)
    {
        if (model.TryGetValue<string>("_id", out var id, out _) && string.IsNullOrEmpty(id))
        {
	        model.RemoveProperty("_id");
        }
    }
	
    #endregion
    
    #region Membership Methods
    
    private async Task<Membership> CheckMembershipAsync(string membershipId, CancellationToken cancellationToken = default)
    {
        var membership = await this._membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
        if (membership == null)
        {
            throw ErtisAuthException.MembershipNotFound(membershipId);
        }
		
        return membership;
    }
	
    private void EnsureMembershipId(DynamicObject model, string membershipId)
    {
        model.SetValue("membership_id", membershipId, true);
    }
	
    #endregion
	
    #region UserType Methods
    
    private async Task<UserType> GetUserTypeAsync(DynamicObject model, DynamicObject? current, string membershipId, bool fallbackWithOriginUserType = false, CancellationToken cancellationToken = default)
    {
        if (model.TryGetValue<string>("user_type", out var userTypeName, out _) && !string.IsNullOrEmpty(userTypeName))
        {
            var userType = await this._userTypeService.GetByNameOrSlugAsync(membershipId, userTypeName, true, cancellationToken: cancellationToken);
            if (userType == null)
            {
	            throw ErtisAuthException.UserTypeNotFound(userTypeName, "name");
            }
            
            return userType;
        }
        else if (current != null && current.TryGetValue<string>("user_type", out var currentUserTypeName, out _) && !string.IsNullOrEmpty(currentUserTypeName))
        {
	        var userType = await this._userTypeService.GetByNameOrSlugAsync(membershipId, currentUserTypeName, true, cancellationToken: cancellationToken);
	        if (userType == null)
	        {
		        throw ErtisAuthException.UserTypeNotFound(userTypeName ?? string.Empty, "name");
	        }
			
	        return userType;
        }
        else if (fallbackWithOriginUserType)
		{
			var fallbackUserType = await this._userTypeService.GetByNameOrSlugAsync(membershipId, "user", true, cancellationToken: cancellationToken);
			return fallbackUserType ?? throw ErtisAuthException.UserTypeRequired();
		}
        else
        {
	        throw ErtisAuthException.UserTypeRequired();
        }
    }
    
    // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
    /// <summary>
    /// The user type can be given by name or slug, but users always store its slug (other records refer to user types by slug).
    /// On create the resolved user type (possibly the fallback one) is always written; on update only a given value is
    /// normalized, before any comparison with the stored value, so that the same user type given by name is not seen as a change.
    /// </summary>
    private static void NormalizeUserType(DynamicObject model, UserType userType, bool isCreate = false)
    {
        if (isCreate || model.ContainsProperty("user_type"))
        {
            model.SetValue("user_type", userType.Slug, true);
        }
    }
    
    private async Task EnsureUserTypeAsync(string membershipId, UserType userType, DynamicObject model, string? userId, string? currentUserTypeSlug)
    {
        // Check IsAbstract
        if (userType.IsAbstract)
        {
            throw ErtisAuthException.InheritedTypeIsAbstract(userType.Name);
        }
		
        // User type can not be changed
        if (!string.IsNullOrEmpty(currentUserTypeSlug) && currentUserTypeSlug != userType.Slug)
        {
            var details = new Dictionary<string, object?>
            {
	            { "membershipId", membershipId },
	            { "userId", userId },
	            { "currentUserTypeSlug", currentUserTypeSlug },
	            { "userType", userType },
	            { "model", model.ToDictionary() }
            };
			
            throw ErtisAuthException.UserTypeImmutable(details);
        }
		
        // User model validation
        var validationContext = new FieldValidationContext(model);
        if (!userType.ValidateContent(model, validationContext) || !await this.CheckUniquePropertiesAsync(membershipId, userType, model, userId, validationContext))
        {
            throw new CumulativeValidationException(validationContext.Errors);
        }
    }
    
    /// <summary>
    /// Checks the unique fields in the same scope as their unique indexes (see <see cref="UniqueFieldIndexHelper"/>):
    /// the unique fields of the origin user type among all users of the membership, the others among the users of the user type
    /// declaring the field unique and its descendants. The indexes are the final guard; this check reports all violations at once.
    /// </summary>
    private async Task<bool> CheckUniquePropertiesAsync(string membershipId, UserType userType, DynamicObject model, string? userId, IValidationContext validationContext)
    {
        var isValid = true;
        IReadOnlyList<UniqueFieldIndex>? membershipIndexes = null;
        var uniqueProperties = userType.GetUniqueProperties();
        foreach (var uniqueProperty in uniqueProperties)
        {
	        var path = uniqueProperty.GetSelfPath(userType);
	        if (model.TryGetValue(path, out var value, out _) && value != null)
	        {
		        var fieldInfoType = uniqueProperty.GetType();
		        if (value is string str && string.IsNullOrEmpty(str) && (uniqueProperty.Type == FieldType.@string || fieldInfoType.IsAssignableTo(typeof(StringFieldInfo))))
		        {
			        continue;
		        }
		        
		        var queries = new List<IQuery>
		        {
			        QueryBuilder.Equals("membership_id", membershipId),
			        QueryBuilder.Equals(path, value)
		        };
		        
		        if (!UniqueFieldIndexHelper.IsGlobalPath(path))
		        {
			        membershipIndexes ??= await this.GetMembershipUniqueIndexesAsync(membershipId);
			        var index = membershipIndexes.FirstOrDefault(x => x.Path == path && x.UserTypes.Contains(userType.Slug));
			        if (index != null)
			        {
				        queries.Add(QueryBuilder.Where("user_type", [QueryBuilder.Contains(index.UserTypes)]));
			        }
		        }
		        
		        var found = await this.FindOneAsync(queries.ToArray());
		        if (found != null && found.TryGetValue(path, out var value_, out _) && value.Equals(value_))
		        {
			        if (string.IsNullOrEmpty(userId) || found.TryGetValue<string>("_id", out var foundId, out _) && userId != foundId)
			        {
				        isValid = false;
				        validationContext.Errors.Add(GetUniqueConstraintError(uniqueProperty));
			        }
		        }
	        }
        }
        
        return isValid;
    }
    
    private async Task<IReadOnlyList<UniqueFieldIndex>> GetMembershipUniqueIndexesAsync(string membershipId)
    {
        var userTypes = await this._userTypeService.GetAsync(membershipId, null, null, false, null, null);
        return UniqueFieldIndexHelper.GetMembershipIndexes(membershipId, userTypes.Items);
    }
    
    private static FieldValidationException GetUniqueConstraintError(IFieldInfo uniqueProperty)
    {
        return new FieldValidationException($"The '{uniqueProperty.Name}' field has unique constraint. The same value is already using in another user.", uniqueProperty);
    }
    
    /// <summary>
    /// A write rejected by a unique index (a concurrent write passed the uniqueness check with the same value)
    /// gets the same validation error as the uniqueness check.
    /// </summary>
    private static bool TryGetUniqueConstraintError(DuplicateKeyException exception, UserType userType, out CumulativeValidationException error)
    {
        error = null!;
        if (!UniqueFieldIndexHelper.TryParseIndexName(exception.IndexName, out _, out var path))
        {
	        return false;
        }
        
        var uniqueProperty = userType.GetUniqueProperties().FirstOrDefault(x => x.GetSelfPath(userType) == path);
        if (uniqueProperty == null)
        {
	        return false;
        }
        
        error = new CumulativeValidationException([GetUniqueConstraintError(uniqueProperty)]);
        return true;
    }
    
    private void EnsureManagedProperties(DynamicObject model, string membershipId)
    {
        model.RemoveProperty("_id");
        model.RemoveProperty("password");
        model.RemoveProperty("password_hash");
        model.RemoveProperty("membership_id");
        model.RemoveProperty("sys");
        
        model.SetValue("membership_id", membershipId, true);
    }
    
    private void EnsureEmailAddress(DynamicObject model)
    {
        if (model.TryGetValue<string>("email_address", out var emailAddress) && !string.IsNullOrEmpty(emailAddress))
        {
	        model.SetValue("email_address", emailAddress.ToLower());
        }
    }
    
    #endregion
	
    #region Role Methods
	
    private async Task<Role> EnsureRoleAsync(DynamicObject model, string membershipId, CancellationToken cancellationToken = default)
    {
        var roleSlug = model.GetValue<string>("role");
        if (string.IsNullOrEmpty(roleSlug))
        {
	        throw ErtisAuthException.RoleRequired();
        }
        
        var role = await this._roleService.GetBySlugAsync(roleSlug, membershipId, cancellationToken: cancellationToken);
        if (role == null)
        {
	        throw ErtisAuthException.RoleNotFound(roleSlug, true);
        }
		
        return role;
    }
	
    #endregion
	
    #region Ubac Methods
	
    private void EnsureUbacs(DynamicObject model)
    {
        var permissionList = new List<Ubac>();
        if (model.TryGetValue("permissions", out string[]? permissions, out _) && permissions != null)
        {
	        foreach (var permission in permissions)
	        {
		        var ubac = Ubac.Parse(permission);
		        permissionList.Add(ubac);
	        }
        }
		
        var forbiddenList = new List<Ubac>();
        if (model.TryGetValue("forbidden", out string[]? forbiddens, out _) && forbiddens != null)
        {
	        foreach (var forbidden in forbiddens)
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
			        throw ErtisAuthException.UbacsConflicted($"Permitted and forbidden sets are conflicted. The same permission is there in the both set. ('{permissionUbac}')");
		        }
	        }	
        }
    }
	
    #endregion
	
    #region Reference Methods
	
    private async Task EmbedReferencesAsync(UserType userType, DynamicObject model, CancellationToken cancellationToken = default)
    {
        var referenceProperties = userType.GetReferenceProperties();
        foreach (var referenceProperty in referenceProperties)
        {
            var path = referenceProperty.GetSelfPath(userType);
			
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (referenceProperty.ReferenceType)
            {
                case ReferenceFieldInfo.ReferenceTypes.single:
                {
                    if (model.TryGetValue(path, out var value, out _) && value is string referenceId && !string.IsNullOrEmpty(referenceId))
                    {
                        var referenceItem = await this.GetAsync(userType.MembershipId, referenceId, cancellationToken: cancellationToken);
                        if (referenceItem != null)
                        {
                            if (!string.IsNullOrEmpty(referenceProperty.ContentType))
                            {
                                if (referenceItem.TryGetValue<string>("user_type", out var referenceItemUserType, out _) && !string.IsNullOrEmpty(referenceItemUserType))
                                {
                                    if (await this._userTypeService.IsInheritFromAsync(userType.MembershipId, referenceItemUserType, referenceProperty.ContentType, cancellationToken: cancellationToken))
                                    {
                                        model.TrySetValue(path, referenceItem.ToDynamic(), out Exception _);   
                                    }
                                    else
                                    {
                                        throw new FieldValidationException(
                                            $"This reference-type field only can bind contents from '{referenceProperty.ContentType}' content-type or inherited from '{referenceProperty.ContentType}' content-type. ('{referenceProperty.Name}')",
                                            referenceProperty);
                                    }
                                }
                                else
                                {
                                    throw new FieldValidationException(
                                        $"Content type could not read for reference value '{referenceProperty.Name}'",
                                        referenceProperty);
                                }
                            }
                        }
                        else
                        {
                            throw new FieldValidationException(
                                $"Could not find any content with id '{referenceId}' for reference type '{referenceProperty.Name}'",
                                referenceProperty);
                        }
                    }
					
                    break;
                }
                case ReferenceFieldInfo.ReferenceTypes.multiple:
                {
                    if (model.TryGetValue(path, out var value, out _) && value is object[] referenceObjectIds && referenceObjectIds.Any() && referenceObjectIds.All(x => x is string))
                    {
                        var referenceIds = referenceObjectIds.Cast<string>();
                        var referenceItems = new List<object>();
                        foreach (var referenceId in referenceIds)
                        {
                            var referenceItem = await this.GetAsync(userType.MembershipId, referenceId, cancellationToken: cancellationToken);
                            if (referenceItem != null)
                            {
                                if (!string.IsNullOrEmpty(referenceProperty.ContentType))
                                {
                                    if (referenceItem.TryGetValue<string>("user_type", out var referenceItemUserType, out _) && !string.IsNullOrEmpty(referenceItemUserType))
                                    {
                                        if (await this._userTypeService.IsInheritFromAsync(userType.MembershipId, referenceItemUserType, referenceProperty.ContentType, cancellationToken: cancellationToken))
                                        {
                                            referenceItems.Add(referenceItem.ToDynamic());
                                        }
                                        else
                                        {
                                            throw new FieldValidationException(
                                                $"This reference-type field only can bind contents from '{referenceProperty.ContentType}' content-type or inherited from '{referenceProperty.ContentType}' content-type. ('{referenceProperty.Name}')",
                                                referenceProperty);
                                        }
                                    }
                                    else
                                    {
                                        throw new FieldValidationException(
                                            $"Content type could not read for reference value '{referenceProperty.Name}'",
                                            referenceProperty);
                                    }
                                }
                            }
                            else
                            {
                                throw new FieldValidationException(
                                    $"Could not find any content with id '{referenceId}' for reference type '{referenceProperty.Name}'",
                                    referenceProperty);
                            }
                        }
                        
                        model.TrySetValue(path, referenceItems.ToArray(), out _);
                    }
					
                    break;
                }
            }
        }
    }
	
    #endregion
	
    #region Sys Methods
	
    private void EnsureSys(DynamicObject model, Utilizer utilizer)
    {
        var now = DateTime.UtcNow;
        var utilizerName = utilizer.Username;
        if (utilizer.Type == Utilizer.UtilizerType.System)
        {
            utilizerName = "system";
        }
		
        if (model.TryGetValue<SysModel>("sys", out var sys, out _) && sys != null)
        {
            sys.CreatedAt ??= now;
            sys.CreatedBy ??= utilizerName;
            sys.ModifiedAt = now;
            sys.ModifiedBy = utilizerName;
        }
        else
        {
            sys = new SysModel
            {
                CreatedAt = now,
                CreatedBy = utilizerName
            };
        }
		
        model.SetValue("sys", sys.ToDictionary(), true);
    }
	
    #endregion
    
    #region Password Methods
	
    private string? GetPassword(DynamicObject model)
    {
        model.TryGetValue<string>("password", out var password, out _);
        return password;
    }
    
    private void EnsurePassword(DynamicObject model, out string? password)
    {
        password = this.GetPassword(model);
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(password.Trim()))
        {
	        throw ErtisAuthException.PasswordRequired();
        }
        else if (password.Length < 6)
        {
	        throw ErtisAuthException.PasswordMinLengthRuleError(6);
        }
    }
    
    private void EnsurePasswordHash(DynamicObject model, DynamicObject current)
    {
        if (!model.ContainsProperty("password_hash") && current.TryGetValue<string>("password_hash", out var passwordHash, out _))
        {
	        model.SetValue("password_hash", passwordHash, true);
        }
    }
    
    // ReSharper disable once MemberCanBeMadeStatic.Local
    private void SetPasswordHash(DynamicObject model, Membership membership, string password)
    {
        if (!string.IsNullOrEmpty(password))
        {
	        model.SetValue("password_hash", this.CalculatePasswordHash(membership, password), true);
        }
    }
    
	public async Task<bool> CheckPasswordAsync(Utilizer utilizer, string password, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(password))
		{
			return false;
		}
		
		var membership = await this._membershipService.GetAsync(utilizer.MembershipId, cancellationToken: cancellationToken);
		if (membership == null)
		{
			throw ErtisAuthException.MembershipNotFound(utilizer.MembershipId);
		}
		
		var user = await this.GetUserWithPasswordAsync(membership, utilizer.Id);
		if (user == null)
		{
			throw ErtisAuthException.UserNotFound(utilizer.Id, "_id");
		}
		
		return this.VerifyPassword(membership, password, user.PasswordHash);
	}
	
	public string CalculatePasswordHash(Membership membership, string password)
	{
		if (string.IsNullOrEmpty(password))
		{
			return password;
		}
		
		return PasswordHasher.HashPassword(password, membership.GetHashAlgorithm(), membership.GetEncoding());
	}
	
	public bool VerifyPassword(Membership membership, string password, string? passwordHash)
	{
		if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(passwordHash))
		{
			return false;
		}
		
		return PasswordHasher.VerifyPassword(password, passwordHash, membership.GetHashAlgorithm(), membership.GetEncoding());
	}
	
    #endregion
	
	#region Change Password
	
	public async Task<DynamicObject> ChangePasswordAsync(Utilizer utilizer, string membershipId, string userId, string newPassword, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(newPassword))
		{
			throw ErtisAuthException.ValidationError(new []
			{
				"Password can not be null or empty!"
			});
		}
		
		var membership = await this._membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
		if (membership == null)
		{
			throw ErtisAuthException.MembershipNotFound(membershipId);
		}
		
		var user = await this.GetByIdAsync(membership.Id, userId);
		if (user == null)
		{
			throw ErtisAuthException.UserNotFound(userId, "_id");
		}
		
		var prior = (DynamicObject) user.Clone();
		user.RemoveProperty("_id");
		user.RemoveProperty("password");
		user.RemoveProperty("password_hash");
		user.RemoveProperty("membership_id");
		user.SetValue("membership_id", membershipId, true);
		
		var passwordHash = this.CalculatePasswordHash(membership, newPassword);
		user.SetValue("password_hash", passwordHash, true);
		
		var updatedUser = await base.UpdateAsync(userId, user, cancellationToken: cancellationToken);
		
		// Events are readable (events.read) and forwarded to webhooks and mail hooks: no password hashes
		prior.HidePasswordHash();
		updatedUser.HidePasswordHash();
		await this._eventService.FireEventAsync(ErtisAuthEventType.UserPasswordChanged, userId, membershipId, updatedUser, prior, cancellationToken: cancellationToken);
		
		return updatedUser!;
	}
	
	#endregion
	
    #region Provider Methods
	
    private KnownProviders GetSourceProvider(DynamicObject model)
    {
        if (model.TryGetValue<string>("source_provider", out var sourceProviderName, out _) && Enum.TryParse<KnownProviders>(sourceProviderName, out var sourceProvider))
        {
	        return sourceProvider;
        }
        
        return KnownProviders.ErtisAuth;
    }
	
    #endregion
    
    #region Read Methods
    
    public async Task<DynamicObject?> GetAsync(string membershipId, string id, CancellationToken cancellationToken = default)
    {
        await this.CheckMembershipAsync(membershipId, cancellationToken: cancellationToken);
        var user = await this.GetByIdAsync(membershipId, id);
		
		user?.RemoveProperty("password_hash");
		return user;
    }
    
    public async Task<User?> GetUserAsync(string membershipId, string id, CancellationToken cancellationToken = default)
    {
        var dynamicObject = await this.GetAsync(membershipId, id, cancellationToken: cancellationToken);
        return dynamicObject?.Deserialize<User>();
    }
    
    public async Task<IPaginationCollection<DynamicObject>> GetAsync(
        string membershipId,
        int? skip = null, 
        int? limit = null, 
        bool withCount = false, 
        string? orderBy = null,
        SortDirection? sortDirection = null, 
        CancellationToken cancellationToken = default)
    {
        await this.CheckMembershipAsync(membershipId, cancellationToken: cancellationToken);
        QueryHelper.EnsureSortable(orderBy, HiddenFields);
        var queries = new[]
        {
            QueryBuilder.Equals("membership_id", membershipId)
        };
		
		var results = await base.GetAsync(queries, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
		return results.HidePasswordHash();
	}
    
    public async Task<IPaginationCollection<DynamicObject>> QueryAsync(
        string membershipId,
        string query, 
        int? skip = null, 
        int? limit = null, 
        bool? withCount = null,
        string? orderBy = null, 
        SortDirection? sortDirection = null, 
        IDictionary<string, bool>? selectFields = null, 
        string? locale = null, 
        CancellationToken cancellationToken = default)
    {
		await this.CheckMembershipAsync(membershipId, cancellationToken: cancellationToken);
        query = QueryHelper.InjectMembershipIdToQuery<dynamic>(query, membershipId, HiddenFields);
        QueryHelper.EnsureSortable(orderBy, HiddenFields);
		var results = await base.QueryAsync(query, skip, limit, withCount, orderBy, sortDirection, selectFields, language: locale, cancellationToken: cancellationToken);
		return results.HidePasswordHash();
    }
    
    public async Task<IPaginationCollection<DynamicObject>> SearchAsync(
        string membershipId,
        string keyword,
        int? skip = null,
        int? limit = null,
        bool? withCount = null,
        string? orderBy = null,
        SortDirection? sortDirection = null, 
        CancellationToken cancellationToken = default)
    {
		await this.CheckMembershipAsync(membershipId, cancellationToken: cancellationToken);
        var query = QueryHelper.FullTextSearchQuery(membershipId, keyword);
        QueryHelper.EnsureSortable(orderBy, HiddenFields);
		var results = await base.QueryAsync(query, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
		return results.HidePasswordHash();
    }
    
    public async Task<UserWithPasswordHash?> GetUserWithPasswordAsync(string membershipId, string id, CancellationToken cancellationToken = default)
    {
        return await this.GetUserWithPasswordAsync(await this.CheckMembershipAsync(membershipId, cancellationToken: cancellationToken), id);
    }
    
    private async Task<UserWithPasswordHash?> GetUserWithPasswordAsync(Membership membership, string id)
    {
        var dynamicObject = await base.FindOneAsync(QueryBuilder.Equals("membership_id", membership.Id), QueryBuilder.Equals("_id", QueryBuilder.ObjectId(id)));
        return dynamicObject?.Deserialize<UserWithPasswordHash>();
    }
    
    public async Task<UserWithPasswordHash?> GetUserWithPasswordAsync(string membershipId, string username, string email, CancellationToken cancellationToken = default)
    {
        var dynamicObject = await this.FindOneAsync(
	        QueryBuilder.And(
		        QueryBuilder.Equals("membership_id", membershipId), 
		        QueryBuilder.Or(
			        QueryBuilder.Equals("username", username),
			        QueryBuilder.Equals("email_address", email.ToLower()),
			        QueryBuilder.Equals("username", email),
			        QueryBuilder.Equals("email_address", username.ToLower())
			    )
		    )
	    );
        
        return dynamicObject?.Deserialize<UserWithPasswordHash>();
    }
    
    private async Task<DynamicObject?> GetByIdAsync(string membershipId, string userId)
    {
        return await base.FindOneAsync(
	        QueryBuilder.Equals("membership_id", membershipId),
	        QueryBuilder.Equals("_id", QueryBuilder.ObjectId(userId))
        );
    }
    
    private async Task<DynamicObject> EnsureUserAsync(string membershipId, string userId)
    {
        var current = await this.GetByIdAsync(membershipId, userId);
        return current ?? throw ErtisAuthException.UserNotFound(userId, "_id");
    }
    
    public async Task<User?> GetByUsernameOrEmailAddressAsync(string membershipId, string usernameOrEmailAddress)
    {
        var dynamicObject = await this.FindOneAsync(
	        QueryBuilder.And(
		        QueryBuilder.Equals("membership_id", membershipId), 
		        QueryBuilder.Or(
			        QueryBuilder.Equals("username", usernameOrEmailAddress),
			        QueryBuilder.Equals("email_address", usernameOrEmailAddress.ToLower())
		        )
	        )
        );
        
        return dynamicObject?.Deserialize<User>();
    }
    
    #endregion
	
    #region Validation Methods
	
    private async Task EnsureAndValidateAsync(
        Utilizer utilizer, 
        string membershipId, 
        string? id, 
        UserType userType,
        DynamicObject model, 
        DynamicObject? current, 
        CancellationToken cancellationToken = default)
    {
        this.EnsureMembershipId(model, membershipId);
        this.EnsureId(model);
        this.EnsureSys(model, utilizer);
        this.EnsureUbacs(model);
        
        await this.EnsureUserTypeAsync(membershipId, userType, model, id, current?.GetValue<string>("user_type"));
        await this.EmbedReferencesAsync(userType, model, cancellationToken: cancellationToken);
        await this.EnsureRoleAsync(model, membershipId, cancellationToken: cancellationToken);
    }
	
    #endregion
    
    #region Create Methods
    
    public async Task<DynamicObject> CreateAsync(Utilizer utilizer, string membershipId, DynamicObject model, string? host = null, CancellationToken cancellationToken = default)
    {
        var membership = await this.CheckMembershipAsync(membershipId, cancellationToken: cancellationToken);
        
        this.EnsureEmailAddress(model);
		this.EnsureServerManagedProperties(model, utilizer);
        var activationMailHook = await this.EnsureUserActivationAsync(membership, cancellationToken: cancellationToken);
        model.SetValue("is_active", membership.UserActivation != Status.Active, true);
		
        string? password = null;
        var sourceProvider = this.GetSourceProvider(model);
        if (sourceProvider == KnownProviders.ErtisAuth)
        {
	        this.EnsurePassword(model, out password);
        }
        
        var userType = await this.GetUserTypeAsync(model, null, membershipId, sourceProvider == KnownProviders.ErtisAuth, cancellationToken: cancellationToken);
        NormalizeUserType(model, userType, isCreate: true);
        this.EnsureManagedProperties(model, membershipId);
        await this.EnsureAndValidateAsync(utilizer, membershipId, null, userType, model, null, cancellationToken: cancellationToken);
        
        if (sourceProvider == KnownProviders.ErtisAuth && !string.IsNullOrEmpty(password))
        {
	        this.SetPasswordHash(model, membership, password);
        }
        
        DynamicObject created;
        try
        {
	        created = await base.CreateAsync(model, cancellationToken: cancellationToken);
        }
        catch (DuplicateKeyException ex) when (TryGetUniqueConstraintError(ex, userType, out var error))
        {
	        throw error;
        }
        
		created.HidePasswordHash();
		
		await this.FireOnCreatedEvent(membershipId, utilizer, created);
		
		// SendActivationMail
		if (activationMailHook != null)
		{
			await this.TrySendActivationMailAsync(created, activationMailHook, membership, host);
		}
        
        return created;
    }
	
    #endregion
	
    #region User Activation Methods
	
    public async Task<string?> SendActivationMailAsync(string membershipId, string userId, string? host = null, CancellationToken cancellationToken = default)
    {
        var membership = await this.CheckMembershipAsync(membershipId, cancellationToken: cancellationToken);
        var user = await this.GetUserAsync(membershipId, userId, cancellationToken);
        if (user == null)
        {
	        throw ErtisAuthException.UserNotFound(userId, "_id");
        }
        
        if (user.IsActive)
        {
	        throw ErtisAuthException.UserAlreadyActive();
        }
        
        var activationMailHook = await this.EnsureUserActivationAsync(membership, cancellationToken: cancellationToken);
        if (activationMailHook == null)
        {
	        throw ErtisAuthException.ActivationMailHookWasNotDefined();
        }
        
        return await this.TrySendActivationMailAsync(user, activationMailHook, membership, host);
    }
	
    private async Task TrySendActivationMailAsync(DynamicObject userDynamicObject, MailHook activationMailHook, Membership membership, string? host)
    {
        await this.TrySendActivationMailAsync(userDynamicObject.Deserialize<User>()!, activationMailHook, membership, host);
    }
    
    private async Task<string?> TrySendActivationMailAsync(User user, MailHook? activationMailHook, Membership membership, string? host)
    {
        try
        {
	        if (membership.UserActivation == Status.Active)
	        {
		        if (activationMailHook != null && !string.IsNullOrEmpty(host))
		        {
			        await this.SendActivationMailAsync(user, activationMailHook, membership.Id, host);
			        return user.EmailAddress;
		        }
		        else
		        {
			        this._logger.LogError("Activation mail could not be sent (ActivationMailHook: {Id}, Host: {Host})", activationMailHook?.Id, host);
		        }
	        }
        }
        catch (Exception ex)
        {
	        this._logger.LogError(ex, "Activation mail could not be sent");
        }
		
        return null;
    }
	
    private async Task SendActivationMailAsync(User user, MailHook activationMailHook, string membershipId, string host)
    {
        var activationToken = await this.GenerateActivationTokenAsync(user);
        var activationLink = this.GenerateActivationLink(activationToken, membershipId, host);
        this._mailHookService.SendHookMailAsync(activationMailHook, user.Id, membershipId, new
        {
	        user,
	        activationLink
        });
    }
    
    private async Task<MailHook?> EnsureUserActivationAsync(Membership membership, CancellationToken cancellationToken = default)
    {
        if (membership.UserActivation == Status.Active)
        {
	        if (membership.MailProviders == null || !membership.MailProviders.Any())
	        {
		        throw ErtisAuthException.NotDefinedAnyMailProvider();
	        }
	        else
	        {
		        var activationMailHook = await this._mailHookService.GetUserActivationMailHookAsync(membership.Id, cancellationToken: cancellationToken);
		        if (activationMailHook == null)
		        {
			        throw ErtisAuthException.ActivationMailHookWasNotDefined();
		        }
				
		        return activationMailHook;
	        }
        }
		
        return null;
    }
	
    private async Task<ActivationToken> GenerateActivationTokenAsync(User user, CancellationToken cancellationToken = default)
    {
        var membership = await this._membershipService.GetAsync(user.MembershipId, cancellationToken: cancellationToken);
        if (membership == null)
        {
	        throw ErtisAuthException.MembershipNotFound(user.MembershipId);
        }
        
        var tokenClaims = new TokenClaims(user.Id, user, membership, TTLs.ACTIVATION_TOKEN_TTL);
        tokenClaims.AddClaim(ActionTokens.TokenTypeClaim, ActionTokens.ActivationTokenType);
        var token = this._jwtService.GenerateToken(tokenClaims, encoding: membership.GetEncoding());
        var activationToken = new ActivationToken(token, TTLs.ACTIVATION_TOKEN_TTL);
		
        return activationToken;
    }
    
    private string GenerateActivationLink(ActivationToken activationToken, string membershipId, string host)
    {
        return ActionTokenLinkHelper.GenerateLink(host, ActionTokenLinkHelper.ActivationQueryParameter, membershipId, activationToken.Token);
    }
    
    public async Task<User?> ActivateUserAsync(Utilizer utilizer, string membershipId, string activationCode, CancellationToken cancellationToken = default)
    {
        var membership = await this.CheckMembershipAsync(membershipId, cancellationToken: cancellationToken);
        var token = ActionTokenLinkHelper.Decode(membershipId, activationCode) ?? throw ErtisAuthException.InvalidToken();
        var securityToken = await this._jwtService.ValidateActionTokenAsync(token, membership, ActionTokens.ActivationTokenType);
        
        var userId = securityToken.Subject;
        var user = await this.GetAsync(membershipId, userId, cancellationToken: cancellationToken);
        if (user == null)
        {
	        throw ErtisAuthException.UserNotFound(userId, "_id");
        }
        
        // Single use: the activation link can not be used again once the user is active
        if (user.TryGetValue<bool>("is_active", out var isActive, out _) && isActive)
        {
	        throw ErtisAuthException.UserAlreadyActive();
        }
        
        // A link issued before the last change of the user (e.g. freezing it, or the activation itself) is no longer valid
        var modifiedAt = user.Deserialize<User>()?.Sys?.ModifiedAt;
        if (modifiedAt != null && securityToken.IssuedAt < TruncateToSeconds(AsUtc(modifiedAt.Value)))
        {
	        throw ErtisAuthException.InvalidToken("Activation token is no longer valid");
        }
        
        user.SetValue("is_active", true, true);
        var updated = await this.UpdateAsync(utilizer, membershipId, userId, user, false, cancellationToken: cancellationToken);
        return updated?.Deserialize<User>();
    }
    
	private static DateTime AsUtc(DateTime dateTime)
	{
		return dateTime.Kind switch
		{
			DateTimeKind.Local => dateTime.ToUniversalTime(),
			DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
			_ => dateTime
		};
	}
	
	/// <summary>
	/// JWT times have a precision of seconds.
	/// </summary>
	private static DateTime TruncateToSeconds(DateTime dateTime)
	{
		return new DateTime(dateTime.Ticks - dateTime.Ticks % TimeSpan.TicksPerSecond, dateTime.Kind);
	}
	
    public async Task<User?> ActivateUserByIdAsync(Utilizer utilizer, string membershipId, string userId, CancellationToken cancellationToken = default)
    {
        await this.CheckMembershipAsync(membershipId, cancellationToken: cancellationToken);
        var user = await this.GetAsync(membershipId, userId, cancellationToken: cancellationToken);
        if (user != null)
        {
	        if (user.TryGetValue<bool>("is_active", out var isActive, out _) && isActive)
	        {
		        throw ErtisAuthException.UserAlreadyActive();
	        }
	        
	        user.SetValue("is_active", true, true);
	        var updated = await this.UpdateAsync(utilizer, membershipId, userId, user, cancellationToken: cancellationToken);
	        return updated?.Deserialize<User>();
        }
        else
        {
	        throw ErtisAuthException.UserNotFound(userId, "_id");
        }
    }
    
    public async Task<User?> FreezeUserByIdAsync(Utilizer utilizer, string membershipId, string userId, CancellationToken cancellationToken = default)
    {
        await this.CheckMembershipAsync(membershipId, cancellationToken: cancellationToken);
        var user = await this.GetAsync(membershipId, userId, cancellationToken: cancellationToken);
        if (user != null)
        {
	        if (user.TryGetValue<bool>("is_active", out var isActive, out _) && !isActive)
	        {
		        throw ErtisAuthException.UserAlreadyInactive();
	        }
	        
	        user.SetValue("is_active", false, true);
	        var updated = await this.UpdateAsync(utilizer, membershipId, userId, user, cancellationToken: cancellationToken);
	        return updated?.Deserialize<User>();
        }
        else
        {
	        throw ErtisAuthException.UserNotFound(userId, "_id");
        }
    }
	
    #endregion
    
    #region Update Methods
	
    public async Task<DynamicObject?> UpdateAsync(Utilizer utilizer, string membershipId, string userId, DynamicObject model, bool fireEvent = true, CancellationToken cancellationToken = default)
    {
        await this.CheckMembershipAsync(membershipId, cancellationToken: cancellationToken);
        this.EnsureEmailAddress(model);
        var current = await this.EnsureUserAsync(membershipId, userId);
		this.EnsureServerManagedProperties(model, utilizer);
        var userType = await this.GetUserTypeAsync(model, current, membershipId, cancellationToken: cancellationToken);
        NormalizeUserType(model, userType);
        this.EnsureManagedProperties(model, membershipId);
        model = this.SyncModel(current, model);
		await this.CheckPrivilegedPropertiesAsync(utilizer, userId, model, current, cancellationToken: cancellationToken);
        this.EnsurePasswordHash(model, current);
        await this.EnsureAndValidateAsync(utilizer, membershipId, userId, userType, model, current, cancellationToken: cancellationToken);
        
        DynamicObject? updated;
        try
        {
	        updated = await base.UpdateAsync(userId, model, cancellationToken: cancellationToken);
        }
        catch (DuplicateKeyException ex) when (TryGetUniqueConstraintError(ex, userType, out var error))
        {
	        throw error;
        }
        
		if (updated == null)
		{
			return null;
		}
		
		current.HidePasswordHash();
		updated.HidePasswordHash();
		
        if (fireEvent)
        {
	        await this.FireOnUpdatedEvent(membershipId, utilizer, current, updated);
        }
        
        return updated;
    }
    
    // ReSharper disable once MemberCanBeMadeStatic.Local
    private DynamicObject SyncModel(DynamicObject current, DynamicObject model)
    {
        model = current.Merge(model);
        model.RemoveProperty("_id");
		
        return model;
    }
	
	/// <summary>
	/// Properties that only the auth api itself (system utilizer, e.g. provider logins) may write.
	/// Values sent by callers are ignored: on update the current values are kept, on create the defaults are used.
	/// </summary>
	private static readonly string[] ServerManagedProperties = ["source_provider", "connected_accounts"];
	
	/// <summary>
	/// Properties whose change requires a real users.update permission on the target user (granted by role or UBAC).
	/// The own-update exception (a user updating its own profile) does not cover them.
	/// </summary>
	private static readonly string[] PrivilegedProperties = ["role", "permissions", "forbidden", "is_active", "user_type"];
	
	/// <summary>
	/// Fields never returned, which therefore can not be filtered or sorted on either (see QueryHelper).
	/// </summary>
	private static readonly IReadOnlyCollection<string> HiddenFields = ["password_hash"];
	
	private void EnsureServerManagedProperties(DynamicObject model, Utilizer utilizer)
	{
		if (utilizer.Type == Utilizer.UtilizerType.System)
		{
			return;
		}
		
		foreach (var property in ServerManagedProperties)
		{
			model.RemoveProperty(property);
		}
	}
	
	private async Task CheckPrivilegedPropertiesAsync(Utilizer utilizer, string userId, DynamicObject model, DynamicObject current, CancellationToken cancellationToken = default)
	{
		if (utilizer.Type == Utilizer.UtilizerType.System)
		{
			return;
		}
		
		var changedProperties = PrivilegedProperties.Where(x => !IsUnchanged(current, model, x)).ToArray();
		if (changedProperties.Length == 0)
		{
			return;
		}
		
		var utilizerRole = string.IsNullOrEmpty(utilizer.Role) ? null : await this._roleService.GetBySlugAsync(utilizer.Role, utilizer.MembershipId, cancellationToken: cancellationToken);
		var rbac = new Rbac(new RbacSegment(utilizer.Id), new RbacSegment("users"), Rbac.CrudActionSegments.Update, new RbacSegment(userId));
		if (!this._accessControlService.HasGrantedPermission(utilizerRole, rbac, utilizer))
		{
			throw ErtisAuthException.AccessDenied($"You are not authorized to change the following fields: {string.Join(", ", changedProperties)}");
		}
	}
	
	private static bool IsUnchanged(DynamicObject current, DynamicObject model, string property)
	{
		switch (property)
		{
			case "permissions":
			case "forbidden":
				return GetStringSet(current, property).SetEquals(GetStringSet(model, property));
			case "is_active":
				return GetBoolean(current, property) == GetBoolean(model, property);
			default:
				return GetString(current, property) == GetString(model, property);
		}
	}
	
	private static HashSet<string> GetStringSet(DynamicObject model, string property)
	{
		return model.TryGetValue(property, out string[]? values, out _) && values != null ? values.ToHashSet() : [];
	}
	
	private static bool? GetBoolean(DynamicObject model, string property)
	{
		return model.TryGetValue<bool>(property, out var value, out _) ? value : null;
	}
	
	private static string? GetString(DynamicObject model, string property)
	{
		return model.TryGetValue<string>(property, out var value, out _) ? value : null;
	}
		
    #endregion
    
    #region Delete Methods
	
    public async Task<bool> DeleteAsync(Utilizer utilizer, string membershipId, string id, CancellationToken cancellationToken = default)
    {
        var current = await this.GetAsync(membershipId, id, cancellationToken: cancellationToken);
        if (current == null)
        {
            throw ErtisAuthException.UserNotFound(id, "_id");
        }
        
        await this.CheckMembershipAsync(membershipId, cancellationToken: cancellationToken);
        
        var isDeleted = await base.DeleteAsync(id, cancellationToken: cancellationToken);
        if (isDeleted)
        {
            await this.FireOnDeletedEvent(membershipId, utilizer, current);
        }
        
        return isDeleted;
    }
	
    public async Task<bool?> BulkDeleteAsync(Utilizer utilizer, string membershipId, string[] ids, CancellationToken cancellationToken = default)
    {
        await this.CheckMembershipAsync(membershipId, cancellationToken: cancellationToken);
		
        var isAllDeleted = true;
        var isAllFailed = true;
        foreach (var id in ids)
        {
	        var isDeleted = await base.DeleteAsync(id, cancellationToken: cancellationToken);
	        isAllDeleted &= isDeleted;
	        isAllFailed &= !isDeleted;
        }
        
        if (isAllDeleted)
        {
	        return true;
        }
        else if (isAllFailed)
        {
	        return false;
        }
        else
        {
	        return null;
        }
    }
	
    #endregion
}