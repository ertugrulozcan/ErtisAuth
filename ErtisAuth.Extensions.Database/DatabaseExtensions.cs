using Ertis.Core.Models.Resources;
using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Database;
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
		
		RegisterClassMaps();
		RegisterDiscriminatorConventions();
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
	}
	
	private static void RegisterDiscriminatorConventions()
	{
		// IMailProvider
		MailProviderDiscriminatorConvention.Register();
	}
	
	public static void UseMongoDB(this IApplicationBuilder app)
	{
		CheckDatabaseIndexesAsync(app.ApplicationServices).ConfigureAwait(false).GetAwaiter().GetResult();
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
	
	#endregion
}