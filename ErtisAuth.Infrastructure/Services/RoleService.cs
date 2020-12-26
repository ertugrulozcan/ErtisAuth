using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Roles;
using ErtisAuth.Infrastructure.Exceptions;
using ErtisAuth.Infrastructure.Mapping;

namespace ErtisAuth.Infrastructure.Services
{
	public class RoleService : MembershipBoundedCrudService<Role, RoleDto>, IRoleService
	{
		#region Services

		private readonly IMembershipService membershipService;

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="membershipService"></param>
		/// <param name="roleRepository"></param>
		public RoleService(IMembershipService membershipService, IRoleRepository roleRepository) : base(membershipService, roleRepository)
		{
			this.membershipService = membershipService;

			this.Initialize();
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
					var role = await this.GetByNameAsync(Rbac.ReservedRoles.Administrator, membership.Id);
					if (role == null)
					{
						await this.CreateAsync(membership.Id, new Role
						{
							Name = Rbac.ReservedRoles.Administrator,
							Description = "Administrator",
							Slug = Rbac.ReservedRoles.Administrator,
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
				"roles"
			};

			RbacSegment[] adminPrivilages =
			{
				Rbac.CrudActions.Create,
				Rbac.CrudActions.Read,
				Rbac.CrudActions.Update,
				Rbac.CrudActions.Delete
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

		#region Methods
		
		protected override bool ValidateModel(Role model, out IEnumerable<string> errors)
		{
			var errorList = new List<string>();
			if (string.IsNullOrEmpty(model.Name))
			{
				errorList.Add("name is a required field");
			}
			
			/*
			if (string.IsNullOrEmpty(model.Slug))
			{
				errorList.Add("slug is a required field");
			}
			*/
			
			if (string.IsNullOrEmpty(model.MembershipId))
			{
				errorList.Add("membership_id is a required field");
			}

			try
			{
				if (model.Permissions != null)
				{
					foreach (var permission in model.Permissions)
					{
						Rbac.Parse(permission);
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

			if (string.IsNullOrEmpty(destination.Slug))
			{
				destination.Slug = Ertis.Core.Helpers.Slugifier.Slugify(destination.Name);
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