using Ertis.Core.Collections;
using ErtisAuth.Core.Events;
using ErtisAuth.Core.Models.Identity;
using Ertis.MongoDB.Queries;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable EventNeverSubscribedTo.Global
namespace ErtisAuth.Abstractions.Services;

public interface IGenericCrudService<T>
{
	#region Events
	
	event EventHandler<CreateResourceEventArgs<T>> OnCreated;
	
	event EventHandler<UpdateResourceEventArgs<T>> OnUpdated;
	
	event EventHandler<DeleteResourceEventArgs<T>> OnDeleted;
	
	#endregion
	
	#region Methods
	
	Task<T?> GetAsync(string id, CancellationToken cancellationToken = default);
	
	Task<IPaginationCollection<T>> GetAsync(
		int? skip = null, 
		int? limit = null, 
		bool withCount = false, 
		string? orderBy = null, 
		SortDirection? sortDirection = null, 
		CancellationToken cancellationToken = default);
	
	Task<IPaginationCollection<T>> SearchAsync(
		string keyword, 
		TextSearchOptions? options = null,
		int? skip = null, 
		int? limit = null,
		bool? withCount = null, 
		string? sortField = null, 
		SortDirection? sortDirection = null, 
		CancellationToken cancellationToken = default);
	
	Task<T> CreateAsync(Utilizer utilizer, T model, CancellationToken cancellationToken = default);
	
	Task<T> UpdateAsync(Utilizer utilizer, T model, CancellationToken cancellationToken = default);
	
	Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
	
	#endregion
}