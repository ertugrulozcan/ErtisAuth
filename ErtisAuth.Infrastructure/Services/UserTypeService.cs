using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.UserTypes;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.UserTypes;
using ErtisAuth.Infrastructure.Exceptions;
using ErtisAuth.Infrastructure.Mapping;

namespace ErtisAuth.Infrastructure.Services
{
	public class UserTypeService : MembershipBoundedCrudService<UserType, UserTypeDto>, IUserTypeService
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="membershipService"></param>
		/// <param name="userTypeRepository"></param>
		public UserTypeService(IMembershipService membershipService, IUserTypeRepository userTypeRepository) : base(membershipService, userTypeRepository)
		{
			
		}

		#endregion
		
		#region Methods
		
		protected override bool ValidateModel(UserType model, out IEnumerable<string> errors)
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

			errors = errorList;
			return !errors.Any();
		}

		protected override void Overwrite(UserType destination, UserType source)
		{
			destination.Id = source.Id;
			destination.MembershipId = source.MembershipId;
			destination.Slug = source.Slug;
			destination.Sys = source.Sys;
			
			if (string.IsNullOrEmpty(destination.Name))
			{
				destination.Name = source.Name;
			}
			
			if (string.IsNullOrEmpty(destination.Description))
			{
				destination.Description = source.Description;
			}

			if (destination.Schema == null)
			{
				destination.Schema = source.Schema;
			}
		}

		protected override bool IsAlreadyExist(UserType model, string membershipId, UserType exclude = default)
		{
			if (exclude == null)
			{
				return this.GetUserTypeByName(model.Name, membershipId) != null;	
			}
			else
			{
				var current = this.GetUserTypeByName(model.Name, membershipId);
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

		protected override async Task<bool> IsAlreadyExistAsync(UserType model, string membershipId, UserType exclude = default)
		{
			if (exclude == null)
			{
				return await this.GetUserTypeByNameAsync(model.Name, membershipId) != null;	
			}
			else
			{
				var current = await this.GetUserTypeByNameAsync(model.Name, membershipId);
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
		
		protected override ErtisAuthException GetAlreadyExistError(UserType model)
		{
			return ErtisAuthException.UserTypeWithSameNameAlreadyExists($"'{model.Name}'");
		}
		
		protected override ErtisAuthException GetNotFoundError(string id)
		{
			return ErtisAuthException.UserTypeNotFound(id);
		}

		private UserType GetUserTypeByName(string name, string membershipId)
		{
			var dto = this.repository.FindOne(x => x.Name == name && x.MembershipId == membershipId);
			if (dto == null)
			{
				return null;
			}
			
			return Mapper.Current.Map<UserTypeDto, UserType>(dto);
		}
		
		private async Task<UserType> GetUserTypeByNameAsync(string name, string membershipId)
		{
			var dto = await this.repository.FindOneAsync(x => x.Name == name && x.MembershipId == membershipId);
			if (dto == null)
			{
				return null;
			}
			
			return Mapper.Current.Map<UserTypeDto, UserType>(dto);
		}
		
		#endregion
	}
}