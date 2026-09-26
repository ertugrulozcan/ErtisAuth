using Ertis.Core.Collections;
using ErtisAuth.Core.Events;
using Ertis.MongoDB.Queries;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global
namespace ErtisAuth.Abstractions.Services;

public interface IGenericCrudService<T>
{
	#region Events
	
	event EventHandler<CreateResourceEventArgs<T>> OnCreated;
	
	event EventHandler<UpdateResourceEventArgs<T>> OnUpdated;
	
	event EventHandler<DeleteResourceEventArgs<T>> OnDeleted;
	
	#endregion
	
	#region Methods
	
	T? Get(string id);
	
	Task<T?> GetAsync(string id, CancellationToken cancellationToken = default);
	
	IPaginationCollection<T> Get(
		int? skip = null, 
		int? limit = null, 
		bool withCount = false, 
		string? orderBy = null, 
		SortDirection? sortDirection = null);
	
	Task<IPaginationCollection<T>> GetAsync(
		int? skip = null, 
		int? limit = null, 
		bool withCount = false, 
		string? orderBy = null, 
		SortDirection? sortDirection = null, 
		CancellationToken cancellationToken = default);
	
	IPaginationCollection<T> Search(
		string keyword, 
		TextSearchOptions? options = null,
		int? skip = null, 
		int? limit = null,
		bool? withCount = null, 
		string? sortField = null, 
		SortDirection? sortDirection = null);
	
	Task<IPaginationCollection<T>> SearchAsync(
		string keyword, 
		TextSearchOptions? options = null,
		int? skip = null, 
		int? limit = null,
		bool? withCount = null, 
		string? sortField = null, 
		SortDirection? sortDirection = null, 
		CancellationToken cancellationToken = default);
	
	T Create(T model);
	
	Task<T> CreateAsync(T model, CancellationToken cancellationToken = default);
	
	T Update(T model);
	
	Task<T> UpdateAsync(T model, CancellationToken cancellationToken = default);
	
	bool Delete(string id);
	
	Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
	
	#endregion
}