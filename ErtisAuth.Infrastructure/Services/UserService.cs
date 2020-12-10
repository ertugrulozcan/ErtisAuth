using System.Collections.Generic;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Users;
using ErtisAuth.Infrastructure.Exceptions;
using ErtisAuth.Infrastructure.Extensions;

namespace ErtisAuth.Infrastructure.Services
{
	public class UserService : MembershipBoundedCrudService<User, UserDto>, IUserService
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="membershipService"></param>
		/// <param name="userRepository"></param>
		public UserService(IMembershipService membershipService, IUserRepository userRepository) : base(membershipService, userRepository)
		{
			
		}

		#endregion
		
		#region Methods

		public async Task<IPaginationCollection<dynamic>> QueryAsync(
			string query, 
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string sortField = null,
			SortDirection? sortDirection = null, 
			IDictionary<string, bool> selectFields = null)
		{
			return await this.repository.QueryAsync(query, skip, limit, withCount, sortField, sortDirection, selectFields);
		}

		public override User Create(string membershipId, User model)
		{
			if (model == null)
			{
				throw ErtisAuthException.RequestBodyNull();
			}

			if (!model.ValidateForPost(out var errors))
			{
				throw ErtisAuthException.ValidationError(errors);
			}
			
			return base.Create(membershipId, model);
		}
		
		public override async Task<User> CreateAsync(string membershipId, User model)
		{
			if (model == null)
			{
				throw ErtisAuthException.RequestBodyNull();
			}

			if (!model.ValidateForPost(out var errors))
			{
				throw ErtisAuthException.ValidationError(errors);
			}
			
			return await base.CreateAsync(membershipId, model);
		}

		#endregion
	}
}