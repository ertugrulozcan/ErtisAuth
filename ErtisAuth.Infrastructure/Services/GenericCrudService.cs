using Ertis.Core.Collections;
using Ertis.MongoDB.Queries;
using Ertis.MongoDB.Repository;
using ErtisAuth.Core.Events;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Infrastructure.Helpers;

namespace ErtisAuth.Infrastructure.Services;

public abstract class GenericCrudService<TModel> : 
	IGenericCrudService<TModel>
	where TModel : class, Core.Models.IHasIdentifier
{
	#region Services
	
	protected readonly IMongoRepository<TModel> _repository;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="repository"></param>
	protected GenericCrudService(IMongoRepository<TModel> repository)
	{
		this._repository = repository;
	}
	
	#endregion
	
	#region Events
	
	public event EventHandler<CreateResourceEventArgs<TModel>>? OnCreated;
	
	public event EventHandler<UpdateResourceEventArgs<TModel>>? OnUpdated;
	
	public event EventHandler<DeleteResourceEventArgs<TModel>>? OnDeleted;
	
	#endregion
	
	#region Abstract Methods
	
	/// <summary>
	/// Returns the validation errors of the model (empty when valid).
	/// Async, so that validations reading the database do not block the thread.
	/// </summary>
	protected abstract Task<IEnumerable<string>> ValidateModelAsync(TModel model, CancellationToken cancellationToken = default);
	
	protected abstract void Overwrite(TModel destination, TModel source);
	
	protected abstract Task<bool> IsAlreadyExistAsync(TModel model, TModel? exclude = null);
	
	protected abstract ErtisAuthException GetAlreadyExistError(TModel model);
	
	protected abstract ErtisAuthException GetNotFoundError(string id);
	
	#endregion
	
	#region Read Methods
	
	public virtual async Task<TModel?> GetAsync(string id, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		
		return await this._repository.FindOneAsync(id, cancellationToken: cancellationToken);
	}
	
	public virtual async Task<IPaginationCollection<TModel>> GetAsync(
		int? skip = null, 
		int? limit = null, 
		bool withCount = false, 
		string? orderBy = null, 
		SortDirection? sortDirection = null, 
		CancellationToken cancellationToken = default)
	{
		limit ??= Constants.PaginationDefaults.MAX_LIMIT;
		return await this._repository.FindAsync(skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
	}
	
	public virtual async Task<IPaginationCollection<dynamic>> QueryAsync(
		string query, 
		int? skip = null, 
		int? limit = null, 
		bool? withCount = null, 
		string? sortField = null, 
		SortDirection? sortDirection = null,
		IDictionary<string, bool>? selectFields = null, 
		CancellationToken cancellationToken = default)
	{
		limit ??= Constants.PaginationDefaults.MAX_LIMIT;
		return await this._repository.QueryAsync(query, skip, limit, withCount, sortField, sortDirection, selectFields, cancellationToken: cancellationToken);
	}
	
	#endregion
	
	#region Search Methods
	
	public async Task<IPaginationCollection<TModel>> SearchAsync(
		string keyword,
		TextSearchOptions? options = null,
		int? skip = null,
		int? limit = null,
		bool? withCount = null,
		string? sortField = null,
		SortDirection? sortDirection = null, 
		CancellationToken cancellationToken = default)
	{
		limit ??= Constants.PaginationDefaults.MAX_LIMIT;
		return await this._repository.SearchAsync(keyword, options, skip, limit, withCount, sortField, sortDirection, cancellationToken: cancellationToken);
	}
	
	#endregion
	
	#region Create Methods
	
	public virtual async Task<TModel> CreateAsync(Utilizer utilizer, TModel model, CancellationToken cancellationToken = default)
	{
		try
		{
			// Model validation
			var errors = (await this.ValidateModelAsync(model, cancellationToken: cancellationToken)).ToArray();
			if (errors.Length > 0)
			{
				throw ErtisAuthException.ValidationError(errors);
			}
			
			// Check existing
			if (await this.IsAlreadyExistAsync(model))
			{
				throw this.GetAlreadyExistError(model);
			}
			
			// Ensure sys
			SysInfoHelper.SetCreated(model, utilizer);
			
			// Insert to database
			var inserted = await this._repository.InsertAsync(model, cancellationToken: cancellationToken);
			
			this.OnCreated?.Invoke(this, new CreateResourceEventArgs<TModel>(inserted));
			
			return inserted;
		}
		catch (MongoDB.Driver.MongoWriteException ex)
		{
			if (ex.WriteError.Category == MongoDB.Driver.ServerErrorCategory.DuplicateKey)
			{
				throw ErtisAuthException.DuplicateKeyError(ex.WriteError.Message);
			}
            
			throw;
		}
	}
	
	#endregion
	
	#region Update Methods
	
	public virtual async Task<TModel> UpdateAsync(Utilizer utilizer, TModel model, CancellationToken cancellationToken = default)
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
			var errors = (await this.ValidateModelAsync(model, cancellationToken: cancellationToken)).ToArray();
			if (errors.Length > 0)
			{
				throw ErtisAuthException.ValidationError(errors);
			}
			
			// Check existing
			if (await this.IsAlreadyExistAsync(model, current))
			{
				throw this.GetAlreadyExistError(model);
			}
			
			// Ensure sys
			SysInfoHelper.SetModified(model, current, utilizer);
			
			// Update
			var updated = await this._repository.UpdateAsync(model, cancellationToken: cancellationToken);
			
			this.OnUpdated?.Invoke(this, new UpdateResourceEventArgs<TModel>(current, updated));
			
			return updated;
		}
		catch (MongoDB.Driver.MongoWriteException ex)
		{
			if (ex.WriteError.Category == MongoDB.Driver.ServerErrorCategory.DuplicateKey)
			{
				throw ErtisAuthException.DuplicateKeyError(ex.WriteError.Message);
			}
            
			throw;
		}
	}
	
	#endregion
	
	#region Delete Methods
	
	public virtual async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
	{
		var current = await this.GetAsync(id, cancellationToken: cancellationToken);
		if (current != null)
		{
			var isDeleted = await this._repository.DeleteAsync(id, cancellationToken: cancellationToken);
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