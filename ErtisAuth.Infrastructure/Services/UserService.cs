using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Users;
using ErtisAuth.Infrastructure.Exceptions;
using ErtisAuth.Infrastructure.Mapping;

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
		
		protected override bool ValidateModel(User model, out IEnumerable<string> errors)
		{
			var errorList = new List<string>();
			if (string.IsNullOrEmpty(model.Username))
			{
				errorList.Add("'username' is a required field");
			}
			
			if (string.IsNullOrEmpty(model.EmailAddress))
			{
				errorList.Add("'email_address' is a required field");
			}
			
			if (string.IsNullOrEmpty(model.MembershipId))
			{
				errorList.Add("'membership_id' is a required field");
			}

			errors = errorList;
			return !errors.Any();
		}

		protected override void Overwrite(User destination, User source)
		{
			destination.Id = source.Id;
			destination.Username = source.Username;
			destination.EmailAddress = source.EmailAddress;
			destination.PasswordHash = source.PasswordHash;
			destination.MembershipId = source.MembershipId;
			destination.Sys = source.Sys;
			
			if (string.IsNullOrEmpty(destination.FirstName))
			{
				destination.FirstName = source.FirstName;
			}
			
			if (string.IsNullOrEmpty(destination.LastName))
			{
				destination.LastName = source.LastName;
			}
			
			if (string.IsNullOrEmpty(destination.Role))
			{
				destination.Role = source.Role;
			}
		}

		protected override bool IsAlreadyExist(User model, string membershipId)
		{
			return this.GetByUsernameOrEmail(model.Username, model.EmailAddress, membershipId) != null;
		}

		protected override async Task<bool> IsAlreadyExistAsync(User model, string membershipId)
		{
			return await this.GetByUsernameOrEmailAsync(model.Username, model.EmailAddress, membershipId) != null;
		}
		
		protected override ErtisAuthException GetAlreadyExistError(User model)
		{
			return ErtisAuthException.UserWithSameUsernameAlreadyExists($"'{model.Username}', '{model.EmailAddress}'");
		}
		
		public User GetByUsernameOrEmail(string username, string email, string membershipId)
		{
			var dto = this.repository.FindOne(x =>
				(x.Username == username || 
				 x.EmailAddress == email ||
				 x.Username == email ||
				 x.EmailAddress == username) && x.MembershipId == membershipId);

			if (dto == null)
			{
				return null;
			}
			
			return Mapper.Current.Map<UserDto, User>(dto);
		}
		
		public async Task<User> GetByUsernameOrEmailAsync(string username, string email, string membershipId)
		{
			var dto = await this.repository.FindOneAsync(x =>
				(x.Username == username || 
				 x.EmailAddress == email ||
				 x.Username == email ||
				 x.EmailAddress == username) && x.MembershipId == membershipId);

			if (dto == null)
			{
				return null;
			}
			
			return Mapper.Current.Map<UserDto, User>(dto);
		}

		#endregion
	}
}