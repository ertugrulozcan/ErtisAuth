using System.Linq.Expressions;
using Ertis.Core.Collections;
using Ertis.MongoDB.Repository;
using ErtisAuth.Core.Models;
using NSubstitute;

namespace ErtisAuth.Infrastructure.Tests.Helpers;

/// <summary>
/// Backs a substituted repository with an in-memory list for the read/write calls the services use.
/// Models must carry their ids. Updates replace the stored instance, so a cached instance of the prior version stays distinguishable.
/// </summary>
internal static class InMemoryRepository
{
	#region Methods
	
	/// <param name="repository"></param>
	/// <param name="prepareInsert">Applied to inserted models, e.g. to assign the id a database would generate</param>
	public static List<TModel> Setup<TModel>(IMongoRepository<TModel> repository, Action<TModel>? prepareInsert = null) where TModel : class, IHasIdentifier
	{
		var store = new List<TModel>();
		
		repository
			.FindOneAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => store.FirstOrDefault(x => x.Id == callInfo.ArgAt<string>(0)));
		
		repository
			.FindOneAsync(Arg.Any<Expression<Func<TModel, bool>>>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => store.FirstOrDefault(callInfo.ArgAt<Expression<Func<TModel, bool>>>(0).Compile()));
		
		repository
			.FindOne(Arg.Any<string>())
			.Returns(callInfo => store.FirstOrDefault(x => x.Id == callInfo.ArgAt<string>(0)));
		
		repository
			.FindOne(Arg.Any<Expression<Func<TModel, bool>>>())
			.Returns(callInfo => store.FirstOrDefault(callInfo.ArgAt<Expression<Func<TModel, bool>>>(0).Compile()));
		
		repository
			.FindAsync(default(Expression<Func<TModel, bool>>)!, orderBy: null)
			.ReturnsForAnyArgs(callInfo => ToCollection(store.Where(callInfo.ArgAt<Expression<Func<TModel, bool>>>(0).Compile())));
		
		repository
			.FindAsync(default(Expression<Func<TModel, bool>>)!, sorting: null)
			.ReturnsForAnyArgs(callInfo => ToCollection(store.Where(callInfo.ArgAt<Expression<Func<TModel, bool>>>(0).Compile())));
		
		repository
			.Find(default(Expression<Func<TModel, bool>>)!, orderBy: null)
			.ReturnsForAnyArgs(callInfo => ToCollection(store.Where(callInfo.ArgAt<Expression<Func<TModel, bool>>>(0).Compile())));
		
		repository
			.FindAsync(orderBy: null)
			.ReturnsForAnyArgs(_ => ToCollection(store));
		
		repository
			.Find(orderBy: null)
			.ReturnsForAnyArgs(_ => ToCollection(store));
		
		repository
			.InsertAsync(null!)
			.ReturnsForAnyArgs(callInfo =>
			{
				var model = callInfo.ArgAt<TModel>(0);
				prepareInsert?.Invoke(model);
				return Store(store, model);
			});
		
		repository
			.UpdateAsync(null!)
			.ReturnsForAnyArgs(callInfo => Store(store, callInfo.ArgAt<TModel>(0)));
		
		repository
			.Update(null!)
			.ReturnsForAnyArgs(callInfo => Store(store, callInfo.ArgAt<TModel>(0)));
		
		repository
			.DeleteAsync(null!)
			.ReturnsForAnyArgs(callInfo => store.RemoveAll(x => x.Id == callInfo.ArgAt<string>(0)) > 0);
		
		repository
			.Delete(null!)
			.ReturnsForAnyArgs(callInfo => store.RemoveAll(x => x.Id == callInfo.ArgAt<string>(0)) > 0);
		
		return store;
	}
	
	private static TModel Store<TModel>(List<TModel> store, TModel model) where TModel : class, IHasIdentifier
	{
		store.RemoveAll(x => x.Id == model.Id);
		store.Add(model);
		return model;
	}
	
	private static IPaginationCollection<TModel> ToCollection<TModel>(IEnumerable<TModel> items)
	{
		var array = items.ToArray();
		return new PaginationCollection<TModel>
		{
			Count = array.Length,
			Items = array
		};
	}
	
	#endregion
}
