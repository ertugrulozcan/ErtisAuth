using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Helpers;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Applications;
using ErtisAuth.Events.EventArgs;
using ErtisAuth.Infrastructure.Mapping;
using Microsoft.Extensions.Caching.Memory;

namespace ErtisAuth.Infrastructure.Services
{
	public class ApplicationService : MembershipBoundedCrudService<Application, ApplicationDto>, IApplicationService
	{
		#region Constants

		private const string CACHE_KEY = "applications";

		#endregion
		
		#region Services

		private readonly IRoleService roleService;
		private readonly IEventService eventService;
		private readonly IMemoryCache _memoryCache;

		#endregion
		
		#region Fields

		private Application serverApplication;

		#endregion
		
		#region Properties

		private Application ServerApplication
		{
			get
			{
				return this.serverApplication ??= new Application
				{
					Id = "ertisauth_server",
					Name = "ertisauth_server",
					Role = ReservedRoles.Server
				};
			}
		}

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
			this.roleService = roleService;
			this.eventService = eventService;
			this._memoryCache = memoryCache;
			
			this.OnCreated += this.ApplicationCreatedEventHandler;
			this.OnUpdated += this.ApplicationUpdatedEventHandler;
			this.OnDeleted += this.ApplicationDeletedEventHandler;
		}

		#endregion
		
		#region Event Handlers

		private void ApplicationCreatedEventHandler(object sender, CreateResourceEventArgs<Application> eventArgs)
		{
			this.eventService.FireEventAsync(this, new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.ApplicationCreated,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Resource,
				MembershipId = eventArgs.Utilizer.MembershipId
			});
		}
		
		private void ApplicationUpdatedEventHandler(object sender, UpdateResourceEventArgs<Application> eventArgs)
		{
			this.eventService.FireEventAsync(this, new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.ApplicationUpdated,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Updated,
				Prior = eventArgs.Prior,
				MembershipId = eventArgs.Utilizer.MembershipId
			});
		}
		
		private void ApplicationDeletedEventHandler(object sender, DeleteResourceEventArgs<Application> eventArgs)
		{
			this.eventService.FireEventAsync(this, new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.ApplicationDeleted,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Resource,
				MembershipId = eventArgs.Utilizer.MembershipId
			});
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
				var role = this.roleService.GetByName(model.Role, model.MembershipId);
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

		[SuppressMessage("ReSharper", "ConvertIfStatementToNullCoalescingAssignment")]
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
			
			if (destination.Permissions == null)
			{
				destination.Permissions = source.Permissions;
			}
			
			if (destination.Forbidden == null)
			{
				destination.Forbidden = source.Forbidden;
			}
		}

		protected override bool IsAlreadyExist(Application model, string membershipId, Application exclude = default)
		{
			if (model.Name == this.ServerApplication.Name)
			{
				return true;
			}
			
			if (exclude == null)
			{
				return this.GetApplicationByName(model.Name, membershipId) != null;	
			}
			else
			{
				var current = this.GetApplicationByName(model.Name, membershipId);
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

		protected override async Task<bool> IsAlreadyExistAsync(Application model, string membershipId, Application exclude = default)
		{
			if (model.Name == this.ServerApplication.Name)
			{
				return true;
			}
			
			if (exclude == null)
			{
				return await this.GetApplicationByNameAsync(model.Name, membershipId) != null;	
			}
			else
			{
				var current = await this.GetApplicationByNameAsync(model.Name, membershipId);
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
		
		protected override ErtisAuthException GetAlreadyExistError(Application model)
		{
			return ErtisAuthException.ApplicationWithSameNameAlreadyExists($"'{model.Name}'");
		}
		
		protected override ErtisAuthException GetNotFoundError(string id)
		{
			return ErtisAuthException.ApplicationNotFound(id);
		}

		public bool IsSystemReservedApplication(Application application)
		{
			if (application != null)
			{
				return this.ServerApplication.Id == application.Id && this.ServerApplication.Name == application.Name;
			}

			return false;
		}

		#endregion
		
		#region Cache Methods

		private static string GetCacheKey(string membershipId, string applicationId)
		{
			return $"{CACHE_KEY}.{membershipId}.{applicationId}";
		}
		
		private static MemoryCacheEntryOptions GetCacheTTL()
		{
			return new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromHours(12));
		}

		private void PurgeAllCache(string membershipId) => this.PurgeAllCacheAsync(membershipId).ConfigureAwait(false).GetAwaiter().GetResult();
		
		private async ValueTask PurgeAllCacheAsync(string membershipId, CancellationToken cancellationToken = default)
		{
			var applications = await this.GetAsync(
				membershipId, 
				null, null, false, null, null,
				cancellationToken: cancellationToken);
			foreach (var application in applications.Items)
			{
				var cacheKey1 = GetCacheKey(membershipId, application.Id);
				this._memoryCache.Remove(cacheKey1);
			}
		}

		#endregion

		#region Read Methods

		public override Application Get(string membershipId, string id)
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

		public override async ValueTask<Application> GetAsync(string membershipId, string id, CancellationToken cancellationToken = default)
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

		public Application GetById(string id)
		{
			if (id == this.ServerApplication.Id)
			{
				return this.ServerApplication;
			}
			
			var cacheKey = GetCacheKey("*", id);
			if (!this._memoryCache.TryGetValue<Application>(cacheKey, out var application))
			{
				var dto = this.repository.FindOne(x => x.Id == id);
				if (dto == null)
				{
					return null;
				}
				
				application = Mapper.Current.Map<ApplicationDto, Application>(dto);
				this._memoryCache.Set(cacheKey, application, GetCacheTTL());
			}
			
			return application;
		}

		public async ValueTask<Application> GetByIdAsync(string id, CancellationToken cancellationToken = default)
		{
			if (id == this.ServerApplication.Id)
			{
				return this.ServerApplication;
			}
			
			var cacheKey = GetCacheKey("*", id);
			if (!this._memoryCache.TryGetValue<Application>(cacheKey, out var application))
			{
				var dto = await this.repository.FindOneAsync(x => x.Id == id, cancellationToken);
				if (dto == null)
				{
					return null;
				}
				
				application = Mapper.Current.Map<ApplicationDto, Application>(dto);
				this._memoryCache.Set(cacheKey, application, GetCacheTTL());
			}
			
			return application;
		}
		
		private Application GetApplicationByName(string name, string membershipId)
		{
			if (name == this.ServerApplication.Name)
			{
				return this.ServerApplication;
			}
			
			var dto = this.repository.FindOne(x => x.Name == name && x.MembershipId == membershipId);
			if (dto == null)
			{
				return null;
			}
			
			return Mapper.Current.Map<ApplicationDto, Application>(dto);
		}
		
		private async Task<Application> GetApplicationByNameAsync(string name, string membershipId, CancellationToken cancellationToken = default)
		{
			if (name == this.ServerApplication.Name)
			{
				return this.ServerApplication;
			}
			
			var dto = await this.repository.FindOneAsync(x => x.Name == name && x.MembershipId == membershipId, cancellationToken: cancellationToken);
			return dto == null ? null : Mapper.Current.Map<ApplicationDto, Application>(dto);
		}

		#endregion
		
		#region Create Methods

		public override Application Create(Utilizer utilizer, string membershipId, Application model)
		{
			this.PurgeAllCache(membershipId);
			return base.Create(utilizer, membershipId, model);
		}

		public override async ValueTask<Application> CreateAsync(Utilizer utilizer, string membershipId, Application model, CancellationToken cancellationToken = default)
		{
			await this.PurgeAllCacheAsync(membershipId, cancellationToken: cancellationToken);
			return await base.CreateAsync(utilizer, membershipId, model, cancellationToken);
		}

		#endregion

		#region Update Methods

		public override Application Update(Utilizer utilizer, string membershipId, Application model)
		{
			this.PurgeAllCache(membershipId);
			return base.Update(utilizer, membershipId, model);
		}

		public override async ValueTask<Application> UpdateAsync(Utilizer utilizer, string membershipId, Application model, CancellationToken cancellationToken = default)
		{
			await this.PurgeAllCacheAsync(membershipId, cancellationToken: cancellationToken);
			return await base.UpdateAsync(utilizer, membershipId, model, cancellationToken);
		}

		#endregion
		
		#region Delete Methods

		public override bool Delete(Utilizer utilizer, string membershipId, string id)
		{
			this.PurgeAllCache(membershipId);
			return base.Delete(utilizer, membershipId, id);
		}

		public override async ValueTask<bool> DeleteAsync(Utilizer utilizer, string membershipId, string id, CancellationToken cancellationToken = default)
		{
			await this.PurgeAllCacheAsync(membershipId, cancellationToken: cancellationToken);
			return await base.DeleteAsync(utilizer, membershipId, id, cancellationToken);
		}
		
		#endregion
	}
}