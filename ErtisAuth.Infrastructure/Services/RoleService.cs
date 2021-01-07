using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Roles;
using ErtisAuth.Infrastructure.Events;
using ErtisAuth.Infrastructure.Exceptions;
using ErtisAuth.Infrastructure.Mapping;

namespace ErtisAuth.Infrastructure.Services
{
	public class RoleService : MembershipBoundedCrudService<Role, RoleDto>, IRoleService
	{
		#region Services

		private readonly IMembershipService membershipService;
		private readonly IEventService eventService;

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="membershipService"></param>
		/// <param name="eventService"></param>
		/// <param name="roleRepository"></param>
		public RoleService(IMembershipService membershipService, IEventService eventService, IRoleRepository roleRepository) : base(membershipService, roleRepository)
		{
			this.membershipService = membershipService;
			this.eventService = eventService;

			this.Initialize();
			
			this.OnCreated += this.RoleCreatedEventHandler;
			this.OnUpdated += this.RoleUpdatedEventHandler;
			this.OnDeleted += this.RoleDeletedEventHandler;
		}

		#endregion
		
		#region Initialize Methods

		private void Initialize()
		{
			this.InitializeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
		}
		
		private async Task InitializeAsync()
		{
			var memberships = await this.membershipService.GetAsync(0, null, false, null, null);
			if (memberships?.Items != null)
			{
				foreach (var membership in memberships.Items)
				{
					var utilizer = new Utilizer
					{
						Role = Rbac.ReservedRoles.Administrator,
						Type = "system",
						MembershipId = membership.Id
					};
					
					var role = await this.GetByNameAsync(Rbac.ReservedRoles.Administrator, membership.Id);
					if (role == null)
					{
						await this.CreateAsync(utilizer, membership.Id, new Role
						{
							Name = Rbac.ReservedRoles.Administrator,
							Description = "Administrator",
							MembershipId = membership.Id,
							Permissions = this.AssertAdminPermissionsForReservedResources()
						});
					}
				}	
			}
		}

		private IEnumerable<string> AssertAdminPermissionsForReservedResources()
		{
			string[] reservedResources = {
				"users",
				"applications",
				"roles",
				"events"
			};

			RbacSegment[] adminPrivilages =
			{
				Rbac.CrudActionSegments.Create,
				Rbac.CrudActionSegments.Read,
				Rbac.CrudActionSegments.Update,
				Rbac.CrudActionSegments.Delete
			};

			var permissions = new List<string>();
			foreach (var resource in reservedResources)
			{
				var resourceSegment = new RbacSegment(resource);
				foreach (var privilage in adminPrivilages)
				{
					var rbac = new Rbac(RbacSegment.All, resourceSegment, privilage, RbacSegment.All);
					permissions.Add(rbac.ToString());	
				}
			}

			return permissions;
		}
		
		#endregion
		
		#region Event Handlers

		private void RoleCreatedEventHandler(object sender, CreateResourceEventArgs<Role> eventArgs)
		{
			this.eventService.FireEventAsync(new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.RoleCreated,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Resource,
				MembershipId = eventArgs.Utilizer.MembershipId
			});
		}
		
		private void RoleUpdatedEventHandler(object sender, UpdateResourceEventArgs<Role> eventArgs)
		{
			this.eventService.FireEventAsync(new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.RoleUpdated,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Updated,
				Prior = eventArgs.Prior,
				MembershipId = eventArgs.Utilizer.MembershipId
			});
		}
		
		private void RoleDeletedEventHandler(object sender, DeleteResourceEventArgs<Role> eventArgs)
		{
			this.eventService.FireEventAsync(new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.RoleDeleted,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Resource,
				MembershipId = eventArgs.Utilizer.MembershipId
			});
		}

		#endregion

		#region Methods
		
		protected override bool ValidateModel(Role model, out IEnumerable<string> errors)
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

			try
			{
				var permissionList = new List<Rbac>();
				if (model.Permissions != null)
				{
					foreach (var permission in model.Permissions)
					{
						var rbac = Rbac.Parse(permission);
						permissionList.Add(rbac);
					}
				}
				
				var forbiddenList = new List<Rbac>();
				if (model.Forbidden != null)
				{
					foreach (var forbidden in model.Forbidden)
					{
						var rbac = Rbac.Parse(forbidden);
						forbiddenList.Add(rbac);
					}
				}
				
				// Is there any conflict?
				foreach (var permissionRbac in permissionList)
				{
					foreach (var forbiddenRbac in forbiddenList)
					{
						if (permissionRbac == forbiddenRbac)
						{
							errorList.Add($"Permitted and forbidden sets are conflicted. The same permission is there in the both set. ('{permissionRbac}')");
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

		protected override void Overwrite(Role destination, Role source)
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
			
			if (destination.Description == null)
			{
				destination.Description = source.Description;
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

		protected override bool IsAlreadyExist(Role model, string membershipId, Role exclude = default)
		{
			if (exclude == null)
			{
				return this.GetByName(model.Name, membershipId) != null;	
			}
			else
			{
				var current = this.GetByName(model.Name, membershipId);
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

		protected override async Task<bool> IsAlreadyExistAsync(Role model, string membershipId, Role exclude = default)
		{
			if (exclude == null)
			{
				return await this.GetByNameAsync(model.Name, membershipId) != null;	
			}
			else
			{
				var current = await this.GetByNameAsync(model.Name, membershipId);
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
		
		protected override ErtisAuthException GetAlreadyExistError(Role model)
		{
			return ErtisAuthException.RoleWithSameNameAlreadyExists(model.Name);
		}
		
		protected override ErtisAuthException GetNotFoundError(string id)
		{
			return ErtisAuthException.RoleNotFound(id);
		}
		
		public Role GetByName(string name, string membershipId)
		{
			var dto = this.repository.FindOne(x => x.Name == name && x.MembershipId == membershipId);
			if (dto == null)
			{
				return null;
			}
			
			return Mapper.Current.Map<RoleDto, Role>(dto);
		}
		
		public async Task<Role> GetByNameAsync(string name, string membershipId)
		{
			var dto = await this.repository.FindOneAsync(x => x.Name == name && x.MembershipId == membershipId);
			if (dto == null)
			{
				return null;
			}
			
			return Mapper.Current.Map<RoleDto, Role>(dto);
		}

		#endregion
	}
}