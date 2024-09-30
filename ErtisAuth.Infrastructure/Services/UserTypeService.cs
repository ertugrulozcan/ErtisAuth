using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ertis.MongoDB.Queries;
using Ertis.Schema.Extensions;
using Ertis.Schema.Types;
using Ertis.Schema.Types.CustomTypes;
using Ertis.Schema.Types.Primitives;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dto.Models.Users;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Events.EventArgs;
using ErtisAuth.Infrastructure.Constants;
using Microsoft.Extensions.Caching.Memory;

namespace ErtisAuth.Infrastructure.Services
{
    public class UserTypeService : MembershipBoundedCrudService<UserType, UserTypeDto>, IUserTypeService
    {
	    #region Constants

	    private const string CACHE_KEY = "user-types";

	    #endregion
	    
	    #region Services

	    private readonly IEventService eventService;
	    private readonly IMemoryCache _memoryCache;

	    #endregion
	    
        #region Fields

	    private static UserType originUserType;
	    private static ObjectFieldInfo sysFieldInfo;

	    #endregion
	    
	    #region Properties

	    private static UserType OriginUserType
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
						    IsRequired = true,
					    },
					    new StringFieldInfo
					    {
						    Name = "lastname",
						    DisplayName = "Last Name",
						    Description = "Last Name",
						    IsRequired = false,
					    },
					    new StringFieldInfo
					    {
						    Name = "username",
						    DisplayName = "Username",
						    Description = "Username",
						    IsRequired = true,
						    IsVirtual = true,
						    IsUnique = true,
					    },
					    new EmailAddressFieldInfo
					    {
						    Name = "email_address",
						    DisplayName = "Email Address",
						    Description = "Email Address",
						    IsRequired = true,
						    IsUnique = true,
					    },
					    new StringFieldInfo
					    {
						    Name = "role",
						    DisplayName = "Role",
						    Description = "Role",
						    IsRequired = true,
					    },
					    new ArrayFieldInfo
					    {
						    Name = "permissions",
						    DisplayName = "User Permissions",
						    Description = "UBAC Permissions Array",
						    IsRequired = false,
						    ItemSchema = new StringFieldInfo { Name = "ubac" },
						    UniqueItems = true,
					    },
					    new ArrayFieldInfo
					    {
						    Name = "forbidden",
						    DisplayName = "User Forbidden",
						    Description = "UBAC Forbidden Array",
						    IsRequired = false,
						    ItemSchema = new StringFieldInfo { Name = "ubac" },
						    UniqueItems = true,
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
						    IsRequired = true,
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
							    },
						    }),
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
				    }
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
        /// <param name="memoryCache"></param>
        public UserTypeService(
            IMembershipService membershipService,
            IEventService eventService,
            IUserTypeRepository repository,
            IMemoryCache memoryCache)
            : base(membershipService, repository)
        {
	        this.eventService = eventService;
	        this._memoryCache = memoryCache;
	        
	        this.OnCreated += this.UserTypeCreatedEventHandler;
	        this.OnUpdated += this.UserTypeUpdatedEventHandler;
	        this.OnDeleted += this.UserTypeDeletedEventHandler;
        }

        #endregion
        
        #region Event Handlers

        private void UserTypeCreatedEventHandler(object sender, CreateResourceEventArgs<UserType> eventArgs)
        {
	        this.eventService.FireEventAsync(this, new ErtisAuthEvent
	        {
		        EventType = ErtisAuthEventType.UserTypeCreated,
		        UtilizerId = eventArgs.Utilizer.Id,
		        Document = eventArgs.Resource,
		        MembershipId = eventArgs.MembershipId
	        });
        }
		
        private void UserTypeUpdatedEventHandler(object sender, UpdateResourceEventArgs<UserType> eventArgs)
        {
	        this.eventService.FireEventAsync(this, new ErtisAuthEvent
	        {
		        EventType = ErtisAuthEventType.UserTypeUpdated,
		        UtilizerId = eventArgs.Utilizer.Id,
		        Document = eventArgs.Updated,
		        Prior = eventArgs.Prior,
		        MembershipId = eventArgs.MembershipId
	        });
        }
		
        private void UserTypeDeletedEventHandler(object sender, DeleteResourceEventArgs<UserType> eventArgs)
        {
	        this.eventService.FireEventAsync(this, new ErtisAuthEvent
	        {
		        EventType = ErtisAuthEventType.UserTypeDeleted,
		        UtilizerId = eventArgs.Utilizer.Id,
		        Document = eventArgs.Resource,
		        MembershipId = eventArgs.MembershipId
	        });
        }

        #endregion
        
        #region Methods

        public async ValueTask<Dictionary<string, List<string>>> GetFieldInfoOwnerRelationsAsync(string membershipId, string id, CancellationToken cancellationToken = default)
        {
	        var fieldInfoOwnerRelationDictionary = new Dictionary<string, List<string>>();

	        UserType userType = null;
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
		        userType = await base.GetAsync(membershipId, id, cancellationToken: cancellationToken);
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
        
        private async ValueTask<List<UserType>> GetGenealogyAsync(UserType userType, CancellationToken cancellationToken = default)
        {
	        var ancestors = new List<UserType> { userType };
	        var pivotUserType = userType;
	        do
	        {
		        pivotUserType = await this.GetBaseUserTypeAsync(userType.MembershipId, pivotUserType.BaseUserType, cancellationToken: cancellationToken);
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

	        var baseUserType = await this.GetBaseUserTypeAsync(model.MembershipId, model.BaseUserType, cancellationToken: cancellationToken);
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
		
        private async Task<UserType> GetBaseUserTypeAsync(string membershipId, string baseUserTypeName, CancellationToken cancellationToken = default)
        {
	        if (baseUserTypeName == OriginUserType.Name || baseUserTypeName == OriginUserType.Slug)
	        {
		        if (OriginUserType.Clone() is UserType userType)
		        {
			        userType.MembershipId = membershipId;
			        return userType;
		        }
	        }
			
	        return await this.GetByNameOrSlugAsync(membershipId, baseUserTypeName, cancellationToken: cancellationToken);
        }

        public async Task<UserType> GetByNameOrSlugAsync(string membershipId, string nameOrSlug, bool forceGetFreshData = false, CancellationToken cancellationToken = default)
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
				return await this.GetAsync(membershipId, x => x.Name == nameOrSlug || x.Slug == nameOrSlug, cancellationToken: cancellationToken);
			}
			else
			{
				var cacheKey = GetCacheKey(membershipId, nameOrSlug);
				if (!this._memoryCache.TryGetValue<UserType>(cacheKey, out var userType))
				{
					userType = await this.GetAsync(membershipId, x => x.Name == nameOrSlug || x.Slug == nameOrSlug, cancellationToken: cancellationToken);
					if (userType == null)
					{
						return null;
					}

					this._memoryCache.Set(cacheKey, userType, GetCacheTTL());
				}
			
				return userType;
			}
		}

        public async Task<bool> IsInheritFromAsync(string membershipId, string childUserTypeName, string parentUserTypeName, CancellationToken cancellationToken = default)
        {
	        if (string.IsNullOrEmpty(childUserTypeName))
	        {
		        throw new ArgumentNullException(nameof(childUserTypeName),
			        "ChildUserType name is null on IsInheritFromAsync()");
	        }
	        
	        if (string.IsNullOrEmpty(parentUserTypeName))
	        {
		        throw new ArgumentNullException(nameof(parentUserTypeName),
			        "ParentUserType name is null on IsInheritFromAsync()");
	        }

	        if (childUserTypeName == parentUserTypeName)
	        {
		        return true;
	        }

	        var allUserTypes = await this.GetAsync(membershipId, null, null, false, null, null, cancellationToken: cancellationToken);
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

        private static bool IsInheritFrom(UserType childUserType, UserType parentUserType, UserType[] allUserTypes)
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
        
		protected override bool ValidateModel(UserType model, out IEnumerable<string> errors)
		{
			var errorList = new List<string>();

			try
			{
				if (string.IsNullOrEmpty(model.Name))
				{
					errorList.Add("Name is required");
				}

				if (model.Properties == null)
				{
					errorList.Add("Properties is required");
				}

				if (!model.ValidateSchema(out var validationException))
				{
					errorList.Add(validationException.Message);
				}

				if (errorList.Any())
				{
					errors = errorList;
					return false;
				}
			}
			catch (Exception ex)
			{
				errorList.Add(ex.Message);
			}

			errors = errorList;
			return !errorList.Any();
		}

		protected override bool IsAlreadyExist(UserType model, string membershipId, UserType exclude = default) =>
			this.IsAlreadyExistAsync(model, membershipId, exclude).ConfigureAwait(false).GetAwaiter().GetResult();
		
		protected override async Task<bool> IsAlreadyExistAsync(UserType model, string membershipId, UserType exclude = default)
		{
			CheckReservedUserTypeName(model.Name);
			
			if (exclude == null)
			{
				return await this.GetByNameOrSlugAsync(membershipId, model.Name) != null;	
			}
			else
			{
				var current = await this.GetByNameOrSlugAsync(membershipId, model.Name);
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
				OriginUserType.Name,
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
		
		// ReSharper disable once OutParameterValueIsAlwaysDiscarded.Local
		private bool IsDeletable(string id, string membershipId, out IEnumerable<string> errors)
		{
			if (id == OriginUserType.Id)
			{
				errors = new [] { "Origin user-type is immutable, you can not delete it." };
				return false;
			}
			
			var userType = this.GetAsync(membershipId, id).ConfigureAwait(false).GetAwaiter().GetResult();
			if (userType == null)
			{
				throw ErtisAuthException.UserTypeNotFound(id, "_id");
			}
			
			var query = QueryBuilder.Where(QueryBuilder.Equals("membership_id", membershipId), QueryBuilder.Equals("base_type", userType.Slug));
			var inheritedUserTypes = this.QueryAsync(membershipId, query.ToString()).ConfigureAwait(false).GetAwaiter().GetResult();
			if (inheritedUserTypes.Items.Any())
			{
				errors = new[]
				{
					$"This user type is currently using as the base type of some other user types. ({string.Join(", ", inheritedUserTypes.Items.Select(x => x["title"]))})"
				};
				
				return false;	
			}

			errors = null;
			return true;
		}

		#endregion
		
		#region Create Methods

		public override UserType Create(Utilizer utilizer, string membershipId, UserType model)
		{
			var created = base.Create(utilizer, membershipId, model);
			this.PurgeAllCache(membershipId);
			return created;
		}

		public override async ValueTask<UserType> CreateAsync(Utilizer utilizer, string membershipId, UserType model, CancellationToken cancellationToken = default)
		{
			var created = await base.CreateAsync(utilizer, membershipId, model, cancellationToken);
			await this.PurgeAllCacheAsync(membershipId, cancellationToken: cancellationToken);
			return created;
		}

		#endregion

		#region Update Methods

		public override UserType Update(Utilizer utilizer, string membershipId, UserType model)
		{
			var updated = base.Update(utilizer, membershipId, model);
			this.PurgeAllCache(membershipId);
			return updated;
		}

		public override async ValueTask<UserType> UpdateAsync(Utilizer utilizer, string membershipId, UserType model, CancellationToken cancellationToken = default)
		{
			var updated = await base.UpdateAsync(utilizer, membershipId, model, cancellationToken);
			await this.PurgeAllCacheAsync(membershipId, cancellationToken: cancellationToken);
			return updated;
		}

		#endregion
		
		#region Delete Methods

		public override bool Delete(Utilizer utilizer, string membershipId, string id)
		{
			// Is Deletable?
			if (!this.IsDeletable(id, membershipId, out var _))
			{
				throw ErtisAuthException.UserTypeCanNotBeDelete();
			}
			
			var isDeleted = base.Delete(utilizer, membershipId, id);
			this.PurgeAllCache(membershipId);
			return isDeleted;
		}

		public override async ValueTask<bool> DeleteAsync(Utilizer utilizer, string membershipId, string id, CancellationToken cancellationToken = default)
		{
			// Is Deletable?
			if (!this.IsDeletable(id, membershipId, out var _))
			{
				throw ErtisAuthException.UserTypeCanNotBeDelete();
			}
			
			var isDeleted = await base.DeleteAsync(utilizer, membershipId, id, cancellationToken);
			await this.PurgeAllCacheAsync(membershipId, cancellationToken: cancellationToken);
			return isDeleted;
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

		private void PurgeAllCache(string membershipId) => this.PurgeAllCacheAsync(membershipId).ConfigureAwait(false).GetAwaiter().GetResult();
		
		private async ValueTask PurgeAllCacheAsync(string membershipId, CancellationToken cancellationToken = default)
		{
			var userTypes = await this.GetAsync(
				membershipId, 
				null, null, false, null, null,
				cancellationToken: cancellationToken);
			foreach (var userType in userTypes.Items)
			{
				var cacheKey1 = GetCacheKey(membershipId, userType.Name);
				this._memoryCache.Remove(cacheKey1);
				
				var cacheKey2 = GetCacheKey(membershipId, userType.Slug);
				this._memoryCache.Remove(cacheKey2);
			}
		}

		#endregion
    }
}