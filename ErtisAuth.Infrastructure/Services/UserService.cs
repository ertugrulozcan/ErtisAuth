using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
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
using ErtisAuth.Core.Helpers;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Events.EventArgs;
using ErtisAuth.Identity.Jwt.Services.Interfaces;

namespace ErtisAuth.Infrastructure.Services
{
    public class UserService : DynamicObjectCrudService, IUserService
    {
        #region Services
        
        private readonly IUserTypeService _userTypeService;
        private readonly IMembershipService _membershipService;
        private readonly IRoleService _roleService;
        private readonly IEventService _eventService;
        private readonly IJwtService _jwtService;
        private readonly ICryptographyService _cryptographyService;

        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="userTypeService"></param>
        /// <param name="membershipService"></param>
        /// <param name="roleService"></param>
        /// <param name="eventService"></param>
        /// <param name="jwtService"></param>
        /// <param name="cryptographyService"></param>
        /// <param name="repository"></param>
        [SuppressMessage("ReSharper", "SuggestBaseTypeForParameterInConstructor")]
        public UserService(
            IUserTypeService userTypeService, 
            IMembershipService membershipService, 
            IRoleService roleService,
            IEventService eventService,
            IJwtService jwtService,
            ICryptographyService cryptographyService,
            IUserRepository repository) : base(repository)
        {
            this._userTypeService = userTypeService;
            this._membershipService = membershipService;
            this._roleService = roleService;
            this._eventService = eventService;
            this._jwtService = jwtService;
            this._cryptographyService = cryptographyService;
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
            
            this.OnCreated?.Invoke(this, new CreateResourceEventArgs<DynamicObject>(utilizer, inserted));
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
            
            this.OnUpdated?.Invoke(this, new UpdateResourceEventArgs<DynamicObject>(utilizer, prior, updated));
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
            
            this.OnDeleted?.Invoke(this, new DeleteResourceEventArgs<DynamicObject>(utilizer, deleted));
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
        
        private async Task<Membership> CheckMembershipAsync(string membershipId)
        {
            var membership = await this._membershipService.GetAsync(membershipId);
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

        private async Task<UserType> GetUserTypeAsync(DynamicObject model, string membershipId, bool fallbackWithOriginUserType = false)
        {
	        if (model.TryGetValue("user_type", out string userTypeName, out _) && !string.IsNullOrEmpty(userTypeName))
            {
	            var userType = await this._userTypeService.GetByNameOrSlugAsync(membershipId, userTypeName);
	            if (userType == null)
	            {
		            throw ErtisAuthException.UserTypeNotFound(userTypeName, "name");
	            }

	            return userType;   
            }
            else if (fallbackWithOriginUserType)
            {
	            return await this._userTypeService.GetByNameOrSlugAsync(membershipId, UserType.ORIGIN_USER_TYPE_NAME);
            }
	        else
	        {
		        throw ErtisAuthException.UserTypeRequired();
	        }
        }
        
        private async Task EnsureUserTypeAsync(string membershipId, UserType userType, DynamicObject model, string userId, string currentUserTypeName)
        {
            // Check IsAbstract
            if (userType.IsAbstract)
            {
                throw ErtisAuthException.InheritedTypeIsAbstract(userType.Name);
            }
                    
            // User type can not changed
            if (!string.IsNullOrEmpty(currentUserTypeName) && currentUserTypeName != userType.Name)
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
                var path = uniqueProperty.Path;
                var segments = path.Split('.');
                if (segments.Length > 1 && segments[0] == userType.Slug)
                {
                    path = string.Join(".", segments.Skip(1));
                }

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
        
        private void ClearReadonlyProperties(DynamicObject model, UserType userType, IDictionary<string, object> dictionary)
        {
	        model.RemoveProperty("_id");
	        model.RemoveProperty("password");
	        
	        var readonlyProperties = userType.Properties.Where(x => x.IsReadonly);
	        foreach (var fieldInfo in readonlyProperties)
	        {
		        var slug = fieldInfo.Path?.Split('.').LastOrDefault();
		        model.RemoveProperty(slug);
	        }

	        if (dictionary != null)
	        {
		        foreach (var (key, value) in dictionary)
		        {
			        model.SetValue(key, value, true);
		        }
	        }
        }
        
        #endregion

        #region Role Methods

        private async Task EnsureRoleAsync(DynamicObject model, string membershipId)
        {
	        var roleName = model.GetValue<string>("role");
	        var role = await this._roleService.GetByNameAsync(roleName, membershipId);
	        if (role == null)
	        {
		        throw ErtisAuthException.RoleNotFound(roleName, true);
	        }
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

        private async Task EmbedReferencesAsync(UserType userType, DynamicObject model)
        {
            var referenceProperties = userType.GetReferenceProperties();
            foreach (var referenceProperty in referenceProperties)
            {
                var path = referenceProperty.Path;
                var segments = path.Split('.');
                if (segments.Length > 1 && segments[0] == userType.Slug)
                {
                    path = string.Join(".", segments.Skip(1));
                }

                // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                switch (referenceProperty.ReferenceType)
                {
                    case ReferenceFieldInfo.ReferenceTypes.single:
                    {
                        if (model.TryGetValue(path, out var value, out _) && value is string referenceId && !string.IsNullOrEmpty(referenceId))
                        {
                            var referenceItem = await this.GetAsync(userType.MembershipId, referenceId);
                            if (referenceItem != null)
                            {
                                if (!string.IsNullOrEmpty(referenceProperty.ContentType))
                                {
                                    if (referenceItem.TryGetValue("user_type", out string referenceItemUserType, out _))
                                    {
                                        if (await this._userTypeService.IsInheritFromAsync(userType.MembershipId, referenceItemUserType, referenceProperty.ContentType))
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
                                var referenceItem = await this.GetAsync(userType.MembershipId, referenceId);
                                if (referenceItem != null)
                                {
                                    if (!string.IsNullOrEmpty(referenceProperty.ContentType))
                                    {
                                        if (referenceItem.TryGetValue("user_type", out string referenceItemUserType, out _))
                                        {
                                            if (await this._userTypeService.IsInheritFromAsync(userType.MembershipId, referenceItemUserType, referenceProperty.ContentType))
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

            if (model.TryGetValue<SysModel>("sys", out var sys, out _) && sys != null)
            {
                sys.CreatedAt ??= now;
                sys.CreatedBy ??= utilizer.Username;
                sys.ModifiedAt = now;
                sys.ModifiedBy = utilizer.Username;
            }
            else
            {
                sys = new SysModel
                {
                    CreatedAt = now,
                    CreatedBy = utilizer.Username,
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

        #region Read Methods
        
        public async Task<DynamicObject> GetAsync(string membershipId, string id)
        {
            await this.CheckMembershipAsync(membershipId);
            return await base.FindOneAsync(QueryBuilder.Equals("membership_id", membershipId), QueryBuilder.ObjectId(id));
        }
        
        public async Task<IPaginationCollection<DynamicObject>> GetAsync(
            string membershipId,
            int? skip = null, 
            int? limit = null, 
            bool withCount = false, 
            string orderBy = null,
            SortDirection? sortDirection = null)
        {
            await this.CheckMembershipAsync(membershipId);
            var queries = new[]
            {
                QueryBuilder.Equals("membership_id", membershipId)
            };
                
            return await base.GetAsync(queries, skip, limit, withCount, orderBy, sortDirection);
        }

        public async Task<IPaginationCollection<DynamicObject>> QueryAsync(
            string membershipId,
            string query, 
            int? skip = null, 
            int? limit = null, 
            bool? withCount = null,
            string orderBy = null, 
            SortDirection? sortDirection = null, 
            IDictionary<string, bool> selectFields = null)
        {
            await this.CheckMembershipAsync(membershipId);
            query = Helpers.QueryHelper.InjectMembershipIdToQuery<dynamic>(query, membershipId);
            return await base.QueryAsync(query, skip, limit, withCount, orderBy, sortDirection, selectFields);
        }

        public async Task<IPaginationCollection<DynamicObject>> SearchAsync(
	        string membershipId,
	        string keyword,
	        int? skip = null,
	        int? limit = null,
	        bool? withCount = null,
	        string orderBy = null,
	        SortDirection? sortDirection = null)
        {
	        await this.CheckMembershipAsync(membershipId);
	        var query = QueryBuilder.And(QueryBuilder.Equals("membership_id", membershipId), QueryBuilder.FullTextSearch(keyword)).ToString();
	        return await base.QueryAsync(query, skip, limit, withCount, orderBy, sortDirection);
        }
        
        public async Task<UserWithPasswordHash> GetUserWithPasswordAsync(string membershipId, string id)
        {
	        var dynamicObject = await this.GetAsync(membershipId, id);
	        return dynamicObject?.Deserialize<UserWithPasswordHash>();
        }
        
        public async Task<UserWithPasswordHash> GetUserWithPasswordAsync(string membershipId, string username, string email)
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
        
        #endregion

        #region Validation Methods

        private async Task EnsureAndValidateAsync(
	        Utilizer utilizer, 
	        string membershipId, 
	        string id, 
	        UserType userType,
	        DynamicObject model, 
	        DynamicObject current)
        {
	        this.EnsureMembershipId(model, membershipId);
	        this.EnsureId(model);
	        this.EnsureSys(model, utilizer);
	        this.EnsureUbacs(model);
	        
	        await this.EnsureUserTypeAsync(membershipId, userType, model, id, current?.GetValue<string>("user_type"));
	        await this.EmbedReferencesAsync(userType, model);
	        await this.EnsureRoleAsync(model, membershipId);
        }

        #endregion
        
        #region Create Methods
        
        public async Task<DynamicObject> CreateAsync(Utilizer utilizer, string membershipId, DynamicObject model)
        {
	        var membership = await this.CheckMembershipAsync(membershipId);
	        var userType = await this.GetUserTypeAsync(model, membershipId, true);
	        var password = this.GetPassword(model);
	        this.ClearReadonlyProperties(model, userType, new Dictionary<string, object> { { "membership_id", membershipId } });
	        await this.EnsureAndValidateAsync(utilizer, membershipId, null, userType, model, null);
	        this.SetPasswordHash(model, membership, password);
	        var created = await base.CreateAsync(model);
            this.HidePasswordHash(created);
            if (created != null)
            {
                await this.FireOnCreatedEvent(membershipId, utilizer, created);
            }
            
            return created;
        }

        #endregion
        
        #region Update Methods

        public async Task<DynamicObject> UpdateAsync(Utilizer utilizer, string membershipId, string userId, DynamicObject model)
        {
	        await this.CheckMembershipAsync(membershipId);
	        var userType = await this.GetUserTypeAsync(model, membershipId, true);
	        this.ClearReadonlyProperties(model, userType, new Dictionary<string, object> { { "membership_id", membershipId } });
	        model = this.SyncModel(membershipId, userId, model, out var current);
	        await this.EnsureAndValidateAsync(utilizer, membershipId, userId, userType, model, current);
	        var updated = await base.UpdateAsync(userId, model);
	        this.HidePasswordHash(updated);
	        if (updated != null)
	        {
		        await this.FireOnUpdatedEvent(membershipId, utilizer, current, updated);
	        }
            
	        return updated;
        }
        
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private DynamicObject SyncModel(string membershipId, string userId, DynamicObject model, out DynamicObject current)
        {
	        current = this.GetAsync(membershipId, userId).ConfigureAwait(false).GetAwaiter().GetResult();
	        if (current == null)
	        {
		        throw ErtisAuthException.UserNotFound(userId, "_id");
	        }
	        
	        model = current.Merge(model);
	        model.RemoveProperty("_id");

	        return model;
        }

        #endregion
        
        #region Delete Methods

        public bool Delete(Utilizer utilizer, string membershipId, string id) =>
	        this.DeleteAsync(utilizer, membershipId, id).ConfigureAwait(false).GetAwaiter().GetResult();
        
        public async ValueTask<bool> DeleteAsync(Utilizer utilizer, string membershipId, string id)
        {
            var current = await this.GetAsync(membershipId, id);
            if (current == null)
            {
                throw ErtisAuthException.UserNotFound(id, "_id");
            }
            
            await this.CheckMembershipAsync(membershipId);
            
            var isDeleted = await base.DeleteAsync(id);
            if (isDeleted)
            {
                await this.FireOnDeletedEvent(membershipId, utilizer, current);
            }
            
            return isDeleted;
        }

        public bool? BulkDelete(Utilizer utilizer, string membershipId, string[] ids) =>
			this.BulkDeleteAsync(utilizer, membershipId, ids).ConfigureAwait(false).GetAwaiter().GetResult();
        
        public async ValueTask<bool?> BulkDeleteAsync(Utilizer utilizer, string membershipId, string[] ids)
        {
	        await this.CheckMembershipAsync(membershipId);

	        var isAllDeleted = true;
	        var isAllFailed = true;
	        foreach (var id in ids)
	        {
		        var isDeleted = await base.DeleteAsync(id);
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

		#region Change Password

		public async Task<DynamicObject> ChangePasswordAsync(Utilizer utilizer, string membershipId, string userId, string newPassword)
		{
			if (string.IsNullOrEmpty(newPassword))
			{
				throw ErtisAuthException.ValidationError(new []
				{
					"Password can not be null or empty!"
				});
			}
			
			var membership = await this._membershipService.GetAsync(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}

			var user = await this.GetUserWithPasswordAsync(membershipId, userId);
			if (user == null)
			{
				throw ErtisAuthException.UserNotFound(userId, "_id");
			}

			var passwordHash = this._cryptographyService.CalculatePasswordHash(membership, newPassword);
			user.PasswordHash = passwordHash;

			var updatedUser = await this.UpdateAsync(utilizer, membershipId, userId, new DynamicObject(user));
			
			await this._eventService.FireEventAsync(this, new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.UserPasswordChanged,
				UtilizerId = user.Id,
				Document = updatedUser,
				Prior = user,
				MembershipId = membershipId
			});

			return updatedUser;
		}

		#endregion

		#region Forgot Password

		public async Task<ResetPasswordToken> ResetPasswordAsync(Utilizer utilizer, string membershipId, string emailAddress, string server, string host)
		{
			if (string.IsNullOrEmpty(emailAddress))
			{
				throw ErtisAuthException.ValidationError(new []
				{
					"Username or email required!"
				});
			}
			
			var membership = await this._membershipService.GetAsync(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}

			var user = await this.GetUserWithPasswordAsync(membershipId, emailAddress, emailAddress);
			if (user == null)
			{
				throw ErtisAuthException.UserNotFound(emailAddress, "email_address");
			}

			if (utilizer.Role is ReservedRoles.Administrator or ReservedRoles.Server || utilizer.Id == user.Id)
			{
				var tokenClaims = new TokenClaims(Guid.NewGuid().ToString(), user, membership);
				tokenClaims.AddClaim("token_type", "reset_token");
				var resetToken = this._jwtService.GenerateToken(tokenClaims, HashAlgorithms.SHA2_256, Encoding.UTF8);
				var resetPasswordToken = new ResetPasswordToken(resetToken, TimeSpan.FromHours(1));

				var resetPasswordLink = GenerateResetPasswordTokenMailLink(
					emailAddress, 
					resetPasswordToken.Token, 
					membershipId,
					membership.SecretKey, 
					server, 
					host);

				var eventPayload = new
				{
					resetPasswordToken,
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
				});

				return resetPasswordToken;
			}
			else
			{
				throw ErtisAuthException.AccessDenied("Unauthorized access");
			}
		}

		private static string GenerateResetPasswordTokenMailLink(string emailAddress, string resetPasswordToken, string membershipId, string secretKey, string serverUrl, string host)
		{
			var encryptedResetPasswordToken = Identity.Cryptography.StringCipher.Encrypt(resetPasswordToken, membershipId);
			var encryptedSecretKey = Identity.Cryptography.StringCipher.Encrypt(secretKey, membershipId);
			var payloadDictionary = new Dictionary<string, string>
			{
				{ "emailAddress", emailAddress },
				{ "encryptedSecretKey", encryptedSecretKey },
				{ "serverUrl", serverUrl },
				{ "membershipId", membershipId },
				{ "encryptedResetPasswordToken", encryptedResetPasswordToken },
			};

			var encodedPayload = Identity.Cryptography.StringCipher.Encrypt(string.Join('&', payloadDictionary.Select(x => $"{x.Key}={x.Value}")), membershipId);
			var resetPasswordPayload = $"{membershipId}:{encodedPayload}";
			var urlEncodedPayload = System.Web.HttpUtility.UrlEncode(resetPasswordPayload, Encoding.ASCII);
			var resetPasswordLink = $"https://{host}/set-password?token={urlEncodedPayload}";
			return resetPasswordLink;
		}

		public async Task SetPasswordAsync(Utilizer utilizer, string membershipId, string resetToken, string usernameOrEmailAddress, string password)
		{
			if (string.IsNullOrEmpty(usernameOrEmailAddress))
			{
				throw ErtisAuthException.ValidationError(new []
				{
					"Username or email required!"
				});
			}
			
			var membership = await this._membershipService.GetAsync(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}

			var user = await this.GetUserWithPasswordAsync(membershipId, usernameOrEmailAddress, usernameOrEmailAddress);
			if (user == null)
			{
				throw ErtisAuthException.UserNotFound(usernameOrEmailAddress, "username or email_address");
			}

			if (utilizer.Role is ReservedRoles.Administrator or ReservedRoles.Server || utilizer.Id == user.Id)
			{
				if (this._jwtService.TryDecodeToken(resetToken, out var securityToken))
				{
					var expireTime = securityToken.ValidTo.ToLocalTime();
					if (DateTime.Now > expireTime)
					{
						// Token was expired!
						throw ErtisAuthException.TokenWasExpired();	
					}

					await this.ChangePasswordAsync(utilizer, membershipId, user.Id, password);
				}
				else
				{
					// Reset token could not decoded!
					throw ErtisAuthException.InvalidToken();
				}
			}
			else
			{
				throw ErtisAuthException.AccessDenied("Unauthorized access");
			}
		}

		#endregion

		#region Check Password

		public async Task<bool> CheckPasswordAsync(Utilizer utilizer, string password)
		{
			if (string.IsNullOrEmpty(password))
			{
				return false;
			}
			
			var membership = await this._membershipService.GetAsync(utilizer.MembershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(utilizer.MembershipId);
			}

			var user = await this.GetUserWithPasswordAsync(utilizer.MembershipId, utilizer.Id);
			if (user == null)
			{
				throw ErtisAuthException.UserNotFound(utilizer.Id, "_id");
			}

			var passwordHash = this._cryptographyService.CalculatePasswordHash(membership, password);
			return user.PasswordHash == passwordHash;
		}
		
		#endregion
    }
}