using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ertis.Data.Models;
using Ertis.MongoDB.Repository;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Events.EventArgs;
using ErtisAuth.Infrastructure.Mapping;

namespace ErtisAuth.Infrastructure.Services
{
	public abstract class MembershipBoundedCrudService<TModel, TDto> : 
		MembershipBoundedService<TModel, TDto>, 
		IMembershipBoundedCrudService<TModel> 
		where TModel : class, Core.Models.IHasMembership, Core.Models.IHasIdentifier
		where TDto : class, IEntity<string>, Dto.Models.IHasMembership
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="membershipService"></param>
		/// <param name="repository"></param>
		protected MembershipBoundedCrudService(IMembershipService membershipService, IMongoRepository<TDto> repository) : base(membershipService, repository)
		{
			membershipService.RegisterService(this);
		}

		#endregion

		#region Events

		protected event EventHandler<CreateResourceEventArgs<TModel>> OnCreated;
		
		protected event EventHandler<UpdateResourceEventArgs<TModel>> OnUpdated;
		
		protected event EventHandler<DeleteResourceEventArgs<TModel>> OnDeleted;

		#endregion

		#region Abstract Methods

		protected abstract bool ValidateModel(TModel model, out IEnumerable<string> errors);

		protected abstract void Overwrite(TModel destination, TModel source);

		protected abstract bool IsAlreadyExist(TModel model, string membershipId, TModel exclude = default);
		
		protected abstract Task<bool> IsAlreadyExistAsync(TModel model, string membershipId, TModel exclude = default);

		protected abstract ErtisAuthException GetAlreadyExistError(TModel model);

		protected abstract ErtisAuthException GetNotFoundError(string id);
		
		#endregion
		
		#region Virtual Methods

		protected virtual TModel Touch(TModel model)
		{
			return model;
		}
		
		protected virtual async Task<TModel> TouchAsync(TModel model)
		{
			await Task.CompletedTask;
			return model;
		}
		
		#endregion
		
		#region Insert Methods

		public virtual TModel Create(Utilizer utilizer, string membershipId, TModel model)
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
			
			// Touch model
			model = this.Touch(model);

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
			var insertedDto = this.repository.Insert(dto);
			var inserted = Mapper.Current.Map<TDto, TModel>(insertedDto);

			this.OnCreated?.Invoke(this, new CreateResourceEventArgs<TModel>(utilizer, inserted));

			return inserted;
		}
		
		public virtual async ValueTask<TModel> CreateAsync(Utilizer utilizer, string membershipId, TModel model)
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
			
			// Touch model
			model = await this.TouchAsync(model);
			
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
			var insertedDto = await this.repository.InsertAsync(dto);
			var inserted = Mapper.Current.Map<TDto, TModel>(insertedDto);
			
			this.OnCreated?.Invoke(this, new CreateResourceEventArgs<TModel>(utilizer, inserted));

			return inserted;
		}
		
		#endregion

		#region Update Methods

		public virtual TModel Update(Utilizer utilizer, string membershipId, TModel model)
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
			
			// Touch model
			model = this.Touch(model);

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
			var updatedDto = this.repository.Update(dto);
			var updated = Mapper.Current.Map<TDto, TModel>(updatedDto);
			
			this.OnUpdated?.Invoke(this, new UpdateResourceEventArgs<TModel>(utilizer, current, updated));

			return updated;
		}
		
		public virtual async ValueTask<TModel> UpdateAsync(Utilizer utilizer, string membershipId, TModel model)
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
			
			// Touch model
			model = await this.TouchAsync(model);
			
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
			var updatedDto = await this.repository.UpdateAsync(dto);
			var updated = Mapper.Current.Map<TDto, TModel>(updatedDto);
			
			this.OnUpdated?.Invoke(this, new UpdateResourceEventArgs<TModel>(utilizer, current, updated));

			return updated;
		}
		
		protected bool IsIdentical(TModel newModel, TModel currentModel)
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
				var isDeleted = this.repository.Delete(id);
				if (isDeleted)
				{
					this.OnDeleted?.Invoke(this, new DeleteResourceEventArgs<TModel>(utilizer, current));		
				}

				return isDeleted;
			}
			else
			{
				return false;
			}
		}
		
		public virtual async ValueTask<bool> DeleteAsync(Utilizer utilizer, string membershipId, string id)
		{
			var current = await this.GetAsync(membershipId, id);
			if (current != null)
			{
				var isDeleted = await this.repository.DeleteAsync(id);
				if (isDeleted)
				{
					this.OnDeleted?.Invoke(this, new DeleteResourceEventArgs<TModel>(utilizer, current));
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
					var isDeleted = this.repository.Delete(id);
					if (isDeleted)
					{
						this.OnDeleted?.Invoke(this, new DeleteResourceEventArgs<TModel>(utilizer, current));		
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
		
		public virtual async ValueTask<bool?> BulkDeleteAsync(Utilizer utilizer, string membershipId, string[] ids)
		{
			var isAllDeleted = true;
			var isAllFailed = true;
			
			foreach (var id in ids)
			{
				var current = await this.GetAsync(membershipId, id);
				if (current != null)
				{
					var isDeleted = await this.repository.DeleteAsync(id);
					if (isDeleted)
					{
						this.OnDeleted?.Invoke(this, new DeleteResourceEventArgs<TModel>(utilizer, current));		
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
}