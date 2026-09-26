using Ertis.MongoDB.Repository;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Events;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models;

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
	
	protected abstract bool ValidateModel(TModel model, out IEnumerable<string> errors);
	
	protected abstract void Overwrite(TModel destination, TModel source);
	
	protected abstract bool IsAlreadyExist(TModel model, string membershipId, TModel? exclude = null);
	
	protected abstract Task<bool> IsAlreadyExistAsync(TModel model, string membershipId, TModel? exclude = null, CancellationToken cancellationToken = default);
	
	protected abstract ErtisAuthException GetAlreadyExistError(TModel model);
	
	protected abstract ErtisAuthException GetNotFoundError(string id);
	
	#endregion
	
	#region Virtual Methods
	
	// ReSharper disable once UnusedParameter.Global
	// ReSharper disable once VirtualMemberNeverOverridden.Global
	protected virtual TModel Touch(TModel model, CrudOperation crudOperation)
	{
		return model;
	}
	
	protected virtual async Task<TModel> TouchAsync(TModel model, CrudOperation crudOperation, CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
		return model;
	}
	
	#endregion
	
	#region Insert Methods
	
	public virtual TModel Create(Utilizer utilizer, string membershipId, TModel model)
	{
		// Check membership
		var membership = this._membershipService.Get(membershipId);
		if (membership == null)
		{
			throw ErtisAuthException.MembershipNotFound(membershipId);
		}
		else
		{
			model.MembershipId = membershipId;	
		}
		
		// Touch model
		model = this.Touch(model, CrudOperation.Create);
		
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
		var inserted = this._repository.Insert(model);
		
		this.OnCreated?.Invoke(this, new CreateResourceEventArgs<TModel>(utilizer, inserted, membershipId));
		
		return inserted;
	}
	
	public virtual async Task<TModel> CreateAsync(Utilizer utilizer, string membershipId, TModel model, CancellationToken cancellationToken = default)
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
		if (!this.ValidateModel(model, out var errors))
		{
			throw ErtisAuthException.ValidationError(errors);
		}
		
		// Check existing
		if (await this.IsAlreadyExistAsync(model, membershipId, cancellationToken: cancellationToken))
		{
			throw this.GetAlreadyExistError(model);
		}
		
		// Insert to database
		var inserted = await this._repository.InsertAsync(model, cancellationToken: cancellationToken);
		
		this.OnCreated?.Invoke(this, new CreateResourceEventArgs<TModel>(utilizer, inserted, membershipId));
		
		return inserted;
	}
	
	#endregion
	
	#region Update Methods
	
	public virtual TModel Update(Utilizer utilizer, string membershipId, TModel model)
	{
		// Check membership
		var membership = this._membershipService.Get(membershipId);
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
		
		// Touch model
		model = this.Touch(model, CrudOperation.Update);
		
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
		var updated = this._repository.Update(model);
		
		this.OnUpdated?.Invoke(this, new UpdateResourceEventArgs<TModel>(utilizer, current, updated, membershipId));
		
		return updated;
	}
	
	public virtual async Task<TModel> UpdateAsync(Utilizer utilizer, string membershipId, TModel model, CancellationToken cancellationToken = default)
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
		var current = await this.GetAsync(membershipId, model.Id, cancellationToken: cancellationToken);
		if (current == null)
		{
			throw this.GetNotFoundError(model.Id);
		}
		
		this.Overwrite(model, current);
		
		// Touch model
		model = await this.TouchAsync(model, CrudOperation.Update, cancellationToken: cancellationToken);
		
		// Model validation
		if (!this.ValidateModel(model, out var errors))
		{
			throw ErtisAuthException.ValidationError(errors);
		}
		
		// Check existing
		if (await this.IsAlreadyExistAsync(model, membershipId, current, cancellationToken: cancellationToken))
		{
			throw this.GetAlreadyExistError(model);
		}
		
		model.MembershipId = membershipId;
		var updated = await this._repository.UpdateAsync(model, cancellationToken: cancellationToken);
		
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
	
	public virtual bool Delete(Utilizer utilizer, string membershipId, string id)
	{
		var current = this.Get(membershipId, id);
		if (current != null)
		{
			var isDeleted = this._repository.Delete(id);
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
	
	public virtual async Task<bool> DeleteAsync(Utilizer utilizer, string membershipId, string id, CancellationToken cancellationToken = default)
	{
		var current = await this.GetAsync(membershipId, id, cancellationToken: cancellationToken);
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
	
	public virtual bool? BulkDelete(Utilizer utilizer, string membershipId, string[] ids)
	{
		var isAllDeleted = true;
		var isAllFailed = true;
		
		foreach (var id in ids)
		{
			var current = this.Get(membershipId, id);
			if (current != null)
			{
				var isDeleted = this._repository.Delete(id);
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
	
	public virtual async Task<bool?> BulkDeleteAsync(Utilizer utilizer, string membershipId, string[] ids, CancellationToken cancellationToken = default)
	{
		var isAllDeleted = true;
		var isAllFailed = true;
		
		foreach (var id in ids)
		{
			var current = await this.GetAsync(membershipId, id, cancellationToken: cancellationToken);
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