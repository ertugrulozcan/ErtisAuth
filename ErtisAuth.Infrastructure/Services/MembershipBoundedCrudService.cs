using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Data.Models;
using Ertis.MongoDB.Repository;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Infrastructure.Exceptions;
using ErtisAuth.Infrastructure.Mapping;

namespace ErtisAuth.Infrastructure.Services
{
	public abstract class MembershipBoundedCrudService<TModel, TDto> : IMembershipBoundedCrudService<TModel> 
		where TModel : Core.Models.IHasMembership, Core.Models.IHasIdentifier
		where TDto : IEntity<string>, Dto.Models.IHasMembership
	{
		#region Services

		private readonly IMembershipService membershipService;
		protected readonly IMongoRepository<TDto> repository;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="membershipService"></param>
		/// <param name="repository"></param>
		protected MembershipBoundedCrudService(IMembershipService membershipService, IMongoRepository<TDto> repository)
		{
			this.membershipService = membershipService;
			this.repository = repository;
		}

		#endregion

		#region Abstract Methods

		protected abstract bool ValidateModel(TModel model, out IEnumerable<string> errors);

		protected abstract void Overwrite(TModel destination, TModel source);

		protected abstract bool IsAlreadyExist(TModel model, string membershipId, TModel exclude = default);
		
		protected abstract Task<bool> IsAlreadyExistAsync(TModel model, string membershipId, TModel exclude = default);
		
		protected abstract ErtisAuthException GetAlreadyExistError(TModel model);

		protected abstract ErtisAuthException GetNotFoundError(string id);
		
		#endregion
		
		#region Query Methods

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

		public virtual async Task<TModel> GetAsync(string membershipId, string id)
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
		
		public virtual async Task<IPaginationCollection<TModel>> GetAsync(string membershipId, int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection)
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

		#region Insert Methods

		public virtual TModel Create(string membershipId, TModel model)
		{
			// Check membership
			var membership = this.membershipService.Get(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			else
			{
				model.MembershipId = membershipId;	
			}

			// Model validation
			if (!this.ValidateModel(model, out var errors))
			{
				throw ErtisAuthException.ValidationError(errors);
			}
			
			// Check existing
			if (this.IsAlreadyExist(model, membershipId))
			{
				throw this.GetAlreadyExistError(model);
			}
			
			// Insert to database
			var dto = Mapper.Current.Map<TModel, TDto>(model);
			var inserted = this.repository.Insert(dto);
			return Mapper.Current.Map<TDto, TModel>(inserted);
		}
		
		public virtual async Task<TModel> CreateAsync(string membershipId, TModel model)
		{
			// Check membership
			var membership = await this.membershipService.GetAsync(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			else
			{
				model.MembershipId = membershipId;	
			}
			
			// Model validation
			if (!this.ValidateModel(model, out var errors))
			{
				throw ErtisAuthException.ValidationError(errors);
			}
			
			// Check existing
			if (await this.IsAlreadyExistAsync(model, membershipId))
			{
				throw this.GetAlreadyExistError(model);
			}
			
			// Insert to database
			var dto = Mapper.Current.Map<TModel, TDto>(model);
			var inserted = await this.repository.InsertAsync(dto);
			return Mapper.Current.Map<TDto, TModel>(inserted);
		}
		
		#endregion

		#region Update Methods

		public virtual TModel Update(string membershipId, TModel model)
		{
			// Check membership
			var membership = this.membershipService.Get(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			else
			{
				model.MembershipId = membershipId;	
			}

			// Overwrite
			var current = this.Get(membershipId, model.Id);
			if (current == null)
			{
				throw this.GetNotFoundError(model.Id);
			}
			
			this.Overwrite(model, current);

			// Model validation
			if (!this.ValidateModel(model, out var errors))
			{
				throw ErtisAuthException.ValidationError(errors);
			}
			
			// Check existing
			if (this.IsAlreadyExist(model, membershipId, current))
			{
				throw this.GetAlreadyExistError(model);
			}
			
			model.MembershipId = membershipId;
			var dto = Mapper.Current.Map<TModel, TDto>(model);
			var updated = this.repository.Update(dto);
			return Mapper.Current.Map<TDto, TModel>(updated);
		}
		
		public virtual async Task<TModel> UpdateAsync(string membershipId, TModel model)
		{
			// Check membership
			var membership = await this.membershipService.GetAsync(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			else
			{
				model.MembershipId = membershipId;	
			}
			
			// Overwrite
			var current = await this.GetAsync(membershipId, model.Id);
			if (current == null)
			{
				throw this.GetNotFoundError(model.Id);
			}
			
			this.Overwrite(model, current);
			
			// Model validation
			if (!this.ValidateModel(model, out var errors))
			{
				throw ErtisAuthException.ValidationError(errors);
			}
			
			// Check existing
			if (await this.IsAlreadyExistAsync(model, membershipId, current))
			{
				throw this.GetAlreadyExistError(model);
			}
			
			model.MembershipId = membershipId;
			var dto = Mapper.Current.Map<TModel, TDto>(model);
			var updated = await this.repository.UpdateAsync(dto);
			return Mapper.Current.Map<TDto, TModel>(updated);
		}
		
		#endregion
		
		#region Delete Methods

		public virtual bool Delete(string membershipId, string id)
		{
			var current = this.Get(membershipId, id);
			if (current != null)
			{
				return this.repository.Delete(id);
			}
			else
			{
				return false;
			}
		}
		
		public virtual async Task<bool> DeleteAsync(string membershipId, string id)
		{
			var current = await this.GetAsync(membershipId, id);
			if (current != null)
			{
				return await this.repository.DeleteAsync(id);
			}
			else
			{
				return false;
			}
		}
		
		#endregion
	}
}