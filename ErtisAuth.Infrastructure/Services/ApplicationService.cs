using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Helpers;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Applications;
using ErtisAuth.Events.EventArgs;
using ErtisAuth.Infrastructure.Mapping;

namespace ErtisAuth.Infrastructure.Services
{
	public class ApplicationService : MembershipBoundedCrudService<Application, ApplicationDto>, IApplicationService
	{
		#region Services

		private readonly IRoleService roleService;
		private readonly IEventService eventService;

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
		/// <param name="applicationRepository"></param>
		public ApplicationService(
			IMembershipService membershipService, 
			IRoleService roleService, 
			IEventService eventService,
			IApplicationRepository applicationRepository) : base(membershipService, applicationRepository)
		{
			this.roleService = roleService;
			this.eventService = eventService;
			
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

		public Application GetById(string id)
		{
			if (id == this.ServerApplication.Id)
			{
				return this.ServerApplication;
			}
			
			var dto = this.repository.FindOne(x => x.Id == id);
			return Mapper.Current.Map<ApplicationDto, Application>(dto);
		}

		public async ValueTask<Application> GetByIdAsync(string id)
		{
			if (id == this.ServerApplication.Id)
			{
				return this.ServerApplication;
			}
			
			var dto = await this.repository.FindOneAsync(x => x.Id == id);
			return Mapper.Current.Map<ApplicationDto, Application>(dto);
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
		
		private async Task<Application> GetApplicationByNameAsync(string name, string membershipId)
		{
			if (name == this.ServerApplication.Name)
			{
				return this.ServerApplication;
			}
			
			var dto = await this.repository.FindOneAsync(x => x.Name == name && x.MembershipId == membershipId);
			if (dto == null)
			{
				return null;
			}
			
			return Mapper.Current.Map<ApplicationDto, Application>(dto);
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
	}
}