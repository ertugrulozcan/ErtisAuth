using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Data.Models;
using Ertis.MongoDB.Repository;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Infrastructure.Mapping;

namespace ErtisAuth.Infrastructure.Services
{
	public abstract class MembershipBoundedService<TModel, TDto> : IMembershipBoundedService<TModel> 
		where TModel : class, Core.Models.IHasMembership, Core.Models.IHasIdentifier
		where TDto : class, IEntity<string>, Dto.Models.IHasMembership
	{
		#region Services

		protected readonly IMembershipService membershipService;
		protected readonly IMongoRepository<TDto> repository;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="membershipService"></param>
		/// <param name="repository"></param>
		protected MembershipBoundedService(IMembershipService membershipService, IMongoRepository<TDto> repository)
		{
			this.membershipService = membershipService;
			this.repository = repository;
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
			IDictionary<string, bool> selectFields = null)
		{
			return this.repository.Query(query, skip, limit, withCount, sortField, sortDirection, selectFields);
		}
		
		public async ValueTask<IPaginationCollection<dynamic>> QueryAsync(
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
		
		#endregion
		
		#region Get Methods

		public virtual TModel Get(string membershipId, string id)
		{
			var membership = this.membershipService.Get(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			
			var dto = this.repository.FindOne(x => x.Id == id && x.MembershipId == membershipId);
			return Mapper.Current.Map<TDto, TModel>(dto);
		}

		public virtual async ValueTask<TModel> GetAsync(string membershipId, string id)
		{
			var membership = await this.membershipService.GetAsync(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			
			var dto = await this.repository.FindOneAsync(x => x.Id == id && x.MembershipId == membershipId);
			return Mapper.Current.Map<TDto, TModel>(dto);
		}
		
		public virtual IPaginationCollection<TModel> Get(string membershipId, int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection)
		{
			var membership = this.membershipService.Get(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			
			var paginatedDtoCollection = this.repository.Find(x => x.MembershipId == membershipId, skip, limit, withCount, orderBy, sortDirection);
			if (paginatedDtoCollection?.Items != null)
			{
				return new PaginationCollection<TModel>
				{
					Items = paginatedDtoCollection.Items.Select(x => Mapper.Current.Map<TDto, TModel>(x)),
					Count = paginatedDtoCollection.Count
				};
			}
			else
			{
				return new PaginationCollection<TModel>();
			}
		}
		
		public virtual async ValueTask<IPaginationCollection<TModel>> GetAsync(string membershipId, int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection)
		{
			var membership = await this.membershipService.GetAsync(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			
			var paginatedDtoCollection = await this.repository.FindAsync(x => x.MembershipId == membershipId, skip, limit, withCount, orderBy, sortDirection);
			if (paginatedDtoCollection?.Items != null)
			{
				return new PaginationCollection<TModel>
				{
					Items = paginatedDtoCollection.Items.Select(x => Mapper.Current.Map<TDto, TModel>(x)),
					Count = paginatedDtoCollection.Count
				};
			}
			else
			{
				return new PaginationCollection<TModel>();
			}
		}

		#endregion
	}
}