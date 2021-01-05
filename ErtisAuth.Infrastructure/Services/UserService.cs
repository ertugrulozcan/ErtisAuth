using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Users;
using ErtisAuth.Infrastructure.Events;
using ErtisAuth.Infrastructure.Exceptions;
using ErtisAuth.Infrastructure.Mapping;

namespace ErtisAuth.Infrastructure.Services
{
	public class UserService : MembershipBoundedCrudService<User, UserDto>, IUserService
	{
		#region Services

		private readonly IMembershipService membershipService;
		private readonly IRoleService roleService;
		private readonly IEventService eventService;
		private readonly ICryptographyService cryptographyService;

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="membershipService"></param>
		/// <param name="roleService"></param>
		/// <param name="eventService"></param>
		/// <param name="cryptographyService"></param>
		/// <param name="userRepository"></param>
		public UserService(
			IMembershipService membershipService,
			IRoleService roleService, 
			IEventService eventService,
			ICryptographyService cryptographyService, 
			IUserRepository userRepository) : 
			base(membershipService, userRepository)
		{
			this.membershipService = membershipService;
			this.roleService = roleService;
			this.eventService = eventService;
			this.cryptographyService = cryptographyService;
			
			this.OnCreated += this.UserCreatedEventHandler;
			this.OnUpdated += this.UserUpdatedEventHandler;
			this.OnDeleted += this.UserDeletedEventHandler;
		}

		#endregion

		#region Event Handlers

		private void UserCreatedEventHandler(object sender, CreateResourceEventArgs<User> eventArgs)
		{
			this.eventService.FireEventAsync(new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.UserCreated,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Resource,
				MembershipId = eventArgs.Utilizer.MembershipId
			});
		}
		
		private void UserUpdatedEventHandler(object sender, UpdateResourceEventArgs<User> eventArgs)
		{
			this.eventService.FireEventAsync(new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.UserUpdated,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Updated,
				Prior = eventArgs.Prior,
				MembershipId = eventArgs.Utilizer.MembershipId
			});
		}
		
		private void UserDeletedEventHandler(object sender, DeleteResourceEventArgs<User> eventArgs)
		{
			this.eventService.FireEventAsync(new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.UserDeleted,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Resource,
				MembershipId = eventArgs.Utilizer.MembershipId
			});
		}

		#endregion
		
		#region Methods

		public override User Update(Utilizer utilizer, string membershipId, User model)
		{
			var currentUser = this.GetUserWithPassword(model.Id, membershipId);
			var updatedUser = base.Update(utilizer, membershipId, model);
			base.Update(utilizer, membershipId, new UserWithPassword(updatedUser) { PasswordHash = currentUser.PasswordHash });

			return updatedUser;
		}

		public override async Task<User> UpdateAsync(Utilizer utilizer, string membershipId, User model)
		{
			var currentUser = await this.GetUserWithPasswordAsync(model.Id, membershipId);
			var updatedUser = await base.UpdateAsync(utilizer, membershipId, model);
			await base.UpdateAsync(utilizer, membershipId, new UserWithPassword(updatedUser) { PasswordHash = currentUser.PasswordHash });

			return updatedUser;
		}

		protected override bool ValidateModel(User model, out IEnumerable<string> errors)
		{
			var errorList = new List<string>();
			if (string.IsNullOrEmpty(model.Username))
			{
				errorList.Add("username is a required field");
			}
			
			if (string.IsNullOrEmpty(model.EmailAddress))
			{
				errorList.Add("email_address is a required field");
			}
			else if (!this.IsValidEmail(model.EmailAddress))
			{
				errorList.Add("Email address is not valid");
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

			if (model is UserWithPassword userWithPassword)
			{
				if (string.IsNullOrEmpty(userWithPassword.PasswordHash))
				{
					errorList.Add("password is a required field");
				}
			}

			errors = errorList;
			return !errors.Any();
		}

		protected override void Overwrite(User destination, User source)
		{
			destination.Id = source.Id;
			destination.Username = source.Username;
			destination.EmailAddress = source.EmailAddress;
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
			
			if (destination is UserWithPassword destinationWithPassword)
			{
				if (source is UserWithPassword sourceWithPassword)
				{
					destinationWithPassword.PasswordHash = sourceWithPassword.PasswordHash;
				}	
			}
		}

		protected override bool IsAlreadyExist(User model, string membershipId, User exclude = default)
		{
			if (exclude == null)
			{
				return this.GetUserWithPassword(model.Username, model.EmailAddress, membershipId) != null;	
			}
			else
			{
				var current = this.GetUserWithPassword(model.Username, model.EmailAddress, membershipId);
				if (current != null)
				{
					return current.Username != exclude.Username && current.EmailAddress != exclude.EmailAddress;	
				}
				else
				{
					return false;
				}
			}
		}

		protected override async Task<bool> IsAlreadyExistAsync(User model, string membershipId, User exclude = default)
		{
			if (exclude == null)
			{
				return await this.GetUserWithPasswordAsync(model.Username, model.EmailAddress, membershipId) != null;	
			}
			else
			{
				var current = await this.GetUserWithPasswordAsync(model.Username, model.EmailAddress, membershipId);
				if (current != null)
				{
					return current.Username != exclude.Username && current.EmailAddress != exclude.EmailAddress;	
				}
				else
				{
					return false;
				}
			}
		}
		
		protected override ErtisAuthException GetAlreadyExistError(User model)
		{
			return ErtisAuthException.UserWithSameUsernameAlreadyExists($"'{model.Username}', '{model.EmailAddress}'");
		}
		
		protected override ErtisAuthException GetNotFoundError(string id)
		{
			return ErtisAuthException.UserNotFound(id, "_id");
		}

		public UserWithPassword GetUserWithPassword(string id, string membershipId)
		{
			var dto = this.repository.FindOne(x => x.Id == id && x.MembershipId == membershipId);
			if (dto == null)
			{
				return null;
			}
			
			return Mapper.Current.Map<UserDto, UserWithPassword>(dto);
		}

		public async Task<UserWithPassword> GetUserWithPasswordAsync(string id, string membershipId)
		{
			var dto = await this.repository.FindOneAsync(x => x.Id == id && x.MembershipId == membershipId);
			if (dto == null)
			{
				return null;
			}
			
			return Mapper.Current.Map<UserDto, UserWithPassword>(dto);
		}
		
		public UserWithPassword GetUserWithPassword(string username, string email, string membershipId)
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
			
			return Mapper.Current.Map<UserDto, UserWithPassword>(dto);
		}
		
		public async Task<UserWithPassword> GetUserWithPasswordAsync(string username, string email, string membershipId)
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
			
			return Mapper.Current.Map<UserDto, UserWithPassword>(dto);
		}
		
		private bool IsValidEmail(string email)
		{
			try 
			{
				var addr = new System.Net.Mail.MailAddress(email);
				return addr.Address == email;
			}
			catch 
			{
				return false;
			}
		}

		#endregion

		#region Change Password

		public async Task<User> ChangePasswordAsync(Utilizer utilizer, string membershipId, string userId, string newPassword)
		{
			if (string.IsNullOrEmpty(newPassword))
			{
				throw ErtisAuthException.ValidationError(new []
				{
					"Password can not be null or empty!"
				});
			}
			
			var membership = await this.membershipService.GetAsync(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}

			var user = await this.GetAsync(membershipId, userId);
			if (user == null)
			{
				throw ErtisAuthException.UserNotFound(userId, "_id");
			}

			var userWithPassword = Mapper.Current.Map<User, UserWithPassword>(user);
			var passwordHash = this.cryptographyService.CalculatePasswordHash(membership, newPassword);
			userWithPassword.PasswordHash = passwordHash;

			return await this.UpdateAsync(utilizer, membershipId, userWithPassword);
		}

		#endregion
	}
}