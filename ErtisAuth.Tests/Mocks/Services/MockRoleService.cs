using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Core.Models.Resources;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;

namespace ErtisAuth.Tests.Mocks.Services
{
	public class MockRoleService : IRoleService
	{
		#region Properties

		private List<Role> MockRepository { get; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		public MockRoleService()
		{
			this.MockRepository = new List<Role>();
			this.PopulateMockRepository();
		}

		#endregion

		private void PopulateMockRepository()
		{
			const string membershipId = "test_membership";
			var utilizer = new Utilizer
			{
				Id = "test_utilizer",
				Role = "admin",
				Type = Utilizer.UtilizerType.System,
				Username = "admin",
				MembershipId = membershipId,
				Token = "",
				TokenType = SupportedTokenTypes.None
			};

			// 1. Admin Role
			this.Create(utilizer, membershipId, new Role
			{
				Id = "admin_role",
				Name = "admin",
				Description = "System Administrator",
				MembershipId = membershipId,
				Permissions = new []
				{
					"*.memberships.create.*", 
			        "*.memberships.read.*", 
			        "*.memberships.update.*", 
			        "*.memberships.delete.*", 
			        "*.users.create.*", 
			        "*.users.read.*", 
			        "*.users.update.*", 
			        "*.users.delete.*", 
			        "*.applications.create.*", 
			        "*.applications.read.*", 
			        "*.applications.update.*", 
			        "*.applications.delete.*", 
			        "*.roles.create.*", 
			        "*.roles.read.*", 
			        "*.roles.update.*", 
			        "*.roles.delete.*", 
			        "*.events.create.*", 
			        "*.events.read.*", 
			        "*.providers.create.*", 
			        "*.providers.read.*", 
			        "*.providers.update.*", 
			        "*.providers.delete.*", 
			        "*.tokens.create.*", 
			        "*.tokens.read.*", 
			        "*.tokens.update.*", 
			        "*.tokens.delete.*", 
			        "*.webhooks.create.*", 
			        "*.webhooks.read.*", 
			        "*.webhooks.update.*", 
			        "*.webhooks.delete.*"
				},
				Forbidden = null
			});
			
			// 2. Readonly Role
			this.Create(utilizer, membershipId, new Role
			{
				Id = "readonly_role",
				Name = "readonly",
				Description = "Readonly",
				MembershipId = membershipId,
				Permissions = new []
				{ 
					"*.memberships.read.*", 
					"*.users.read.*", 
					"*.applications.read.*", 
					"*.roles.read.*", 
					"*.events.read.*", 
					"*.providers.read.*", 
					"*.tokens.read.*", 
					"*.webhooks.read.*"
				},
				Forbidden = null
			});
		}
		
		#region Get Methods

		public Role Get(string membershipId, string id) => this.GetAsync(membershipId, id).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<Role> GetAsync(string membershipId, string id)
		{
			await Task.CompletedTask;
			return this.MockRepository.FirstOrDefault(x => x.MembershipId == membershipId && x.Id == id);
		}

		public IPaginationCollection<Role> Get(string membershipId, int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection) =>
			this.GetAsync(membershipId, skip, limit, withCount, orderBy, sortDirection).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IPaginationCollection<Role>> GetAsync(string membershipId, int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection)
		{
			await Task.CompletedTask;
			var filteredItems = this.MockRepository.Where(x => x.MembershipId == membershipId);
			if (skip != null && limit != null)
			{
				filteredItems = filteredItems.Skip(skip.Value).Take(limit.Value);
			}
			else if (skip != null)
			{
				filteredItems = filteredItems.Skip(skip.Value);
			}
			else if (limit != null)
			{
				filteredItems = filteredItems.Take(limit.Value);
			}

			if (withCount)
			{
				var array = filteredItems.ToArray();
				return new PaginationCollection<Role>
				{
					Items = array,
					Count = array.Length
				};	
			}
			else
			{
				return new PaginationCollection<Role>
				{
					Items = filteredItems
				};
			}
		}
		
		public Role GetByName(string name, string membershipId) => this.GetByNameAsync(name, membershipId).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<Role> GetByNameAsync(string name, string membershipId)
		{
			await Task.CompletedTask;
			return this.MockRepository.FirstOrDefault(x => x.MembershipId == membershipId && x.Name == name);
		}

		#endregion
		
		#region Query Methods

		public IPaginationCollection<dynamic> Query(
			string query, 
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string sortField = null, 
			SortDirection? sortDirection = null,
			IDictionary<string, bool> selectFields = null) =>
			this.QueryAsync(
				query, 
				skip, 
				limit, 
				withCount, 
				sortField, 
				sortDirection,
				selectFields)
				.ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IPaginationCollection<dynamic>> QueryAsync(
			string query, 
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string sortField = null, 
			SortDirection? sortDirection = null,
			IDictionary<string, bool> selectFields = null)
		{
			await Task.CompletedTask;
			throw new NotImplementedException();
		}

		#endregion
		
		#region Create Methods

		public Role Create(Utilizer utilizer, string membershipId, Role model) => this.CreateAsync(utilizer, membershipId, model).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<Role> CreateAsync(Utilizer utilizer, string membershipId, Role model)
		{
			model.MembershipId = membershipId;
			model.Sys = new SysModel
			{
				CreatedAt = DateTime.Now,
				CreatedBy = utilizer.Username
			};
			
			await Task.CompletedTask;
			this.MockRepository.Add(model);
			return model;
		}

		#endregion
		
		#region Update Methods

		public Role Update(Utilizer utilizer, string membershipId, Role model) => this.UpdateAsync(utilizer, membershipId, model).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<Role> UpdateAsync(Utilizer utilizer, string membershipId, Role model)
		{
			model.MembershipId = membershipId;
			var current = await this.GetAsync(membershipId, model.Id);
			if (current != null)
			{
				model.Sys = new SysModel
				{
					CreatedAt = model.Sys.CreatedAt,
					CreatedBy = model.Sys.CreatedBy,
					ModifiedAt = DateTime.Now,
					ModifiedBy = utilizer.Username
				};
			}
			else
			{
				model.Sys = new SysModel
				{
					CreatedAt = DateTime.Now,
					CreatedBy = utilizer.Username,
					ModifiedAt = DateTime.Now,
					ModifiedBy = utilizer.Username
				};	
			}

			var index = this.MockRepository.FindIndex(x => x.MembershipId == membershipId && x.Id == model.Id);
			this.MockRepository.RemoveAt(index);
			this.MockRepository.Insert(index, model);
			return model;
		}

		#endregion
		
		#region Delete Methods

		public bool Delete(Utilizer utilizer, string membershipId, string id) => this.DeleteAsync(utilizer, membershipId, id).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<bool> DeleteAsync(Utilizer utilizer, string membershipId, string id)
		{
			var index = this.MockRepository.FindIndex(x => x.MembershipId == membershipId && x.Id == id);
			if (index >= 0)
			{
				await Task.CompletedTask;
				this.MockRepository.RemoveAt(index);
				return true;
			}
			else
			{
				return false;
			}
		}

		#endregion
	}
}