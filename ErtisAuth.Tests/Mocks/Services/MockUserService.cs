using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Core.Models.Resources;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Tests.Mocks.Services
{
	public class MockUserService : IUserService
	{
		#region Properties

		private List<User> MockRepository { get; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		public MockUserService()
		{
			this.MockRepository = new List<User>();
			this.PopulateMockRepository();
		}

		#endregion

		#region Methods

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

			// 1. Admin User
			this.Create(utilizer, membershipId, new User
			{
				Id = "user_id",
				Username = "test_user",
				FirstName = "John",
				LastName = "Smith",
				EmailAddress = "john.smith@mail.com",
				Role = "admin",
				MembershipId = membershipId
			});
			
			// 2. Restricted User
			this.Create(utilizer, membershipId, new User
			{
				Id = "restricted_user_id",
				Username = "restricted_test_user",
				FirstName = "Adam",
				LastName = "Brown",
				EmailAddress = "adam.brown@mail.com",
				Role = "admin",
				MembershipId = membershipId,
				Forbidden = new []
				{
					"users.create.*"
				}
			});
			
			// 3. Extra Qualified User
			this.Create(utilizer, membershipId, new User
			{
				Id = "qualified_user_id",
				Username = "qualified_test_user",
				FirstName = "Steve",
				LastName = "Sealer",
				EmailAddress = "steve.sealer@mail.com",
				Role = "readonly",
				MembershipId = membershipId,
				Permissions = new []
				{
					"users.create.*"
				}
			});
		}

		#endregion
		
		#region Get Methods

		public User Get(string membershipId, string id) => this.GetAsync(membershipId, id).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<User> GetAsync(string membershipId, string id)
		{
			await Task.CompletedTask;
			return this.MockRepository.FirstOrDefault(x => x.MembershipId == membershipId && x.Id == id);
		}

		public IPaginationCollection<User> Get(string membershipId, int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection) =>
			this.GetAsync(membershipId, skip, limit, withCount, orderBy, sortDirection).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IPaginationCollection<User>> GetAsync(string membershipId, int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection)
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
				return new PaginationCollection<User>
				{
					Items = array,
					Count = array.Length
				};	
			}
			else
			{
				return new PaginationCollection<User>
				{
					Items = filteredItems
				};
			}
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

		public User Create(Utilizer utilizer, string membershipId, User model) => this.CreateAsync(utilizer, membershipId, model).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<User> CreateAsync(Utilizer utilizer, string membershipId, User model)
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

		public User Update(Utilizer utilizer, string membershipId, User model) => this.UpdateAsync(utilizer, membershipId, model).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<User> UpdateAsync(Utilizer utilizer, string membershipId, User model)
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

		#region Other Methods

		public UserWithPasswordHash GetUserWithPassword(string id, string membershipId)
		{
			throw new NotImplementedException();
		}

		public Task<UserWithPasswordHash> GetUserWithPasswordAsync(string id, string membershipId)
		{
			throw new NotImplementedException();
		}

		public UserWithPasswordHash GetUserWithPassword(string username, string email, string membershipId)
		{
			throw new NotImplementedException();
		}

		public Task<UserWithPasswordHash> GetUserWithPasswordAsync(string username, string email, string membershipId)
		{
			throw new NotImplementedException();
		}

		public User ChangePassword(Utilizer utilizer, string membershipId, string userId, string newPassword)
		{
			throw new NotImplementedException();
		}

		public Task<User> ChangePasswordAsync(Utilizer utilizer, string membershipId, string userId, string newPassword)
		{
			throw new NotImplementedException();
		}

		public ResetPasswordToken ResetPassword(Utilizer utilizer, string membershipId, string usernameOrEmailAddress)
		{
			throw new NotImplementedException();
		}

		public Task<ResetPasswordToken> ResetPasswordAsync(Utilizer utilizer, string membershipId, string usernameOrEmailAddress)
		{
			throw new NotImplementedException();
		}

		public void SetPassword(Utilizer utilizer, string membershipId, string resetToken, string usernameOrEmailAddress, string password)
		{
			throw new NotImplementedException();
		}

		public Task SetPasswordAsync(Utilizer utilizer, string membershipId, string resetToken, string usernameOrEmailAddress, string password)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}