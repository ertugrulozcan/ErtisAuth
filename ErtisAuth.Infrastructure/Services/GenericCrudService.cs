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
	public abstract class GenericCrudService<TModel, TDto> : 
		IGenericCrudService<TModel>
		where TModel : Core.Models.IHasIdentifier
		where TDto : IEntity<string>
	{
		#region Services

		protected readonly IMongoRepository<TDto> repository;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="repository"></param>
		protected GenericCrudService(IMongoRepository<TDto> repository)
		{
			this.repository = repository;
		}

		#endregion
		
		#region Abstract Methods

		protected abstract bool ValidateModel(TModel model, out IEnumerable<string> errors);

		protected abstract void Overwrite(TModel destination, TModel source);

		protected abstract bool IsAlreadyExist(TModel model, TModel exclude = default);
		
		protected abstract Task<bool> IsAlreadyExistAsync(TModel model, TModel exclude = default);
		
		protected abstract ErtisAuthException GetAlreadyExistError(TModel model);

		protected abstract ErtisAuthException GetNotFoundError(string id);
		
		#endregion
		
		#region Read Methods

		public TModel Get(string id)
		{
			var dto = this.repository.FindOne(id);
			return Mapper.Current.Map<TDto, TModel>(dto);
		}
		
		public async Task<TModel> GetAsync(string id)
		{
			var dto = await this.repository.FindOneAsync(id);
			return Mapper.Current.Map<TDto, TModel>(dto);
		}
		
		public IPaginationCollection<TModel> Get(int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection)
		{
			var paginatedDtoCollection = this.repository.Find(expression: null, skip, limit, withCount, orderBy, sortDirection);
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
		
		public async Task<IPaginationCollection<TModel>> GetAsync(int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection)
		{
			var paginatedDtoCollection = await this.repository.FindAsync(expression: null, skip, limit, withCount, orderBy, sortDirection);
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
		
		#region Insert Methods

		public virtual TModel Create(TModel model)
		{
			// Model validation
			if (!this.ValidateModel(model, out var errors))
			{
				throw ErtisAuthException.ValidationError(errors);
			}
			
			// Check existing
			if (this.IsAlreadyExist(model))
			{
				throw this.GetAlreadyExistError(model);
			}
			
			// Insert to database
			var dto = Mapper.Current.Map<TModel, TDto>(model);
			var inserted = this.repository.Insert(dto);
			return Mapper.Current.Map<TDto, TModel>(inserted);
		}
		
		public virtual async Task<TModel> CreateAsync(TModel model)
		{
			// Model validation
			if (!this.ValidateModel(model, out var errors))
			{
				throw ErtisAuthException.ValidationError(errors);
			}
			
			// Check existing
			if (await this.IsAlreadyExistAsync(model))
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

		public virtual TModel Update(TModel model)
		{
			// Overwrite
			var current = this.Get(model.Id);
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
			if (this.IsAlreadyExist(model, current))
			{
				throw this.GetAlreadyExistError(model);
			}
			
			var dto = Mapper.Current.Map<TModel, TDto>(model);
			var updated = this.repository.Update(dto);
			return Mapper.Current.Map<TDto, TModel>(updated);
		}
		
		public virtual async Task<TModel> UpdateAsync(TModel model)
		{
			// Overwrite
			var current = await this.GetAsync(model.Id);
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
			if (await this.IsAlreadyExistAsync(model, current))
			{
				throw this.GetAlreadyExistError(model);
			}
			
			var dto = Mapper.Current.Map<TModel, TDto>(model);
			var updated = await this.repository.UpdateAsync(dto);
			return Mapper.Current.Map<TDto, TModel>(updated);
		}
		
		#endregion
		
		#region Delete Methods

		public virtual bool Delete(string id)
		{
			var current = this.Get(id);
			if (current != null)
			{
				return this.repository.Delete(id);
			}
			else
			{
				return false;
			}
		}
		
		public virtual async Task<bool> DeleteAsync(string id)
		{
			var current = await this.GetAsync(id);
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