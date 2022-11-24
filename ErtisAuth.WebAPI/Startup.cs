using System;
using Ertis.Data.Repository;
using Ertis.Extensions.AspNetCore.Versioning;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Database;
using Ertis.Net.Rest;
using Ertis.Schema.Serialization;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Dao.Repositories;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Extensions.Authorization.Constants;
using ErtisAuth.Extensions.Mailkit.Extensions;
using ErtisAuth.Extensions.Quartz.Extensions;
using ErtisAuth.Identity.Jwt.Services;
using ErtisAuth.Identity.Jwt.Services.Interfaces;
using ErtisAuth.Infrastructure.Adapters;
using ErtisAuth.Infrastructure.Configuration;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.Integrations.OAuth.Extensions;
using ErtisAuth.WebAPI.Adapters;
using ErtisAuth.WebAPI.Auth;
using ErtisAuth.WebAPI.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

namespace ErtisAuth.WebAPI
{
	public class Startup
	{
		#region Constants

		private const string CORS_POLICY_KEY = "cors-policy";

		#endregion

		#region Properties

		public IConfiguration Configuration { get; }
		
		private int ApiVersion { get; set; }

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="configuration"></param>
		public Startup(IConfiguration configuration)
		{
			this.Configuration = configuration;
		}

		#endregion
		
		#region Methods

		public void ConfigureServices(IServiceCollection services)
		{
			Program.SetEnvironmentParameter("Version", this.Configuration.GetValue<string>("Version"));
			
			services.Configure<DatabaseSettings>(this.Configuration.GetSection("Database"));
			services.AddSingleton<IDatabaseSettings>(serviceProvider =>
			{
				var databaseSettings = serviceProvider.GetRequiredService<IOptions<DatabaseSettings>>().Value;
				var connectionString = Ertis.MongoDB.Helpers.ConnectionStringHelper.GenerateConnectionString(databaseSettings);
				Console.WriteLine($"ConnectionString: '{connectionString}'");
				return databaseSettings;
			});

			services.Configure<ApiVersionOptions>(this.Configuration.GetSection("ApiVersion"));
			services.AddSingleton<IApiVersionOptions>(serviceProvider => serviceProvider.GetRequiredService<IOptions<ApiVersionOptions>>().Value);
			
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
			services.AddSingleton<IEventRepository, EventRepository>();
			services.AddSingleton<IRepositoryActionBinder, SysUpserter>();
			
			services.AddSingleton<ICryptographyService, CryptographyService>();
			services.AddSingleton<IJwtService, JwtService>();
			services.AddSingleton<ITokenService, TokenService>();
			services.AddSingleton<IActiveTokenService, ActiveTokenService>();
			services.AddSingleton<IRevokedTokenService, RevokedTokenService>();
			services.AddSingleton<IAccessControlService, AccessControlService>();
			services.AddSingleton<IMembershipService, MembershipService>();
			services.AddSingleton<IUserTypeService, UserTypeService>();
			services.AddSingleton<IUserService, UserService>();
			services.AddSingleton<IApplicationService, ApplicationService>();
			services.AddSingleton<IRoleService, RoleService>();
			services.AddSingleton<IProviderService, ProviderService>();
			services.AddSingleton<IWebhookService, WebhookService>();
			services.AddSingleton<IMailHookService, MailHookService>();
			services.AddSingleton<IEventService, EventService>();
			services.AddSingleton<IMigrationService, MigrationService>();
			
			services.AddSingleton<IRestHandler, SystemRestHandler>();
			services.AddSingleton<IScopeOwnerAccessor, ScopeOwnerAccessor>();
			services.AddSingleton<IAuthorizationHandler, ErtisAuthAuthorizationHandler>();
			services.AddProviders();
			
			// Mailkit
			services.AddMailkit();

			// GeoLocation Tracking
			services.Configure<GeoLocationOptions>(this.Configuration.GetSection("GeoLocationTracking"));
			services.AddSingleton<IGeoLocationOptions>(serviceProvider => serviceProvider.GetRequiredService<IOptions<GeoLocationOptions>>().Value);
			if (this.Configuration.GetSection("GeoLocationTracking").GetValue<bool>("Enabled"))
			{
				var provider = this.Configuration.GetSection("GeoLocationTracking").GetValue<string>("Provider");
				if (!string.IsNullOrEmpty(provider))
				{
					switch (provider)
					{
						case "MaxMind":
							services.Configure<MaxMindOptions>(this.Configuration.GetSection("MaxMind"));
							services.AddSingleton<IMaxMindOptions>(serviceProvider => serviceProvider.GetRequiredService<IOptions<MaxMindOptions>>().Value);
							services.AddSingleton<IGeoLocationService, MaxMindGeoLocationService>();
							break;
						case "Ip2Location":
							services.Configure<Ip2LocationOptions>(this.Configuration.GetSection("Ip2Location"));
							services.AddSingleton<IIp2LocationOptions>(serviceProvider => serviceProvider.GetRequiredService<IOptions<Ip2LocationOptions>>().Value);
							services.AddSingleton<IGeoLocationService, Ip2LocationService>();
							break;
						default:
							Console.WriteLine("Unknown geo location provider: " + provider);
							services.AddSingleton<IGeoLocationService, GeoLocationDisabledService>();
							break;
					}
				}
				else
				{
					Console.WriteLine("Geo location provider is undefined");
					services.AddSingleton<IGeoLocationService, GeoLocationDisabledService>();
				}
			}
			else
			{
				services.AddSingleton<IGeoLocationService, GeoLocationDisabledService>();
			}
			
			services.AddHttpContextAccessor();
			
			services.AddCors(options =>
			{
				options.AddPolicy(CORS_POLICY_KEY,
					builder =>
					{
						builder
							.AllowAnyOrigin()
							.AllowAnyMethod()
							.AllowAnyHeader();
					});
			});

			services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, ErtisAuthAuthenticationHandler>(
				Policies.ErtisAuthAuthorizationPolicyName, _ => {});
			
			services.AddAuthorization(options =>
				options.AddPolicy(Policies.ErtisAuthAuthorizationPolicyName, policy =>
				{
					policy.AddAuthenticationSchemes(Policies.ErtisAuthAuthorizationPolicyName);
					policy.AddRequirements(new ErtisAuthAuthorizationRequirement());
				}));

			// Api versioning
			var major = this.Configuration.GetSection("ApiVersion").GetValue<int>("Major");
			var minor = this.Configuration.GetSection("ApiVersion").GetValue<int>("Minor");
			this.ApiVersion = major;
			
			services.AddApiVersioning(o => {
				o.ReportApiVersions = true;
				o.AssumeDefaultVersionWhenUnspecified = true;
				o.DefaultApiVersion = new ApiVersion(major, minor);
			});
			
			// Quartz
			services.AddQuartzJobs();

			services
				.AddControllers()
				.AddNewtonsoftJson(options =>
				{
					options.SerializerSettings.Converters.Add(new DynamicObjectJsonConverter());
					options.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
				});
			
			// Swagger
			if (this.Configuration.GetSection("Documentation").GetValue<bool>("SwaggerEnabled"))
			{
				var swaggerVersion = $"v{this.ApiVersion}";
				services.AddSwaggerGen(c => { c.SwaggerDoc(swaggerVersion, new OpenApiInfo { Title = "ErtisAuth.WebAPI", Version = swaggerVersion }); });	
			}
		}
		
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			
			this.ResolveRequiredServices(app.ApplicationServices);
			app.UseProviders();

			// Swagger
			if (this.Configuration.GetSection("Documentation").GetValue<bool>("SwaggerEnabled"))
			{
				app.UseSwagger();
				app.UseSwaggerUI(options =>
				{
					var swaggerVersion = $"V{this.ApiVersion}";
					options.SwaggerEndpoint("/swagger/v1/swagger.json", "ErtisAuth.WebAPI " + swaggerVersion);
					options.DefaultModelsExpandDepth(-1);
				});	
			}

			app.UseCors(CORS_POLICY_KEY);
			app.UseHttpsRedirection();
			app.UseRouting();
			app.UseAuthentication();
			app.UseAuthorization();
			app.ConfigureGlobalExceptionHandler();
			
			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});
		}

		private void ResolveRequiredServices(IServiceProvider serviceProvider)
		{
			serviceProvider.GetRequiredService<IUserTypeService>();
			serviceProvider.GetRequiredService<IUserService>();
			serviceProvider.GetRequiredService<IApplicationService>();
			serviceProvider.GetRequiredService<IRoleService>();
			serviceProvider.GetRequiredService<IProviderService>();
			serviceProvider.GetRequiredService<IWebhookService>();
			serviceProvider.GetRequiredService<IMailHookService>();
		}

		#endregion
	}
}