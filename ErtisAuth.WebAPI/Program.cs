using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Ertis.Data.Repository;
using Ertis.Extensions.AspNetCore.Versioning;
using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Database;
using Ertis.Net.Rest;
using Ertis.Schema.Serialization;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Dao.Repositories;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Extensions.Authorization.Constants;
using ErtisAuth.Extensions.Database;
using ErtisAuth.Extensions.Mailkit.Extensions;
using ErtisAuth.Extensions.Mailkit.Serialization;
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
using ErtisAuth.WebAPI.Helpers;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using IMongoDatabase = Ertis.MongoDB.Database.IMongoDatabase;

const string CORS_POLICY_KEY = "cors-policy";

void ResolveRequiredServices(IServiceProvider serviceProvider)
{
	serviceProvider.GetRequiredService<IUserTypeService>();
	serviceProvider.GetRequiredService<IUserService>();
	serviceProvider.GetRequiredService<IApplicationService>();
	serviceProvider.GetRequiredService<IRoleService>();
	serviceProvider.GetRequiredService<IProviderService>();
	serviceProvider.GetRequiredService<IWebhookService>();
	serviceProvider.GetRequiredService<IMailHookService>();
}

var builder = WebApplication.CreateBuilder(args);

EnvironmentParams.SetEnvironmentParameter("Version", builder.Configuration.GetValue<string>("Version"));
EnvironmentParams.SetEnvironmentParameter("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));

builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("Database"));
builder.Services.AddSingleton<IDatabaseSettings>(serviceProvider => serviceProvider.GetRequiredService<IOptions<DatabaseSettings>>().Value);

// builder.Services.AddSingleton<IEventSubscriber, MongoEventSubscriber>();
builder.Services.AddSingleton<IMongoClientProvider>(serviceProvider =>
{
	var databaseSettings = serviceProvider.GetRequiredService<IDatabaseSettings>();
	var eventSubscriber = serviceProvider.GetService<IEventSubscriber>();
	return new MongoClientProvider(MongoClientSettings.FromConnectionString(databaseSettings.ConnectionString), eventSubscriber);
});

builder.Services.Configure<ApiVersionOptions>(builder.Configuration.GetSection("ApiVersion"));
builder.Services.AddSingleton<IApiVersionOptions>(serviceProvider => serviceProvider.GetRequiredService<IOptions<ApiVersionOptions>>().Value);

builder.Services.AddSingleton<IMongoDatabase, MongoDatabase>();
builder.Services.AddSingleton<IMembershipRepository, MembershipRepository>();
builder.Services.AddSingleton<IUserTypeRepository, UserTypeRepository>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IApplicationRepository, ApplicationRepository>();
builder.Services.AddSingleton<IRoleRepository, RoleRepository>();
builder.Services.AddSingleton<IWebhookRepository, WebhookRepository>();
builder.Services.AddSingleton<IMailHookRepository, MailHookRepository>();
builder.Services.AddSingleton<IProviderRepository, ProviderRepository>();
builder.Services.AddSingleton<IActiveTokensRepository, ActiveTokensRepository>();
builder.Services.AddSingleton<IRevokedTokensRepository, RevokedTokensRepository>();
builder.Services.AddSingleton<IEventRepository, EventRepository>();
builder.Services.AddSingleton<IRepositoryActionBinder, SysUpserter>();

builder.Services.AddSingleton<ICryptographyService, CryptographyService>();
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<IActiveTokenService, ActiveTokenService>();
builder.Services.AddSingleton<IRevokedTokenService, RevokedTokenService>();
builder.Services.AddSingleton<IAccessControlService, AccessControlService>();
builder.Services.AddSingleton<IMembershipService, MembershipService>();
builder.Services.AddSingleton<IUserTypeService, UserTypeService>();
builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddSingleton<IApplicationService, ApplicationService>();
builder.Services.AddSingleton<IRoleService, RoleService>();
builder.Services.AddSingleton<IProviderService, ProviderService>();
builder.Services.AddSingleton<IWebhookService, WebhookService>();
builder.Services.AddSingleton<IMailHookService, MailHookService>();
builder.Services.AddSingleton<IEventService, EventService>();
builder.Services.AddSingleton<IMigrationService, MigrationService>();

builder.Services.AddSingleton<IRestHandler, RestHandler>();
builder.Services.AddSingleton<IScopeOwnerAccessor, ScopeOwnerAccessor>();
builder.Services.AddSingleton<IAuthorizationHandler, ErtisAuthAuthorizationHandler>();
builder.Services.AddProviders();

builder.Services.AddMemoryCache();

// Mailkit
builder.Services.AddMailkit();

// GeoLocation Tracking
builder.Services.Configure<GeoLocationOptions>(builder.Configuration.GetSection("GeoLocationTracking"));
builder.Services.AddSingleton<IGeoLocationOptions>(serviceProvider => serviceProvider.GetRequiredService<IOptions<GeoLocationOptions>>().Value);
if (builder.Configuration.GetSection("GeoLocationTracking").GetValue<bool>("Enabled"))
{
	var provider = builder.Configuration.GetSection("GeoLocationTracking").GetValue<string>("Provider");
	if (!string.IsNullOrEmpty(provider))
	{
		switch (provider)
		{
			case "MaxMind":
				builder.Services.Configure<MaxMindOptions>(builder.Configuration.GetSection("MaxMind"));
				builder.Services.AddSingleton<IMaxMindOptions>(serviceProvider => serviceProvider.GetRequiredService<IOptions<MaxMindOptions>>().Value);
				builder.Services.AddSingleton<IGeoLocationService, MaxMindGeoLocationService>();
				break;
			case "Ip2Location":
				builder.Services.Configure<Ip2LocationOptions>(builder.Configuration.GetSection("Ip2Location"));
				builder.Services.AddSingleton<IIp2LocationOptions>(serviceProvider => serviceProvider.GetRequiredService<IOptions<Ip2LocationOptions>>().Value);
				builder.Services.AddSingleton<IGeoLocationService, Ip2LocationService>();
				break;
			default:
				Console.WriteLine("Unknown geo location provider: " + provider);
				builder.Services.AddSingleton<IGeoLocationService, GeoLocationDisabledService>();
				break;
		}
	}
	else
	{
		Console.WriteLine("Geo location provider is undefined");
		builder.Services.AddSingleton<IGeoLocationService, GeoLocationDisabledService>();
	}
}
else
{
	builder.Services.AddSingleton<IGeoLocationService, GeoLocationDisabledService>();
}

builder.Services.AddHttpContextAccessor();

builder.Services.AddCors(options =>
{
	options.AddPolicy(CORS_POLICY_KEY,
		policy =>
		{
			policy
				.AllowAnyOrigin()
				.AllowAnyMethod()
				.AllowAnyHeader();
		});
});

builder.Services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, ErtisAuthAuthenticationHandler>(
	Policies.ErtisAuthAuthorizationPolicyName, _ => {});

builder.Services.AddAuthorization(options =>
	options.AddPolicy(Policies.ErtisAuthAuthorizationPolicyName, policy =>
	{
		policy.AddAuthenticationSchemes(Policies.ErtisAuthAuthorizationPolicyName);
		policy.AddRequirements(new ErtisAuthAuthorizationRequirement());
	}));

// Api versioning
var major = builder.Configuration.GetSection("ApiVersion").GetValue<int>("Major");
var minor = builder.Configuration.GetSection("ApiVersion").GetValue<int>("Minor");

builder.Services.AddApiVersioning(o => {
	o.ReportApiVersions = true;
	o.AssumeDefaultVersionWhenUnspecified = true;
	o.DefaultApiVersion = new ApiVersion(major, minor);
});

// Quartz
builder.Services.AddQuartzJobs();

// Swagger
if (builder.Configuration.GetSection("Documentation").GetValue<bool>("SwaggerEnabled"))
{
	var swaggerVersion = $"v{major}";
	builder.Services.AddSwaggerGen(c => { c.SwaggerDoc(swaggerVersion, new OpenApiInfo { Title = "ErtisAuth.WebAPI", Version = swaggerVersion }); });	
}

builder.Services
	.AddControllers()
	.AddNewtonsoftJson(options =>
	{
		options.SerializerSettings.Converters.Add(new DynamicObjectJsonConverter());
		options.SerializerSettings.Converters.Add(new MailProviderJsonConverter());
		options.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
	});

builder.Services.AddRazorPages();

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment() && builder.Configuration.GetSection("Documentation").GetValue<bool>("SwaggerEnabled"))
{
	app.UseSwagger();
	app.UseSwaggerUI(options =>
	{
		var swaggerVersion = $"V{major}";
		options.SwaggerEndpoint("/swagger/v1/swagger.json", "ErtisAuth.WebAPI " + swaggerVersion);
		options.DefaultModelsExpandDepth(-1);
	});	
}

// Database
app.CheckDatabaseIndexes();

app.UseProviders();
app.UseCors(CORS_POLICY_KEY);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.ConfigureGlobalExceptionHandler();
app.MapControllers();

ResolveRequiredServices(app.Services);

app.Run();