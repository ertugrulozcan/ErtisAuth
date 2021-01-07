using Ertis.Data.Repository;
using Ertis.MongoDB.Configuration;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Dao.Repositories;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Identity.Jwt.Services;
using ErtisAuth.Identity.Jwt.Services.Interfaces;
using ErtisAuth.Infrastructure.Adapters;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.WebAPI.Constants;
using ErtisAuth.WebAPI.Extensions;
using ErtisAuth.WebAPI.Models;
using ErtisAuth.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Sentry;

namespace ErtisAuth.WebAPI
{
	public class Startup
	{
		#region Constants

		private const string CORS_POLICY_KEY = "cors-policy";

		#endregion

		#region Properties

		public IConfiguration Configuration { get; }

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
			services.Configure<DatabaseSettings>(this.Configuration.GetSection("Database"));
			services.AddSingleton<IDatabaseSettings>(serviceProvider => serviceProvider.GetRequiredService<IOptions<DatabaseSettings>>().Value);
			
			services.Configure<ApiVersionOptions>(this.Configuration.GetSection("ApiVersion"));
			services.AddSingleton<IApiVersionOptions>(serviceProvider => serviceProvider.GetRequiredService<IOptions<ApiVersionOptions>>().Value);
			
			services.AddSingleton<IMembershipRepository, MembershipRepository>();
			services.AddSingleton<IUserRepository, UserRepository>();
			services.AddSingleton<IApplicationRepository, ApplicationRepository>();
			services.AddSingleton<IRoleRepository, RoleRepository>();
			services.AddSingleton<IProviderRepository, ProviderRepository>();
			services.AddSingleton<IRevokedTokensRepository, RevokedTokensRepository>();
			services.AddSingleton<IEventRepository, EventRepository>();
			services.AddSingleton<IRepositoryActionBinder, SysUpserter>();
			
			services.AddSingleton<ICryptographyService, CryptographyService>();
			services.AddSingleton<IJwtService, JwtService>();
			services.AddSingleton<ITokenService, TokenService>();
			services.AddSingleton<IMembershipService, MembershipService>();
			services.AddSingleton<IUserService, UserService>();
			services.AddSingleton<IApplicationService, ApplicationService>();
			services.AddSingleton<IRoleService, RoleService>();
			services.AddSingleton<IProviderService, ProviderService>();
			services.AddSingleton<IEventService, EventService>();
			
			services.AddSingleton<IScopeOwnerAccessor, ScopeOwnerAccessor>();
			services.AddSingleton<IAuthorizationHandler, ErtisAuthAuthorizationHandler>();
			
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

			services.AddAuthorization(options =>
			{
				options.AddPolicy(Policies.ErtisAuthAuthorizationPolicyName, policy => policy.AddRequirements(new ErtisAuthAuthorizationRequirement()));
			});
			
			// Api versioning
			int major = this.Configuration.GetSection("ApiVersion").GetValue<int>("Major");
			int minor = this.Configuration.GetSection("ApiVersion").GetValue<int>("Minor");
			services.AddApiVersioning(o => {
				o.ReportApiVersions = true;
				o.AssumeDefaultVersionWhenUnspecified = true;
				o.DefaultApiVersion = new ApiVersion(major, minor);
			});
			
			// Swagger
			if (this.Configuration.GetSection("Documentation").GetValue<bool>("SwaggerEnabled"))
			{
				services.AddSwaggerGen();
			}

			services
				.AddControllers()
				.AddNewtonsoftJson();
		}
		
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			var sentryHub = app.ApplicationServices.GetRequiredService<IHub>();

			app.UseCors(CORS_POLICY_KEY);
			app.UseHttpsRedirection();
			app.UseRouting();
			app.UseAuthentication();
			app.UseAuthorization();
			app.ConfigureGlobalExceptionHandler(sentryHub);

			// Swagger
			if (this.Configuration.GetSection("Documentation").GetValue<bool>("SwaggerEnabled"))
			{
				app.UseSwagger();

				int majorVersion = this.Configuration.GetSection("ApiVersion").GetValue<int>("Major");
				app.UseSwaggerUI(c =>
				{
					c.SwaggerEndpoint($"/swagger/v{majorVersion}/swagger.json", $"ErtisAuth V{majorVersion}");
				});
			}
			
			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});
		}

		#endregion
	}
}