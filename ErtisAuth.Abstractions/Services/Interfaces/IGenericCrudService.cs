using System;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using ErtisAuth.Events.EventArgs;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface IGenericCrudService<T>
	{
		#region Events

		event EventHandler<CreateResourceEventArgs<T>> OnCreated;
		
		event EventHandler<UpdateResourceEventArgs<T>> OnUpdated;
		
		event EventHandler<DeleteResourceEventArgs<T>> OnDeleted;

		#endregion
		
		#region Methods

		T Get(string id);
		
		Task<T> GetAsync(string id);

		IPaginationCollection<T> Get(int? skip = null, int? limit = null, bool withCount = false, string orderBy = null, SortDirection? sortDirection = null);
		
		Task<IPaginationCollection<T>> GetAsync(int? skip = null, int? limit = null, bool withCount = false, string orderBy = null, SortDirection? sortDirection = null);
		
		T Create(T model);
		
		Task<T> CreateAsync(T model);

		T Update(T model);
		
		Task<T> UpdateAsync(T model);

		bool Delete(string id);

		Task<bool> DeleteAsync(string id);

		#endregion
	}
}