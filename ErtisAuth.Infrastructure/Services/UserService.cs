using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Core.Models.Resources;
using Ertis.MongoDB.Queries;
using Ertis.Schema.Dynamics;
using Ertis.Schema.Exceptions;
using Ertis.Schema.Extensions;
using Ertis.Schema.Types.CustomTypes;
using Ertis.Schema.Validation;
using Ertis.Security.Cryptography;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Users;
using ErtisAuth.Events.EventArgs;
using ErtisAuth.Identity.Jwt.Services.Interfaces;
using ErtisAuth.Infrastructure.Constants;
using ErtisAuth.Infrastructure.Mapping.Extensions;
using ErtisAuth.Integrations.OAuth.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Infrastructure.Services
{
    public class UserService : DynamicObjectCrudService, IUserService
    {
	    #region Constants

	    private const string CACHE_KEY = "users";
	    private static readonly TimeSpan ACTIVATION_TOKEN_TTL = TimeSpan.FromHours(72);
	    private static readonly TimeSpan RESET_PASSWORD_TOKEN_TTL = TimeSpan.FromHours(2);
	    
	    #endregion
	    
        #region Services
        
        private readonly IUserTypeService _userTypeService;
        private readonly IMembershipService _membershipService;
        private readonly IRoleService _roleService;
        private readonly IAccessControlService _accessControlService;
        private readonly IEventService _eventService;
        private readonly IJwtService _jwtService;
        private readonly ICryptographyService _cryptographyService;
        private readonly IMailHookService _mailHookService;
        private readonly IMemoryCache _memoryCache;
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
        /// <param name="cryptographyService"></param>
        /// <param name="mailHookService"></param>
        /// <param name="repository"></param>
        /// <param name="memoryCache"></param>
        /// <param name="logger"></param>
        [SuppressMessage("ReSharper", "SuggestBaseTypeForParameterInConstructor")]
        public UserService(
            IUserTypeService userTypeService, 
            IMembershipService membershipService, 
            IRoleService roleService,
            IAccessControlService accessControlService,
            IEventService eventService,
            IJwtService jwtService,
            ICryptographyService cryptographyService,
            IMailHookService mailHookService,
            IUserRepository repository,
            IMemoryCache memoryCache,
            ILogger<UserService> logger) : base(repository)
        {
            this._userTypeService = userTypeService;
            this._membershipService = membershipService;
            this._roleService = roleService;
            this._accessControlService = accessControlService;
            this._eventService = eventService;
            this._jwtService = jwtService;
            this._cryptographyService = cryptographyService;
            this._mailHookService = mailHookService;
            this._memoryCache = memoryCache;
            this._logger = logger;
        }

        #endregion

        #region Events

        public event EventHandler<CreateResourceEventArgs<DynamicObject>> OnCreated;
        public event EventHandler<UpdateResourceEventArgs<DynamicObject>> OnUpdated;
        public event EventHandler<DeleteResourceEventArgs<DynamicObject>> OnDeleted;

        #endregion
        
        #region Event Methods

        private async Task FireOnCreatedEvent(string membershipId, Utilizer utilizer, DynamicObject inserted)
        {
            if (this._eventService != null)
            {
                await this._eventService.FireEventAsync(this, new ErtisAuthEvent
                {
                    Document = inserted.ToDynamic(),
                    Prior = null,
                    EventTime = DateTime.Now,
                    EventType = ErtisAuthEventType.UserCreated,
                    MembershipId = membershipId,
                    UtilizerId = utilizer.Id
                });	
            }
            
            this.OnCreated?.Invoke(this, new CreateResourceEventArgs<DynamicObject>(utilizer, inserted, membershipId));
        }
		
        private async Task FireOnUpdatedEvent(string membershipId, Utilizer utilizer, DynamicObject prior, DynamicObject updated)
        {
            if (this._eventService != null)
            {
                await this._eventService.FireEventAsync(this, new ErtisAuthEvent
                {
                    Document = updated.ToDynamic(),
                    Prior = prior.ToDynamic(),
                    EventTime = DateTime.Now,
                    EventType = ErtisAuthEventType.UserUpdated,
                    MembershipId = membershipId,
                    UtilizerId = utilizer.Id
                });	
            }
            
            this.OnUpdated?.Invoke(this, new UpdateResourceEventArgs<DynamicObject>(utilizer, prior, updated, membershipId));
        }
		
        private async Task FireOnDeletedEvent(string membershipId, Utilizer utilizer, DynamicObject deleted)
        {
            if (this._eventService != null)
            {
                await this._eventService.FireEventAsync(this, new ErtisAuthEvent
                {
                    Document = null,
                    Prior = deleted.ToDynamic(),
                    EventTime = DateTime.Now,
                    EventType = ErtisAuthEventType.UserDeleted,
                    MembershipId = membershipId,
                    UtilizerId = utilizer.Id
                });	
            }
            
            this.OnDeleted?.Invoke(this, new DeleteResourceEventArgs<DynamicObject>(utilizer, deleted, membershipId));
        }

        #endregion

        #region Id Methods

        private void EnsureId(DynamicObject model)
        {
	        if (model.TryGetValue("_id", out string id, out _) && string.IsNullOrEmpty(id))
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

        private async Task<UserType> GetUserTypeAsync(DynamicObject model, DynamicObject current, string membershipId, bool fallbackWithOriginUserType = false, CancellationToken cancellationToken = default)
        {
	        if (model.TryGetValue("user_type", out string userTypeName, out _) && !string.IsNullOrEmpty(userTypeName))
            {
	            var userType = await this._userTypeService.GetByNameOrSlugAsync(membershipId, userTypeName, cancellationToken: cancellationToken);
	            if (userType == null)
	            {
		            throw ErtisAuthException.UserTypeNotFound(userTypeName, "name");
	            }

	            return userType;
            }
	        else if (current != null && current.TryGetValue("user_type", out string currentUserTypeName, out _) && !string.IsNullOrEmpty(currentUserTypeName))
	        {
		        var userType = await this._userTypeService.GetByNameOrSlugAsync(membershipId, currentUserTypeName, cancellationToken: cancellationToken);
		        if (userType == null)
		        {
			        throw ErtisAuthException.UserTypeNotFound(userTypeName, "name");
		        }

		        return userType;
	        }
            else if (fallbackWithOriginUserType)
            {
	            return await this._userTypeService.GetByNameOrSlugAsync(membershipId, UserType.ORIGIN_USER_TYPE_SLUG, cancellationToken: cancellationToken);
            }
	        else
	        {
		        throw ErtisAuthException.UserTypeRequired();
	        }
        }
        
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private async Task EnsureUserTypeAsync(string membershipId, UserType userType, DynamicObject model, string userId, string currentUserTypeSlug)
        {
            // Check IsAbstract
            if (userType.IsAbstract)
            {
                throw ErtisAuthException.InheritedTypeIsAbstract(userType.Name);
            }
                    
            // User type can not changed
            if (!string.IsNullOrEmpty(currentUserTypeSlug) && currentUserTypeSlug != userType.Slug)
            {
                throw ErtisAuthException.UserTypeImmutable();
            }

            // User model validation
            var validationContext = new FieldValidationContext(model);
            if (!userType.ValidateContent(model, validationContext) || !await this.CheckUniquePropertiesAsync(membershipId, userType, model, userId, validationContext))
            {
                throw new CumulativeValidationException(validationContext.Errors);
            }
        }

        private async Task<bool> CheckUniquePropertiesAsync(string membershipId, UserType userType, DynamicObject model, string userId, IValidationContext validationContext)
        {
            var isValid = true;
            var uniqueProperties = userType.GetUniqueProperties();
            foreach (var uniqueProperty in uniqueProperties)
            {
	            var path = uniqueProperty.GetSelfPath(userType);
	            if (model.TryGetValue(path, out var value, out _) && value != null)
                {
                    var found = await this.FindOneAsync(
                        QueryBuilder.Equals("membership_id", membershipId),
                        QueryBuilder.Equals(path, value));

                    if (found != null && found.TryGetValue(path, out var value_, out _) && value.Equals(value_))
                    {
                        if (string.IsNullOrEmpty(userId) || found.TryGetValue("_id", out string foundId, out _) && userId != foundId)
                        {
                            isValid = false;
                            validationContext.Errors.Add(new FieldValidationException($"The '{uniqueProperty.Name}' field has unique constraint. The same value is already using in another user.", uniqueProperty));   
                        }
                    }
                }
            }

            return isValid;
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
	        if (model.TryGetValue("permissions", out string[] permissions, out _) && permissions != null)
	        {
		        foreach (var permission in permissions)
		        {
			        var ubac = Ubac.Parse(permission);
			        permissionList.Add(ubac);
		        }
	        }
				
	        var forbiddenList = new List<Ubac>();
	        if (model.TryGetValue("forbidden", out string[] forbiddens, out _) && forbiddens != null)
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
                                    if (referenceItem.TryGetValue("user_type", out string referenceItemUserType, out _))
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
                                        if (referenceItem.TryGetValue("user_type", out string referenceItemUserType, out _))
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

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void EnsureSys(DynamicObject model, Utilizer utilizer)
        {
            var now = DateTime.Now.ToLocalTime().Add(TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow));
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
                    CreatedBy = utilizerName,
                };
            }

            model.SetValue("sys", sys.ToDictionary(), true);
        }

        #endregion
        
        #region Password Methods

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private string GetPassword(DynamicObject model)
        {
	        model.TryGetValue("password", out string password, out _);
	        return password;
        }
        
        private void EnsurePassword(DynamicObject model, out string password)
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
        
        [SuppressMessage("Performance", "CA1822:Mark members as static")]
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
		        model.SetValue("password_hash", this._cryptographyService.CalculatePasswordHash(membership, password), true);
	        }
        }
        
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void HidePasswordHash(DynamicObject model)
        {
	        model.RemoveProperty("password_hash");
        }

        #endregion

        #region Provider Methods

        private KnownProviders GetSourceProvider(DynamicObject model)
        {
	        try
	        {
		        if (!model.ContainsProperty("source_provider"))
		        {
			        return KnownProviders.ErtisAuth;
		        }
		        
		        var sourceProviderName = model.GetValue<string>("source_provider");
		        return Enum.Parse<KnownProviders>(sourceProviderName);
	        }
	        catch
	        {
		        return KnownProviders.ErtisAuth;
	        }
        }

        #endregion
        
        #region Read Methods
        
        public async Task<DynamicObject> GetAsync(string membershipId, string id, CancellationToken cancellationToken = default)
        {
	        await this.CheckMembershipAsync(membershipId, cancellationToken: cancellationToken);
	        return await this.GetByIdAsync(membershipId, id);
        }
        
        public async Task<User> GetFromCacheAsync(string membershipId, string id, CancellationToken cancellationToken = default)
        {
	        var cacheKey = GetCacheKey(membershipId, id);
	        if (!this._memoryCache.TryGetValue<User>(cacheKey, out var user))
	        {
		        var dynamicObject = await this.GetAsync(membershipId, id, cancellationToken: cancellationToken);
		        if (dynamicObject == null)
		        {
			        return null;
		        }
		        
		        user = dynamicObject.Deserialize<User>();
		        this._memoryCache.Set(cacheKey, user, GetCacheTTL());
	        }
			
	        return user;
        }
        
        public async Task<IPaginationCollection<DynamicObject>> GetAsync(
            string membershipId,
            int? skip = null, 
            int? limit = null, 
            bool withCount = false, 
            string orderBy = null,
            SortDirection? sortDirection = null, 
            CancellationToken cancellationToken = default)
        {
            await this.CheckMembershipAsync(membershipId, cancellationToken: cancellationToken);
            var queries = new[]
            {
                QueryBuilder.Equals("membership_id", membershipId)
            };
                
            return await base.GetAsync(queries, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
        }

        public async Task<IPaginationCollection<DynamicObject>> QueryAsync(
            string membershipId,
            string query, 
            int? skip = null, 
            int? limit = null, 
            bool? withCount = null,
            string orderBy = null, 
            SortDirection? sortDirection = null, 
            IDictionary<string, bool> selectFields = null, 
            CancellationToken cancellationToken = default)
        {
            await this.CheckMembershipAsync(membershipId, cancellationToken: cancellationToken);
            query = Helpers.QueryHelper.InjectMembershipIdToQuery<dynamic>(query, membershipId);
            return await base.QueryAsync(query, skip, limit, withCount, orderBy, sortDirection, selectFields, cancellationToken: cancellationToken);
        }

        public async Task<IPaginationCollection<DynamicObject>> SearchAsync(
	        string membershipId,
	        string keyword,
	        int? skip = null,
	        int? limit = null,
	        bool? withCount = null,
	        string orderBy = null,
	        SortDirection? sortDirection = null, 
	        CancellationToken cancellationToken = default)
        {
	        await this.CheckMembershipAsync(membershipId, cancellationToken: cancellationToken);
	        var query = QueryBuilder.And(QueryBuilder.Equals("membership_id", membershipId), QueryBuilder.FullTextSearch(keyword)).ToString();
	        return await base.QueryAsync(query, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
        }
        
        public async Task<UserWithPasswordHash> GetUserWithPasswordAsync(string membershipId, string id, CancellationToken cancellationToken = default)
        {
	        return await this.GetUserWithPasswordAsync(await this.CheckMembershipAsync(membershipId, cancellationToken: cancellationToken), id);
        }
        
        private async Task<UserWithPasswordHash> GetUserWithPasswordAsync(Membership membership, string id)
        {
	        var dynamicObject = await base.FindOneAsync(QueryBuilder.Equals("membership_id", membership.Id), QueryBuilder.Equals("_id", QueryBuilder.ObjectId(id)));
	        return dynamicObject?.Deserialize<UserWithPasswordHash>();
        }
        
        public async Task<UserWithPasswordHash> GetUserWithPasswordAsync(string membershipId, string username, string email, CancellationToken cancellationToken = default)
        {
	        var dynamicObject = await this.FindOneAsync(
		        QueryBuilder.And(
			        QueryBuilder.Equals("membership_id", membershipId), 
			        QueryBuilder.Or(
				        QueryBuilder.Equals("username", username),
				        QueryBuilder.Equals("email_address", email),
				        QueryBuilder.Equals("username", email),
				        QueryBuilder.Equals("email_address", username)
				    )
			    )
		    );
	        
	        return dynamicObject?.Deserialize<UserWithPasswordHash>();
        }
        
        private async Task<DynamicObject> GetByIdAsync(string membershipId, string userId)
        {
	        return await base.FindOneAsync(
		        QueryBuilder.Equals("membership_id", membershipId),
		        QueryBuilder.Equals("_id", QueryBuilder.ObjectId(userId))
	        );
        }
        
        private void EnsureUser(string membershipId, string userId, out DynamicObject currentUser)
        {
	        var current = this.GetByIdAsync(membershipId, userId).ConfigureAwait(false).GetAwaiter().GetResult();
	        currentUser = current ?? throw ErtisAuthException.UserNotFound(userId, "_id");
        }
        
        public async Task<User> GetByUsernameOrEmailAddressAsync(string membershipId, string usernameOrEmailAddress)
        {
	        var dynamicObject = await this.FindOneAsync(
		        QueryBuilder.And(
			        QueryBuilder.Equals("membership_id", membershipId), 
			        QueryBuilder.Or(
				        QueryBuilder.Equals("username", usernameOrEmailAddress),
				        QueryBuilder.Equals("email_address", usernameOrEmailAddress)
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
	        string id, 
	        UserType userType,
	        DynamicObject model, 
	        DynamicObject current, 
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
        
        public async Task<DynamicObject> CreateAsync(Utilizer utilizer, string membershipId, DynamicObject model, string host = null, CancellationToken cancellationToken = default)
        {
	        var membership = await this.CheckMembershipAsync(membershipId, cancellationToken: cancellationToken);
	        var activationMailHook = await this.EnsureUserActivationAsync(membership, cancellationToken: cancellationToken);
	        model.SetValue("is_active", membership.UserActivation != Status.Active, true);

	        string password = null;
	        var sourceProvider = this.GetSourceProvider(model);
	        if (sourceProvider == KnownProviders.ErtisAuth)
	        {
		        this.EnsurePassword(model, out password);    
	        }
	        
	        var userType = await this.GetUserTypeAsync(model, null, membershipId, sourceProvider == KnownProviders.ErtisAuth, cancellationToken: cancellationToken);
	        this.EnsureManagedProperties(model, membershipId);
	        await this.EnsureAndValidateAsync(utilizer, membershipId, null, userType, model, null, cancellationToken: cancellationToken);
	        
	        if (sourceProvider == KnownProviders.ErtisAuth)
	        {
		        this.SetPasswordHash(model, membership, password);
	        }
	        
	        var created = await base.CreateAsync(model, cancellationToken: cancellationToken);
            this.HidePasswordHash(created);
            if (created != null)
            {
                await this.FireOnCreatedEvent(membershipId, utilizer, created);
                
                // SendActivationMail
                await this.TrySendActivationMailAsync(created, activationMailHook, membership, host);
            }
            
            return created;
        }

        #endregion

        #region User Activation Methods
        
        public async Task<string> SendActivationMailAsync(string membershipId, string userId, string host = null, CancellationToken cancellationToken = default)
        {
	        var membership = await this.CheckMembershipAsync(membershipId, cancellationToken: cancellationToken);
	        var dynamicObject = await this.GetAsync(userId, cancellationToken);
	        if (dynamicObject == null)
	        {
		        throw ErtisAuthException.UserNotFound(userId, "_id");
	        }
	        
	        var user = dynamicObject.Deserialize<UserDto>().ToModel();
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

        private async Task TrySendActivationMailAsync(DynamicObject userDynamicObject, MailHook activationMailHook, Membership membership, string host)
        {
	        await this.TrySendActivationMailAsync(userDynamicObject.Deserialize<UserDto>().ToModel(), activationMailHook, membership, host);
        }
        
        private async Task<string> TrySendActivationMailAsync(User user, MailHook activationMailHook, Membership membership, string host)
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
        
        private async Task<MailHook> EnsureUserActivationAsync(Membership membership, CancellationToken cancellationToken = default)
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
	        
	        var tokenClaims = new TokenClaims(user.Id, user, membership, ACTIVATION_TOKEN_TTL);
	        tokenClaims.AddClaim("token_type", "activation_token");
	        var token = this._jwtService.GenerateToken(tokenClaims, HashAlgorithms.SHA2_256, Encoding.UTF8);
	        var activationToken = new ActivationToken(token, ACTIVATION_TOKEN_TTL);

	        return activationToken;
        }
        
        private string GenerateActivationLink(ActivationToken activationToken, string membershipId, string host)
        {
	        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{membershipId}:{activationToken.Token}"));
	        return $"{host.TrimEnd('/')}?uat={base64}";
        }
        
        public async Task<User> ActivateUserAsync(Utilizer utilizer, string membershipId, string activationCode, CancellationToken cancellationToken = default)
        {
	        var payload = Encoding.UTF8.GetString(Convert.FromBase64String(activationCode));
	        var parts = payload.Split(':');
	        if (parts.Length > 1)
	        {
		        if (MongoDB.Bson.ObjectId.TryParse(parts[0], out _))
		        {
			        var membershipId_ = parts[0];
			        if (membershipId == membershipId_)
			        {
				        var activationToken = string.Join(':', parts.Skip(1));
				        if (!string.IsNullOrEmpty(activationToken))
				        {
					        if (this._jwtService.TryDecodeToken(activationToken, out var securityToken))
					        {
						        var expireTime = securityToken.ValidTo.ToLocalTime();
						        if (DateTime.Now > expireTime)
						        {
							        // Token was expired!
							        throw ErtisAuthException.TokenWasExpired();	
						        }

						        var userId = securityToken.Subject;
						        var user = await this.GetAsync(membershipId, userId, cancellationToken: cancellationToken);
						        if (user != null)
						        {
							        user.SetValue("is_active", true, true);
							        var updated = await this.UpdateAsync(utilizer, membershipId, userId, user, false, cancellationToken: cancellationToken);
							        return updated?.Deserialize<User>();
						        }
						        else
						        {
							        throw ErtisAuthException.UserNotFound(userId, "_id");
						        }
					        }
				        }
			        }
		        }
	        }
		        
	        throw ErtisAuthException.InvalidToken();
        }
        
        public async Task<User> ActivateUserByIdAsync(Utilizer utilizer, string membershipId, string userId, CancellationToken cancellationToken = default)
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
        
        public async Task<User> FreezeUserByIdAsync(Utilizer utilizer, string membershipId, string userId, CancellationToken cancellationToken = default)
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

        public async Task<DynamicObject> UpdateAsync(Utilizer utilizer, string membershipId, string userId, DynamicObject model, bool fireEvent = true, CancellationToken cancellationToken = default)
        {
	        await this.CheckMembershipAsync(membershipId, cancellationToken: cancellationToken);
	        this.EnsureUser(membershipId, userId, out var current);
	        var userType = await this.GetUserTypeAsync(model, current, membershipId, true, cancellationToken: cancellationToken);
	        this.EnsureManagedProperties(model, membershipId);
	        model = this.SyncModel(current, model);
	        await this.CheckRoleUpdatePermissionAsync(utilizer, membershipId, model, current, cancellationToken: cancellationToken);
	        this.EnsurePasswordHash(model, current);
	        await this.EnsureAndValidateAsync(utilizer, membershipId, userId, userType, model, current, cancellationToken: cancellationToken);
	        var updated = await base.UpdateAsync(userId, model, cancellationToken: cancellationToken);
	        this.HidePasswordHash(current);
	        this.HidePasswordHash(updated);
	        if (updated != null && fireEvent)
	        {
		        await this.FireOnUpdatedEvent(membershipId, utilizer, current, updated);
	        }
	        
	        this.PurgeUserCache(membershipId, userId);
            
	        return updated;
        }
        
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private DynamicObject SyncModel(DynamicObject current, DynamicObject model)
        {
	        model = current.Merge(model);
	        model.RemoveProperty("_id");

	        return model;
        }

        private async Task CheckRoleUpdatePermissionAsync(Utilizer utilizer, string membershipId, DynamicObject model, DynamicObject current, CancellationToken cancellationToken = default)
        {
	        // Is role changed?
	        var role = await this.EnsureRoleAsync(current, membershipId, cancellationToken: cancellationToken);
	        model.TryGetValue<string>("role", out var roleName);
	        if (!string.IsNullOrEmpty(roleName) && roleName != role.Slug)
	        {
		        var utilizerRole = await this._roleService.GetBySlugAsync(utilizer.Role, utilizer.MembershipId, cancellationToken: cancellationToken);
		        if (utilizerRole != null)
		        {
			        // Is authorized for user update
			        if (!this._accessControlService.HasPermission(utilizerRole, new Rbac(new RbacSegment(utilizer.Id), new RbacSegment("users"), Rbac.CrudActionSegments.Update, new RbacSegment(utilizer.Id))))
			        {
				        throw ErtisAuthException.Unauthorized("You are not authorized for this action");
			        }   
		        }
		        else
		        {
			        throw ErtisAuthException.Unauthorized("Utilizer role not found");
		        }
	        }
        }

        #endregion
        
        #region Delete Methods

        public bool Delete(Utilizer utilizer, string membershipId, string id) =>
	        this.DeleteAsync(utilizer, membershipId, id).ConfigureAwait(false).GetAwaiter().GetResult();
        
        public async ValueTask<bool> DeleteAsync(Utilizer utilizer, string membershipId, string id, CancellationToken cancellationToken = default)
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
                this.PurgeUserCache(membershipId, id);
            }
            
            return isDeleted;
        }

        public bool? BulkDelete(Utilizer utilizer, string membershipId, string[] ids) =>
			this.BulkDeleteAsync(utilizer, membershipId, ids).ConfigureAwait(false).GetAwaiter().GetResult();
        
        public async ValueTask<bool?> BulkDeleteAsync(Utilizer utilizer, string membershipId, string[] ids, CancellationToken cancellationToken = default)
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

	        foreach (var id in ids)
	        {
		        this.PurgeUserCache(membershipId, id);
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
			
			var dynamicObject = await this.GetByIdAsync(membership.Id, userId);
			if (dynamicObject == null)
			{
				throw ErtisAuthException.UserNotFound(userId, "_id");
			}

			var prior = dynamicObject.Clone();
			this.EnsureManagedProperties(dynamicObject, membershipId);
			dynamicObject = this.SyncModel(dynamicObject, dynamicObject);
			
			var passwordHash = this._cryptographyService.CalculatePasswordHash(membership, newPassword);
			dynamicObject.SetValue("password_hash", passwordHash, true);

			var updatedUser = await base.UpdateAsync(userId, dynamicObject, cancellationToken: cancellationToken);
			await this._eventService.FireEventAsync(this, new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.UserPasswordChanged,
				UtilizerId = userId,
				Document = updatedUser,
				Prior = prior,
				MembershipId = membershipId
			}, cancellationToken: cancellationToken);

			this.PurgeUserCache(membershipId, userId);
			
			return updatedUser;
		}

		#endregion

		#region Forgot Password

		public async Task<ResetPasswordToken> ResetPasswordAsync(Utilizer utilizer, string membershipId, string emailAddress, string host, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrEmpty(emailAddress))
			{
				throw ErtisAuthException.Synthetic(HttpStatusCode.BadRequest, "Email address required (email_address)", "UsernameOrEmailAddressRequired");
			}

			var membership = await this.CheckMembershipAsync(membershipId, cancellationToken: cancellationToken);
			var user = await this.GetByUsernameOrEmailAddressAsync(membershipId, emailAddress);
			if (user == null)
			{
				throw ErtisAuthException.UserNotFound(emailAddress, "email_address");
			}

			var resetPasswordToken = this.GenerateResetPasswordToken(user, membership);
			var resetPasswordLink = GenerateResetPasswordLink(
				resetPasswordToken, 
				membershipId,
				host);

			var eventPayload = new
			{
				resetPasswordToken.Token,
				resetPasswordLink,
				user,
				membership
			};
				
			await this._eventService.FireEventAsync(this, new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.UserPasswordReset,
				UtilizerId = user.Id,
				Document = eventPayload,
				MembershipId = membershipId
			}, cancellationToken: cancellationToken);
			
			await this.SendResetPasswordMailAsync(resetPasswordToken, membership, user, host, cancellationToken: cancellationToken);

			return resetPasswordToken;
		}
		
		private async Task SendResetPasswordMailAsync(ResetPasswordToken resetPasswordToken, Membership membership, User user, string host = null, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrEmpty(host))
			{
				throw ErtisAuthException.HostRequired();
			}
	        
			if (membership.MailProviders == null || !membership.MailProviders.Any())
			{
				throw ErtisAuthException.NotDefinedAnyMailProvider();
			}
	        
			var resetPasswordMailHook = await this._mailHookService.GetResetPasswordMailHookAsync(membership.Id, cancellationToken: cancellationToken);
			if (resetPasswordMailHook == null)
			{
				throw ErtisAuthException.ResetPasswordMailHookWasNotDefined();
			}
			
			var resetPasswordLink = this.GenerateResetPasswordLink(resetPasswordToken, membership.Id, host);
			this._mailHookService.SendHookMailAsync(resetPasswordMailHook, user.Id, membership.Id, new
			{
				user,
				resetPasswordLink
			}, cancellationToken: cancellationToken);
		}
		
		private ResetPasswordToken GenerateResetPasswordToken(User user, Membership membership)
		{
			var tokenClaims = new TokenClaims(Guid.NewGuid().ToString(), user, membership, RESET_PASSWORD_TOKEN_TTL);
			tokenClaims.AddClaim("token_type", "reset_token");
			
			var resetToken = this._jwtService.GenerateToken(tokenClaims, HashAlgorithms.SHA2_256, Encoding.UTF8);
			return new ResetPasswordToken(resetToken, RESET_PASSWORD_TOKEN_TTL);
		}

		private string GenerateResetPasswordLink(ResetPasswordToken resetPasswordToken, string membershipId, string host)
		{
			var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{membershipId}:{resetPasswordToken.Token}"));
			return $"{host.TrimEnd('/')}?rpt={base64}";
		}

		public async Task<User> VerifyResetTokenAsync(string membershipId, string resetToken, CancellationToken cancellationToken = default)
		{
			var payload = Encoding.UTF8.GetString(Convert.FromBase64String(resetToken));
			var parts = payload.Split(':');
			if (parts.Length > 1)
			{
				if (MongoDB.Bson.ObjectId.TryParse(parts[0], out _))
				{
					var membershipId_ = parts[0];
					if (membershipId == membershipId_)
					{
						var resetPasswordToken = string.Join(':', parts.Skip(1));
						if (!string.IsNullOrEmpty(resetPasswordToken))
						{
							if (this._jwtService.TryDecodeToken(resetPasswordToken, out var securityToken))
							{
								var expireTime = securityToken.ValidTo.ToLocalTime();
								if (DateTime.Now > expireTime)
								{
									// Token was expired!
									throw ErtisAuthException.TokenWasExpired();	
								}
				
								var dynamicObject = await this.GetAsync(membershipId, securityToken.Subject, cancellationToken: cancellationToken);
								if (dynamicObject == null)
								{
									throw ErtisAuthException.UserNotFound(securityToken.Subject, "_id");
								}
				
								return dynamicObject.Deserialize<User>();
							}
						}
					}
				}
			}
			
			throw ErtisAuthException.InvalidToken();
		}

		public async Task SetPasswordAsync(Utilizer utilizer, string membershipId, string resetToken, string usernameOrEmailAddress, string password, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrEmpty(usernameOrEmailAddress))
			{
				throw ErtisAuthException.ValidationError(new []
				{
					"Username or email required!"
				});
			}
			
			var membership = await this._membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}

			var user = await this.GetUserWithPasswordAsync(membershipId, usernameOrEmailAddress, usernameOrEmailAddress, cancellationToken: cancellationToken);
			if (user == null)
			{
				throw ErtisAuthException.UserNotFound(usernameOrEmailAddress, "username or email_address");
			}

			await this.VerifyResetTokenAsync(membershipId, resetToken, cancellationToken: cancellationToken);
			await this.ChangePasswordAsync(utilizer, membershipId, user.Id, password, cancellationToken: cancellationToken);
		}
		
		#endregion

		#region Check Password

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

			var passwordHash = this._cryptographyService.CalculatePasswordHash(membership, password);
			return !string.IsNullOrEmpty(passwordHash?.Trim()) && !string.IsNullOrEmpty(user.PasswordHash?.Trim()) && user.PasswordHash == passwordHash;
		}
		
		#endregion
		
		#region Cache Methods

		private static string GetCacheKey(string membershipId, string userId)
		{
			return $"{CACHE_KEY}.{membershipId}.{userId}";
		}
		
		private static MemoryCacheEntryOptions GetCacheTTL()
		{
			return new MemoryCacheEntryOptions().SetAbsoluteExpiration(CacheDefaults.UsersCacheTTL);
		}
		
		private void PurgeUserCache(string membershipId, string userId)
		{
			var cacheKey = GetCacheKey(membershipId, userId);
			this._memoryCache.Remove(cacheKey);
		}

		#endregion
    }
}