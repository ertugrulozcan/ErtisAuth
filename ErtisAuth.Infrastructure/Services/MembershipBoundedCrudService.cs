using Ertis.MongoDB.Repository;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Events;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models;
using ErtisAuth.Infrastructure.Helpers;

namespace ErtisAuth.Infrastructure.Services;

public abstract class MembershipBoundedCrudService<TModel> : 
	MembershipBoundedService<TModel>, 
	IMembershipBoundedCrudService<TModel> 
	where TModel : class, IHasMembership, IHasIdentifier
{
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="membershipService"></param>
	/// <param name="repository"></param>
	protected MembershipBoundedCrudService(IMembershipService membershipService, IMongoRepository<TModel> repository) : base(membershipService, repository)
	{
		membershipService.RegisterService(this);
	}
	
	#endregion
	
	#region Events
	
	protected event EventHandler<CreateResourceEventArgs<TModel>>? OnCreated;
	
	protected event EventHandler<UpdateResourceEventArgs<TModel>>? OnUpdated;
	
	protected event EventHandler<DeleteResourceEventArgs<TModel>>? OnDeleted;
	
	#endregion
	
	#region Abstract Methods
	
	/// <summary>
	/// Returns the validation errors of the model (empty when valid).
	/// Async, so that validations reading the database do not block the thread.
	/// </summary>
	protected abstract Task<IEnumerable<string>> ValidateModelAsync(TModel model, CancellationToken cancellationToken = default);
	
	protected abstract void Overwrite(TModel destination, TModel source);
	
	protected abstract Task<bool> IsAlreadyExistAsync(TModel model, string membershipId, TModel? exclude = null, CancellationToken cancellationToken = default);
	
	protected abstract ErtisAuthException GetAlreadyExistError(TModel model);
	
	protected abstract ErtisAuthException GetNotFoundError(string id);
	
	#endregion
	
	#region Virtual Methods
	
	protected virtual async Task<TModel> TouchAsync(TModel model, CrudOperation crudOperation, CancellationToken cancellationToken = default)
	{
		return await Task.FromResult(model);
	}
	
	#endregion
	
	#region Create Methods
	
	public virtual async Task<TModel> CreateAsync(TModel model, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default)
	{
		// Check membership
		var membership = await this._membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
		if (membership == null)
		{
			throw ErtisAuthException.MembershipNotFound(membershipId);
		}
		else
		{
			model.MembershipId = membershipId;	
		}
		
		// Touch model
		model = await this.TouchAsync(model, CrudOperation.Create, cancellationToken: cancellationToken);
		
		// Model validation
		var errors = (await this.ValidateModelAsync(model, cancellationToken: cancellationToken)).ToArray();
		if (errors.Length > 0)
		{
			throw ErtisAuthException.ValidationError(errors);
		}
		
		// Check existing
		if (await this.IsAlreadyExistAsync(model, membershipId, cancellationToken: cancellationToken))
		{
			throw this.GetAlreadyExistError(model);
		}
		
		// Ensure sys
		SysInfoHelper.SetCreated(model, utilizer);
		
		// Insert to database
		var inserted = await this._repository.InsertAsync(model, cancellationToken: cancellationToken);
		
		// Fire event
		this.OnCreated?.Invoke(this, new CreateResourceEventArgs<TModel>(utilizer, inserted, membershipId));
		
		return inserted;
	}
	
	#endregion
	
	#region Update Methods
	
	public virtual async Task<TModel> UpdateAsync(TModel model, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default)
	{
		// Check membership
		var membership = await this._membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
		if (membership == null)
		{
			throw ErtisAuthException.MembershipNotFound(membershipId);
		}
		else
		{
			model.MembershipId = membershipId;	
		}
		
		// Overwrite
		var current = await this.GetAsync(model.Id, membershipId, cancellationToken: cancellationToken);
		if (current == null)
		{
			throw this.GetNotFoundError(model.Id);
		}
		
		this.Overwrite(model, current);
		
		// Touch model
		model = await this.TouchAsync(model, CrudOperation.Update, cancellationToken: cancellationToken);
		
		// Model validation
		var errors = (await this.ValidateModelAsync(model, cancellationToken: cancellationToken)).ToArray();
		if (errors.Length > 0)
		{
			throw ErtisAuthException.ValidationError(errors);
		}
		
		// Check existing
		if (await this.IsAlreadyExistAsync(model, membershipId, current, cancellationToken: cancellationToken))
		{
			throw this.GetAlreadyExistError(model);
		}
		
		model.MembershipId = membershipId;
		
		// Ensure sys
		SysInfoHelper.SetModified(model, current, utilizer);
		
		// Update
		var updated = await this._repository.UpdateAsync(model, cancellationToken: cancellationToken);
		
		// Fire event
		this.OnUpdated?.Invoke(this, new UpdateResourceEventArgs<TModel>(utilizer, current, updated, membershipId));
		
		return updated;
	}
	
	protected bool IsIdentical(TModel? newModel, TModel? currentModel)
	{
		if (newModel == null || currentModel == null)
		{
			return false;
		}
		
		if (newModel.GetType() != currentModel.GetType())
		{
			return false;
		}
		
		var properties = typeof(TModel).GetProperties();
		foreach (var propertyInfo in properties)
		{
			var oldValue = propertyInfo.GetValue(currentModel);
			var newValue = propertyInfo.GetValue(newModel);
			
			if (oldValue == null && newValue == null)
			{
				continue;
			}
			
			if (oldValue == null || newValue == null)
			{
				return false;
			}
			
			if (oldValue is IEquatable<TModel> equatable)
			{
				if (!equatable.Equals(newValue))
				{
					return false;
				}
			}
			else if (!oldValue.Equals(newValue))
			{
				return false;
			}
		}
		
		return true;
	}
	
	#endregion
	
	#region Delete Methods
	
	public virtual async Task<bool> DeleteAsync(string id, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default)
	{
		var current = await this.GetAsync(id, membershipId, cancellationToken: cancellationToken);
		if (current != null)
		{
			var isDeleted = await this._repository.DeleteAsync(id, cancellationToken: cancellationToken);
			if (isDeleted)
			{
				this.OnDeleted?.Invoke(this, new DeleteResourceEventArgs<TModel>(utilizer, current, membershipId));
			}
			
			return isDeleted;
		}
		else
		{
			return false;
		}
	}
	
	public virtual async Task<bool?> BulkDeleteAsync(string[] ids, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default)
	{
		var isAllDeleted = true;
		var isAllFailed = true;
		
		foreach (var id in ids)
		{
			var current = await this.GetAsync(id, membershipId, cancellationToken: cancellationToken);
			if (current != null)
			{
				var isDeleted = await this._repository.DeleteAsync(id, cancellationToken: cancellationToken);
				if (isDeleted)
				{
					this.OnDeleted?.Invoke(this, new DeleteResourceEventArgs<TModel>(utilizer, current, membershipId));		
				}
				
				isAllDeleted &= isDeleted;
				isAllFailed &= !isDeleted;
			}
		}
		
		if (isAllDeleted)
		{
			return true;
		}
		else if (isAllFailed)
		{
			return false;
		}
		else
		{
			return null;
		}
	}
	
	#endregion
}