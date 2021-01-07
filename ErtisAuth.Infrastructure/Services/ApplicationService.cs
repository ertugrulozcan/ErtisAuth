using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ertis.Core.Helpers;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Applications;
using ErtisAuth.Infrastructure.Events;
using ErtisAuth.Infrastructure.Exceptions;
using ErtisAuth.Infrastructure.Mapping;

namespace ErtisAuth.Infrastructure.Services
{
	public class ApplicationService : MembershipBoundedCrudService<Application, ApplicationDto>, IApplicationService
	{
		#region Services

		private readonly IMembershipService membershipService;
		private readonly IRoleService roleService;
		private readonly IEventService eventService;

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
			this.membershipService = membershipService;
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
			this.eventService.FireEventAsync(new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.ApplicationCreated,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Resource,
				MembershipId = eventArgs.Utilizer.MembershipId
			});
		}
		
		private void ApplicationUpdatedEventHandler(object sender, UpdateResourceEventArgs<Application> eventArgs)
		{
			this.eventService.FireEventAsync(new ErtisAuthEvent
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
			this.eventService.FireEventAsync(new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.ApplicationDeleted,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Resource,
				MembershipId = eventArgs.Utilizer.MembershipId
			});
		}

		#endregion
		
		#region Methods

		public override Application Create(Utilizer utilizer, string membershipId, Application model)
		{
			var membership = this.membershipService.Get(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			
			model.Slug = Slugifier.Slugify(model.Name);

			var application = base.Create(utilizer, membershipId, model);
			if (application != null)
			{
				application.Secret = $"{application.Id}:{membership.SecretKey}";
				application = this.Update(utilizer, membershipId, application);
			}
			
			return application;
		}
		
		public override async Task<Application> CreateAsync(Utilizer utilizer, string membershipId, Application model)
		{
			var membership = await this.membershipService.GetAsync(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			
			model.Slug = Slugifier.Slugify(model.Name);
			
			var application = await base.CreateAsync(utilizer, membershipId, model);
			if (application != null)
			{
				application.Secret = $"{application.Id}:{membership.SecretKey}";	
				application = await this.UpdateAsync(utilizer, membershipId, application);
			}

			return application;
		}

		protected override bool ValidateModel(Application model, out IEnumerable<string> errors)
		{
			var errorList = new List<string>();
			if (string.IsNullOrEmpty(model.Name))
			{
				errorList.Add("name is a required field");
			}
			
			if (string.IsNullOrEmpty(model.Slug))
			{
				errorList.Add("slug is a required field");
			}
			
			/*
			if (string.IsNullOrEmpty(model.Secret))
			{
				errorList.Add("secret is a required field");
			}
			*/

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
			
			if (string.IsNullOrEmpty(destination.Secret))
			{
				destination.Secret = source.Secret;
			}
			
			if (string.IsNullOrEmpty(destination.Slug))
			{
				destination.Slug = source.Slug;
			}
		}

		protected override bool IsAlreadyExist(Application model, string membershipId, Application exclude = default)
		{
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
			var dto = this.repository.FindOne(x => x.Id == id);
			return Mapper.Current.Map<ApplicationDto, Application>(dto);
		}

		public async Task<Application> GetByIdAsync(string id)
		{
			var dto = await this.repository.FindOneAsync(x => x.Id == id);
			return Mapper.Current.Map<ApplicationDto, Application>(dto);
		}
		
		private Application GetApplicationByName(string name, string membershipId)
		{
			var dto = this.repository.FindOne(x => x.Name == name && x.MembershipId == membershipId);
			if (dto == null)
			{
				return null;
			}
			
			return Mapper.Current.Map<ApplicationDto, Application>(dto);
		}
		
		private async Task<Application> GetApplicationByNameAsync(string name, string membershipId)
		{
			var dto = await this.repository.FindOneAsync(x => x.Name == name && x.MembershipId == membershipId);
			if (dto == null)
			{
				return null;
			}
			
			return Mapper.Current.Map<ApplicationDto, Application>(dto);
		}

		#endregion
	}
}