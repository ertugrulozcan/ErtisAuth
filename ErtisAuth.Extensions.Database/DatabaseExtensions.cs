using ErtisAuth.Dao.Repositories.Interfaces;
using Microsoft.AspNetCore.Builder;

namespace ErtisAuth.Extensions.Database;

public static class DatabaseExtensions
{
	#region Methods

	public static void CheckDatabaseIndexes(this IApplicationBuilder app)
	{
		CheckDatabaseIndexesAsync(app.ApplicationServices).ConfigureAwait(false).GetAwaiter().GetResult();
	}
	
	private static async Task CheckDatabaseIndexesAsync(IServiceProvider serviceProvider)
	{
		var interfaces = typeof(IRepositoryBase).Assembly.GetTypes()
			.Where(x => x.IsInterface && x != typeof(IRepositoryBase));
		
		foreach (var serviceType in interfaces)
		{
			var service = serviceProvider.GetService(serviceType);
			if (service is IRepositoryBase repository)
			{
				await repository.CreateIndexesAsync();
			}
		}
	}
	
	#endregion
}