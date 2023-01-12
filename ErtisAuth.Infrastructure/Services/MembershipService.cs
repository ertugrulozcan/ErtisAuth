using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Memberships;
using ErtisAuth.Infrastructure.Mapping;

namespace ErtisAuth.Infrastructure.Services
{
	public class MembershipService : GenericCrudService<Membership, MembershipDto>, IMembershipService
	{
		#region Properties

		private List<IMembershipBoundedService> MembershipBoundedServiceCollection { get; }

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="membershipRepository"></param>
		public MembershipService(IMembershipRepository membershipRepository) : base(membershipRepository)
		{
			this.MembershipBoundedServiceCollection = new List<IMembershipBoundedService>();
		}

		#endregion

		#region Methods

		public void RegisterService<T>(IMembershipBoundedService<T> service) where T : IHasMembership, IHasIdentifier
		{
			if (service is IMembershipBoundedService membershipBoundedResource)
			{
				this.MembershipBoundedServiceCollection.Add(membershipBoundedResource);	
			}
		}
		
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
		
		private IEnumerable<MembershipBoundedResource> GetMembershipBoundedResources(string membershipId, int limit = 10) =>
			this.GetMembershipBoundedResourcesAsync(membershipId, limit).ConfigureAwait(false).GetAwaiter().GetResult();

		private async Task<IEnumerable<MembershipBoundedResource>> GetMembershipBoundedResourcesAsync(string membershipId, int limit = 10, CancellationToken cancellationToken = default)
		{
			var tasks = this.MembershipBoundedServiceCollection.Select(service => 
				service.GetAsync<MembershipBoundedResource>(membershipId, 0, limit, false, null, null, cancellationToken: cancellationToken).AsTask())
				.ToArray();

			await Task.WhenAll(tasks);

			var cumulativeList = new List<MembershipBoundedResource>();
			foreach (var task in tasks)
			{
				var items = (await task).Items;
				cumulativeList.AddRange(items);
			}

			return cumulativeList.Take(limit);
		}

		#endregion

		#region Read Methods

		public Membership GetBySecretKey(string secretKey)
		{
			var dto = this.repository.FindOne(x => x.SecretKey == secretKey);
			if (dto == null)
			{
				return null;
			}

			return Mapper.Current.Map<MembershipDto, Membership>(dto);
		}

		public async Task<Membership> GetBySecretKeyAsync(string secretKey, CancellationToken cancellationToken = default)
		{
			var dto = await this.repository.FindOneAsync(x => x.SecretKey == secretKey, cancellationToken: cancellationToken);
			if (dto == null)
			{
				return null;
			}

			return Mapper.Current.Map<MembershipDto, Membership>(dto);
		}

		#endregion
		
		#region Delete Methods

		public override bool Delete(string id)
		{
			var membershipBoundedResources = this.GetMembershipBoundedResources(id);
			if (membershipBoundedResources.Any())
			{
				throw ErtisAuthException.MembershipCouldNotDeleted(id);
			}
			
			return base.Delete(id);
		}
		
		public override async ValueTask<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
		{
			var membershipBoundedResources = await this.GetMembershipBoundedResourcesAsync(id, cancellationToken: cancellationToken);
			if (membershipBoundedResources.Any())
			{
				throw ErtisAuthException.MembershipCouldNotDeleted(id);
			}
			
			return await base.DeleteAsync(id, cancellationToken: cancellationToken);
		}
		
		#endregion
	}
}