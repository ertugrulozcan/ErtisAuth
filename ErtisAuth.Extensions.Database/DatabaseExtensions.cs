using Ertis.Core.Models;
using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Database;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Dao.Repositories;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dao.Serialization;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using IMongoDatabase = Ertis.MongoDB.Database.IMongoDatabase;

namespace ErtisAuth.Extensions.Database;

public static class DatabaseExtensions
{
	#region Fields
	
	private static int _isSerializationRegistered;
	
	#endregion
	
	#region Methods
	
	public static void AddMongoDB(this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<DatabaseSettings>(configuration.GetSection("Database"));
		services.AddSingleton<IDatabaseSettings>(serviceProvider => serviceProvider.GetRequiredService<IOptions<DatabaseSettings>>().Value);
		
		services.AddSingleton<IMongoClientProvider>(serviceProvider =>
		{
			var databaseSettings = serviceProvider.GetRequiredService<IDatabaseSettings>();
			return new MongoClientProvider(MongoClientSettings.FromConnectionString(databaseSettings.ConnectionString));
		});
		
		services.AddSingleton<IMongoDatabase, MongoDatabase>();
		services.AddSingleton<IMembershipRepository, MembershipRepository>();
		services.AddSingleton<IUserTypeRepository, UserTypeRepository>();
		services.AddSingleton<IUserRepository, UserRepository>();
		services.AddSingleton<IApplicationRepository, ApplicationRepository>();
		services.AddSingleton<IRoleRepository, RoleRepository>();
		services.AddSingleton<IWebhookRepository, WebhookRepository>();
		services.AddSingleton<IMailHookRepository, MailHookRepository>();
		services.AddSingleton<IProviderRepository, ProviderRepository>();
		services.AddSingleton<IActiveTokensRepository, ActiveTokensRepository>();
		services.AddSingleton<IRevokedTokensRepository, RevokedTokensRepository>();
		services.AddSingleton<ITokenCodeRepository, TokenCodeRepository>();
		services.AddSingleton<ICodePolicyRepository, CodePolicyRepository>();
		services.AddSingleton<IOneTimePasswordRepository, OneTimePasswordRepository>();
		services.AddSingleton<IEventRepository, EventRepository>();
		services.AddSingleton<ISetupTokenRepository, SetupTokenRepository>();
		
		// BSON registrations are process-wide and throw when repeated (e.g. several hosts in one test process)
		if (Interlocked.Exchange(ref _isSerializationRegistered, 1) == 0)
		{
			RegisterClassMaps();
			RegisterDiscriminatorConventions();
		}
	}
	
	private static void RegisterClassMaps()
	{
		BsonClassMap.RegisterClassMap<SysModel>(classMap =>
		{
			classMap.AutoMap();
			
			classMap.MapMember(x => x.CreatedAt).SetElementName("created_at");
			classMap.MapMember(x => x.CreatedBy).SetElementName("created_by");
			classMap.MapMember(x => x.ModifiedAt).SetElementName("modified_at").SetIgnoreIfNull(true);
			classMap.MapMember(x => x.ModifiedBy).SetElementName("modified_by").SetIgnoreIfNull(true);
		});
		
		BearerTokenClassMap.Register();
	}
	
	private static void RegisterDiscriminatorConventions()
	{
		// IMailProvider
		MailProviderDiscriminatorConvention.Register();
	}
	
	public static async Task UseMongoDB(this IApplicationBuilder app)
	{
		await CheckDatabaseIndexesAsync(app.ApplicationServices);
		await app.SynchronizeUniqueIndexesAsync();
	}
	
	private static async Task CheckDatabaseIndexesAsync(IServiceProvider serviceProvider)
	{
		var interfaces = typeof(IRepositoryBase).Assembly.GetTypes().Where(x => x.IsInterface && x != typeof(IRepositoryBase));
		foreach (var serviceType in interfaces)
		{
			var service = serviceProvider.GetService(serviceType);
			if (service is IRepositoryBase repository)
			{
				await repository.CreateIndexesAsync();
			}
		}
	}
	
	/// <summary>
	/// Brings the unique indexes of the users collection in line with the user types (e.g. after a manual change in the database).
	/// Failures (e.g. duplicate values in existing users) are logged, the application still starts.
	/// </summary>
	private static async Task SynchronizeUniqueIndexesAsync(this IApplicationBuilder app)
	{
		await app.ApplicationServices.GetRequiredService<IUserUniqueIndexSynchronizer>().SynchronizeAllAsync();
	}
	
	#endregion
}