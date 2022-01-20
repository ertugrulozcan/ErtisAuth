using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ertis.Security.Cryptography;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Helpers;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Users;
using ErtisAuth.Identity.Jwt.Services.Interfaces;
using ErtisAuth.Events.EventArgs;
using ErtisAuth.Infrastructure.Mapping;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace ErtisAuth.Infrastructure.Services
{
	public class UserService : MembershipBoundedCrudService<User, UserDto>, IUserService
	{
		#region Services

		private readonly IRoleService roleService;
		private readonly IEventService eventService;
		private readonly IJwtService jwtService;
		private readonly ICryptographyService cryptographyService;

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="membershipService"></param>
		/// <param name="roleService"></param>
		/// <param name="eventService"></param>
		/// <param name="jwtService"></param>
		/// <param name="cryptographyService"></param>
		/// <param name="userRepository"></param>
		public UserService(
			IMembershipService membershipService,
			IRoleService roleService, 
			IEventService eventService,
			IJwtService jwtService,
			ICryptographyService cryptographyService, 
			IUserRepository userRepository) : 
			base(membershipService, userRepository)
		{
			this.roleService = roleService;
			this.eventService = eventService;
			this.jwtService = jwtService;
			this.cryptographyService = cryptographyService;
			
			this.OnCreated += this.UserCreatedEventHandler;
			this.OnUpdated += this.UserUpdatedEventHandler;
			this.OnDeleted += this.UserDeletedEventHandler;
		}

		#endregion

		#region Event Handlers

		private void UserCreatedEventHandler(object sender, CreateResourceEventArgs<User> eventArgs)
		{
			this.eventService.FireEventAsync(this, new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.UserCreated,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Resource,
				MembershipId = eventArgs.Utilizer.MembershipId
			});
		}
		
		private void UserUpdatedEventHandler(object sender, UpdateResourceEventArgs<User> eventArgs)
		{
			this.eventService.FireEventAsync(this, new ErtisAuthEvent
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
			this.eventService.FireEventAsync(this, new ErtisAuthEvent
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
			var passwordHash = currentUser.PasswordHash;
			if (model is UserWithPasswordHash userWithPassword && !string.IsNullOrEmpty(userWithPassword.PasswordHash) && userWithPassword.PasswordHash != passwordHash)
			{
				passwordHash = userWithPassword.PasswordHash;
			}
			
			return base.Update(utilizer, membershipId, new UserWithPasswordHash(model) { PasswordHash = passwordHash });
		}

		public override async ValueTask<User> UpdateAsync(Utilizer utilizer, string membershipId, User model)
		{
			var currentUser = await this.GetUserWithPasswordAsync(model.Id, membershipId);
			var passwordHash = currentUser.PasswordHash;
			if (model is UserWithPasswordHash userWithPassword && !string.IsNullOrEmpty(userWithPassword.PasswordHash) && userWithPassword.PasswordHash != passwordHash)
			{
				passwordHash = userWithPassword.PasswordHash;
			}
			
			return await base.UpdateAsync(utilizer, membershipId, new UserWithPasswordHash(model) { PasswordHash = passwordHash });
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

			if (model.AdditionalProperties != null)
			{
				var membership = this.membershipService.Get(model.MembershipId);
				if (membership == null)
				{
					throw ErtisAuthException.MembershipNotFound(model.MembershipId);
				}
			
				if (membership.UserType != null)
				{
					try
					{
						var schema = membership.UserType.ConvertToJSchema();
						if (model.AdditionalProperties is JObject jObject)
						{
							jObject.Validate(schema);
						}
						else
						{
							jObject = JObject.FromObject(model.AdditionalProperties);
							jObject.Validate(schema);	
						}
					}
					catch (Exception ex)
					{
						throw ErtisAuthException.UserTypeValidationException(ex.Message);
					}
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

			if (model is UserWithPasswordHash userWithPassword)
			{
				if (string.IsNullOrEmpty(userWithPassword.PasswordHash))
				{
					errorList.Add("password is a required field");
				}
			}

			errors = errorList;
			return !errors.Any();
		}

		[SuppressMessage("ReSharper", "ConvertIfStatementToNullCoalescingAssignment")]
		protected override void Overwrite(User destination, User source)
		{
			destination.Id = source.Id;
			destination.MembershipId = source.MembershipId;
			destination.Sys = source.Sys;
			
			if (this.IsIdentical(destination, source))
			{
				throw ErtisAuthException.IdenticalDocument();
			}
			
			if (string.IsNullOrEmpty(destination.Username))
			{
				destination.Username = source.Username;
			}
			
			if (string.IsNullOrEmpty(destination.EmailAddress))
			{
				destination.EmailAddress = source.EmailAddress;
			}
			
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
			
			if (destination.Permissions == null)
			{
				destination.Permissions = source.Permissions;
			}
			
			if (destination.Forbidden == null)
			{
				destination.Forbidden = source.Forbidden;
			}
			
			if (destination.AdditionalProperties == null)
			{
				destination.AdditionalProperties = source.AdditionalProperties;
			}
			
			if (destination is UserWithPasswordHash destinationWithPassword)
			{
				if (source is UserWithPasswordHash sourceWithPassword)
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

		public UserWithPasswordHash GetUserWithPassword(string id, string membershipId)
		{
			var dto = this.repository.FindOne(x => x.Id == id && x.MembershipId == membershipId);
			if (dto == null)
			{
				return null;
			}
			
			return Mapper.Current.Map<UserDto, UserWithPasswordHash>(dto);
		}

		public async ValueTask<UserWithPasswordHash> GetUserWithPasswordAsync(string id, string membershipId)
		{
			var dto = await this.repository.FindOneAsync(x => x.Id == id && x.MembershipId == membershipId);
			if (dto == null)
			{
				return null;
			}
			
			return Mapper.Current.Map<UserDto, UserWithPasswordHash>(dto);
		}
		
		public UserWithPasswordHash GetUserWithPassword(string username, string email, string membershipId)
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
			
			return Mapper.Current.Map<UserDto, UserWithPasswordHash>(dto);
		}
		
		public async ValueTask<UserWithPasswordHash> GetUserWithPasswordAsync(string username, string email, string membershipId)
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
			
			return Mapper.Current.Map<UserDto, UserWithPasswordHash>(dto);
		}
		
		private bool IsValidEmail(string emailAddress)
		{
			try 
			{
				var email = new System.Net.Mail.MailAddress(emailAddress);
				return email.Address == emailAddress;
			}
			catch 
			{
				return false;
			}
		}

		#endregion

		#region Change Password

		public User ChangePassword(Utilizer utilizer, string membershipId, string userId, string newPassword)
		{
			if (string.IsNullOrEmpty(newPassword))
			{
				throw ErtisAuthException.ValidationError(new []
				{
					"Password can not be null or empty!"
				});
			}
			
			var membership = this.membershipService.Get(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}

			var user = this.Get(membershipId, userId);
			if (user == null)
			{
				throw ErtisAuthException.UserNotFound(userId, "_id");
			}

			var userWithPassword = Mapper.Current.Map<User, UserWithPasswordHash>(user);
			var passwordHash = this.cryptographyService.CalculatePasswordHash(membership, newPassword);
			userWithPassword.PasswordHash = passwordHash;

			var updatedUser = this.Update(utilizer, membershipId, userWithPassword);
			
			this.eventService.FireEventAsync(this, new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.UserPasswordChanged,
				UtilizerId = user.Id,
				Document = updatedUser,
				Prior = userWithPassword,
				MembershipId = membershipId
			});

			return updatedUser;
		}
		
		public async ValueTask<User> ChangePasswordAsync(Utilizer utilizer, string membershipId, string userId, string newPassword)
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

			var userWithPassword = Mapper.Current.Map<User, UserWithPasswordHash>(user);
			var passwordHash = this.cryptographyService.CalculatePasswordHash(membership, newPassword);
			userWithPassword.PasswordHash = passwordHash;

			var updatedUser = await this.UpdateAsync(utilizer, membershipId, userWithPassword);
			
			await this.eventService.FireEventAsync(this, new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.UserPasswordChanged,
				UtilizerId = user.Id,
				Document = updatedUser,
				Prior = userWithPassword,
				MembershipId = membershipId
			});

			return updatedUser;
		}

		#endregion

		#region Forgot Password

		public ResetPasswordToken ResetPassword(Utilizer utilizer, string membershipId, string usernameOrEmailAddress)
		{
			return this.ResetPasswordAsync(utilizer, membershipId, usernameOrEmailAddress).ConfigureAwait(false).GetAwaiter().GetResult();
		}

		public async ValueTask<ResetPasswordToken> ResetPasswordAsync(Utilizer utilizer, string membershipId, string usernameOrEmailAddress)
		{
			if (string.IsNullOrEmpty(usernameOrEmailAddress))
			{
				throw ErtisAuthException.ValidationError(new []
				{
					"Username or email required!"
				});
			}
			
			var membership = await this.membershipService.GetAsync(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}

			var user = await this.GetUserWithPasswordAsync(usernameOrEmailAddress, usernameOrEmailAddress, membershipId);
			if (user == null)
			{
				throw ErtisAuthException.UserNotFound(usernameOrEmailAddress, "username or email_address");
			}

			if (utilizer.Role is ReservedRoles.Administrator or ReservedRoles.Server || utilizer.Id == user.Id)
			{
				var tokenClaims = new TokenClaims(Guid.NewGuid().ToString(), user, membership);
				tokenClaims.AddClaim("token_type", "reset_token");
				var resetToken = this.jwtService.GenerateToken(tokenClaims, HashAlgorithms.SHA2_256, Encoding.UTF8);
				var resetPasswordToken = new ResetPasswordToken(resetToken, TimeSpan.FromHours(1));
				
				await this.eventService.FireEventAsync(this, new ErtisAuthEvent
				{
					EventType = ErtisAuthEventType.UserPasswordReset,
					UtilizerId = user.Id,
					Document = resetPasswordToken,
					MembershipId = membershipId
				});

				return resetPasswordToken;
			}
			else
			{
				throw ErtisAuthException.AccessDenied("Unauthorized access");
			}
		}

		public void SetPassword(Utilizer utilizer, string membershipId, string resetToken, string usernameOrEmailAddress, string password)
		{
			this.SetPasswordAsync(utilizer, membershipId, resetToken, usernameOrEmailAddress, password).ConfigureAwait(false).GetAwaiter().GetResult();
		}

		public async ValueTask SetPasswordAsync(Utilizer utilizer, string membershipId, string resetToken, string usernameOrEmailAddress, string password)
		{
			if (string.IsNullOrEmpty(usernameOrEmailAddress))
			{
				throw ErtisAuthException.ValidationError(new []
				{
					"Username or email required!"
				});
			}
			
			var membership = await this.membershipService.GetAsync(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}

			var user = await this.GetUserWithPasswordAsync(usernameOrEmailAddress, usernameOrEmailAddress, membershipId);
			if (user == null)
			{
				throw ErtisAuthException.UserNotFound(usernameOrEmailAddress, "username or email_address");
			}

			if (utilizer.Role is ReservedRoles.Administrator or ReservedRoles.Server || utilizer.Id == user.Id)
			{
				if (this.jwtService.TryDecodeToken(resetToken, out var securityToken))
				{
					var expireTime = securityToken.ValidTo.ToLocalTime();
					if (DateTime.Now > expireTime)
					{
						// Token was expired!
						throw ErtisAuthException.TokenWasExpired();	
					}

					await this.ChangePasswordAsync(utilizer, membershipId, user.Id, password);
				}
				else
				{
					// Reset token could not decoded!
					throw ErtisAuthException.InvalidToken();
				}
			}
			else
			{
				throw ErtisAuthException.AccessDenied("Unauthorized access");
			}
		}

		#endregion
	}
}