using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Data.Models;
using Ertis.MongoDB.Queries;
using Ertis.MongoDB.Repository;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Events.EventArgs;
using ErtisAuth.Infrastructure.Mapping;

namespace ErtisAuth.Infrastructure.Services
{
	public abstract class GenericCrudService<TModel, TDto> : 
		IGenericCrudService<TModel>
		where TModel : class, Core.Models.IHasIdentifier
		where TDto : class, IEntity<string>
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

		public virtual TModel Get(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				return null;
			}
			
			var dto = this.repository.FindOne(id);
			return dto == null ? null : Mapper.Current.Map<TDto, TModel>(dto);
		}
		
		public virtual async ValueTask<TModel> GetAsync(string id, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrEmpty(id))
			{
				return null;
			}
			
			var dto = await this.repository.FindOneAsync(id, cancellationToken: cancellationToken);
			return dto == null ? null : Mapper.Current.Map<TDto, TModel>(dto);
		}
		
		public virtual IPaginationCollection<TModel> Get(int? skip = null, int? limit = null, bool withCount = false, string orderBy = null, SortDirection? sortDirection = null)
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
		
		public virtual async ValueTask<IPaginationCollection<TModel>> GetAsync(int? skip = null, int? limit = null, bool withCount = false, string orderBy = null, SortDirection? sortDirection = null, CancellationToken cancellationToken = default)
		{
			var paginatedDtoCollection = await this.repository.FindAsync(expression: null, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
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
		
		public virtual async Task<IPaginationCollection<dynamic>> QueryAsync(
			string query, 
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string sortField = null, 
			SortDirection? sortDirection = null,
			IDictionary<string, bool> selectFields = null, 
			CancellationToken cancellationToken = default)
		{
			return await this.repository.QueryAsync(query, skip, limit, withCount, sortField, sortDirection, selectFields, cancellationToken: cancellationToken);
		}

		#endregion
		
		#region Search Methods

		public IPaginationCollection<TModel> Search(
			string keyword,
			TextSearchOptions options = null,
			int? skip = null,
			int? limit = null,
			bool? withCount = null,
			string sortField = null,
			SortDirection? sortDirection = null)
		{
			var paginatedDtoCollection = this.repository.Search(keyword, options, skip, limit, withCount, sortField, sortDirection);
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

		public async ValueTask<IPaginationCollection<TModel>> SearchAsync(
			string keyword,
			TextSearchOptions options = null,
			int? skip = null,
			int? limit = null,
			bool? withCount = null,
			string sortField = null,
			SortDirection? sortDirection = null, 
			CancellationToken cancellationToken = default)
		{
			var paginatedDtoCollection = await this.repository.SearchAsync(keyword, options, skip, limit, withCount, sortField, sortDirection, cancellationToken: cancellationToken);
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

		public virtual TModel Create(TModel model)
		{
			try
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
			catch (MongoDB.Driver.MongoWriteException ex)
			{
				if (ex.WriteError.Category == MongoDB.Driver.ServerErrorCategory.DuplicateKey)
				{
					throw ErtisAuthException.DuplicateKeyError(ex.WriteError.Message);
				}
                
				Console.WriteLine(ex);
				throw;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				throw;
			}
		}
		
		public virtual async ValueTask<TModel> CreateAsync(TModel model, CancellationToken cancellationToken = default)
		{
			try
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
				var insertedDto = await this.repository.InsertAsync(dto, cancellationToken: cancellationToken);
				var inserted = Mapper.Current.Map<TDto, TModel>(insertedDto);
			
				this.OnCreated?.Invoke(this, new CreateResourceEventArgs<TModel>(inserted));

				return inserted;
			}
			catch (MongoDB.Driver.MongoWriteException ex)
			{
				if (ex.WriteError.Category == MongoDB.Driver.ServerErrorCategory.DuplicateKey)
				{
					throw ErtisAuthException.DuplicateKeyError(ex.WriteError.Message);
				}
                
				Console.WriteLine(ex);
				throw;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				throw;
			}
		}
		
		#endregion

		#region Update Methods

		public virtual TModel Update(TModel model)
		{
			try
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
			catch (MongoDB.Driver.MongoWriteException ex)
			{
				if (ex.WriteError.Category == MongoDB.Driver.ServerErrorCategory.DuplicateKey)
				{
					throw ErtisAuthException.DuplicateKeyError(ex.WriteError.Message);
				}
                
				Console.WriteLine(ex);
				throw;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				throw;
			}
		}
		
		public virtual async ValueTask<TModel> UpdateAsync(TModel model, CancellationToken cancellationToken = default)
		{
			try
			{
				// Overwrite
				var current = await this.GetAsync(model.Id, cancellationToken: cancellationToken);
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
				var updatedDto = await this.repository.UpdateAsync(dto, cancellationToken: cancellationToken);
				var updated = Mapper.Current.Map<TDto, TModel>(updatedDto);

				this.OnUpdated?.Invoke(this, new UpdateResourceEventArgs<TModel>(current, updated));

				return updated;
			}
			catch (MongoDB.Driver.MongoWriteException ex)
			{
				if (ex.WriteError.Category == MongoDB.Driver.ServerErrorCategory.DuplicateKey)
				{
					throw ErtisAuthException.DuplicateKeyError(ex.WriteError.Message);
				}
                
				Console.WriteLine(ex);
				throw;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				throw;
			}
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
		
		public virtual async ValueTask<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
		{
			var current = await this.GetAsync(id, cancellationToken: cancellationToken);
			if (current != null)
			{
				var isDeleted = await this.repository.DeleteAsync(id, cancellationToken: cancellationToken);
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