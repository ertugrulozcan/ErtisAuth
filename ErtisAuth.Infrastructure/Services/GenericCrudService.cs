using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Data.Models;
using Ertis.MongoDB.Repository;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Events.EventArgs;
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
		
		#region Events

		public event EventHandler<CreateResourceEventArgs<TModel>> OnCreated;
		
		public event EventHandler<UpdateResourceEventArgs<TModel>> OnUpdated;
		
		public event EventHandler<DeleteResourceEventArgs<TModel>> OnDeleted;

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
		
		public async ValueTask<TModel> GetAsync(string id)
		{
			var dto = await this.repository.FindOneAsync(id);
			return Mapper.Current.Map<TDto, TModel>(dto);
		}
		
		public IPaginationCollection<TModel> Get(int? skip = null, int? limit = null, bool withCount = false, string orderBy = null, SortDirection? sortDirection = null)
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
		
		public async ValueTask<IPaginationCollection<TModel>> GetAsync(int? skip = null, int? limit = null, bool withCount = false, string orderBy = null, SortDirection? sortDirection = null)
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
			var insertedDto = this.repository.Insert(dto);
			var inserted = Mapper.Current.Map<TDto, TModel>(insertedDto);
			
			this.OnCreated?.Invoke(this, new CreateResourceEventArgs<TModel>(inserted));

			return inserted;
		}
		
		public virtual async ValueTask<TModel> CreateAsync(TModel model)
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
			var insertedDto = await this.repository.InsertAsync(dto);
			var inserted = Mapper.Current.Map<TDto, TModel>(insertedDto);
			
			this.OnCreated?.Invoke(this, new CreateResourceEventArgs<TModel>(inserted));

			return inserted;
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
			var updatedDto = this.repository.Update(dto);
			var updated = Mapper.Current.Map<TDto, TModel>(updatedDto);

			this.OnUpdated?.Invoke(this, new UpdateResourceEventArgs<TModel>(current, updated));

			return updated;
		}
		
		public virtual async ValueTask<TModel> UpdateAsync(TModel model)
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
			var updatedDto = await this.repository.UpdateAsync(dto);
			var updated = Mapper.Current.Map<TDto, TModel>(updatedDto);

			this.OnUpdated?.Invoke(this, new UpdateResourceEventArgs<TModel>(current, updated));

			return updated;
		}
		
		#endregion
		
		#region Delete Methods

		public virtual bool Delete(string id)
		{
			var current = this.Get(id);
			if (current != null)
			{
				var isDeleted = this.repository.Delete(id);
				if (isDeleted)
				{
					this.OnDeleted?.Invoke(this, new DeleteResourceEventArgs<TModel>(current));	
				}

				return isDeleted;
			}
			else
			{
				return false;
			}
		}
		
		public virtual async ValueTask<bool> DeleteAsync(string id)
		{
			var current = await this.GetAsync(id);
			if (current != null)
			{
				var isDeleted = await this.repository.DeleteAsync(id);
				if (isDeleted)
				{
					this.OnDeleted?.Invoke(this, new DeleteResourceEventArgs<TModel>(current));	
				}

				return isDeleted;
			}
			else
			{
				return false;
			}
		}
		
		#endregion
	}
}