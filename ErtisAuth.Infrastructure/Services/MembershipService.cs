using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Memberships;
using ErtisAuth.Infrastructure.Exceptions;

namespace ErtisAuth.Infrastructure.Services
{
	public class MembershipService : GenericCrudService<Membership, MembershipDto>, IMembershipService
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="membershipRepository"></param>
		public MembershipService(IMembershipRepository membershipRepository) : base(membershipRepository)
		{
			
		}

		#endregion
		
		protected override bool ValidateModel(Membership model, out IEnumerable<string> errors)
		{
			var errorList = new List<string>();
			
			if (string.IsNullOrEmpty(model.Name))
			{
				errorList.Add("name is a required field");
			}
			
			if (model.ExpiresIn <= 0)
			{
				errorList.Add("expires_in is a required field");
			}
			
			if (model.RefreshTokenExpiresIn <= 0)
			{
				errorList.Add("refresh_token_expires_in is a required field");
			}

			if (string.IsNullOrEmpty(model.SecretKey))
			{
				errorList.Add("secret_key is a required field");
			}
			
			errors = errorList;
			return !errors.Any();
		}

		protected override void Overwrite(Membership destination, Membership source)
		{
			destination.Id = source.Id;
			destination.Sys = source.Sys;
			
			if (string.IsNullOrEmpty(destination.Name))
			{
				destination.Name = source.Name;
			}
			
			if (string.IsNullOrEmpty(destination.SecretKey))
			{
				destination.SecretKey = source.SecretKey;
			}
			
			if (string.IsNullOrEmpty(destination.HashAlgorithm))
			{
				destination.HashAlgorithm = source.HashAlgorithm;
			}
			
			if (string.IsNullOrEmpty(destination.DefaultEncoding))
			{
				destination.DefaultEncoding = source.DefaultEncoding;
			}
			
			if (destination.ExpiresIn == 0)
			{
				destination.ExpiresIn = source.ExpiresIn;
			}
			
			if (destination.RefreshTokenExpiresIn == 0)
			{
				destination.RefreshTokenExpiresIn = source.RefreshTokenExpiresIn;
			}
		}

		protected override bool IsAlreadyExist(Membership model, Membership exclude = default)
		{
			if (exclude == null)
			{
				return this.Get(model.Id) != null;	
			}
			else
			{
				var current = this.Get(model.Id);
				if (current != null)
				{
					return current.Id != exclude.Id;	
				}
				else
				{
					return false;
				}
			}
		}

		protected override async Task<bool> IsAlreadyExistAsync(Membership model, Membership exclude = default)
		{
			if (exclude == null)
			{
				return await this.GetAsync(model.Id) != null;	
			}
			else
			{
				var current = await this.GetAsync(model.Id);
				if (current != null)
				{
					return current.Id != exclude.Id;	
				}
				else
				{
					return false;
				}
			}
		}

		protected override ErtisAuthException GetAlreadyExistError(Membership model)
		{
			return ErtisAuthException.MembershipAlreadyExists(model.Id);
		}

		protected override ErtisAuthException GetNotFoundError(string id)
		{
			return ErtisAuthException.MembershipNotFound(id);
		}
	}
}