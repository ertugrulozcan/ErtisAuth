using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Ertis.Data.Repository;
using Ertis.Extensions.AspNetCore.Versioning;
using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Database;
using Ertis.Net.Rest;
using Ertis.Schema.Serialization;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Dao.Repositories;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Extensions.ApplicationInsights;
using ErtisAuth.Extensions.Authorization.Constants;
using ErtisAuth.Extensions.Database;
using ErtisAuth.Extensions.Hosting;
using ErtisAuth.Extensions.Mailkit.Extensions;
using ErtisAuth.Extensions.Mailkit.Serialization;
using ErtisAuth.Extensions.Prometheus.Extensions;
using ErtisAuth.Extensions.Quartz.Extensions;
using ErtisAuth.Identity.Jwt.Services;
using ErtisAuth.Identity.Jwt.Services.Interfaces;
using ErtisAuth.Infrastructure.Adapters;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.Integrations.OAuth.Extensions;
using ErtisAuth.WebAPI.Adapters;
using ErtisAuth.WebAPI.Auth;
using ErtisAuth.WebAPI.Extensions;
using ErtisAuth.WebAPI.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using MongoDB.Driver;
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

var version = builder.Configuration.GetValue<string>("Version");
if (!string.IsNullOrEmpty(version))
{
	EnvironmentParams.SetEnvironmentParameter("Version", version);
}

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
if (!string.IsNullOrEmpty(environment))
{
	EnvironmentParams.SetEnvironmentParameter("Environment", environment);
}

builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("Database"));
builder.Services.AddSingleton<IDatabaseSettings>(serviceProvider => serviceProvider.GetRequiredService<IOptions<DatabaseSettings>>().Value);

builder.Services.AddSingleton<IMongoClientProvider>(serviceProvider =>
{
	var databaseSettings = serviceProvider.GetRequiredService<IDatabaseSettings>();
	return new MongoClientProvider(MongoClientSettings.FromConnectionString(databaseSettings.ConnectionString));
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
builder.Services.AddSingleton<ITokenCodeRepository, TokenCodeRepository>();
builder.Services.AddSingleton<ICodePolicyRepository, CodePolicyRepository>();
builder.Services.AddSingleton<IOneTimePasswordRepository, OneTimePasswordRepository>();
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
builder.Services.AddSingleton<ITokenCodeService, TokenCodeService>();
builder.Services.AddSingleton<ITokenCodePolicyService, TokenCodePolicyService>();
builder.Services.AddSingleton<IOneTimePasswordService, OneTimePasswordService>();
builder.Services.AddSingleton<IProviderService, ProviderService>();
builder.Services.AddSingleton<IWebhookService, WebhookService>();
builder.Services.AddSingleton<IMailHookService, MailHookService>();
builder.Services.AddSingleton<IEventService, EventService>();
builder.Services.AddSingleton<IMigrationService, MigrationService>();

builder.Services.AddSingleton<ISystemRestHandler, SystemRestHandler>();
builder.Services.AddSingleton<IScopeOwnerAccessor, ScopeOwnerAccessor>();
builder.Services.AddSingleton<IAuthorizationHandler, ErtisAuthAuthorizationHandler>();
builder.Services.AddProviders();

builder.Services.AddMemoryCache();

// Background Workers
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<IBackgroundTaskQueue>(_ => new BackgroundTaskQueue(100));
builder.Services.AddSingleton<IMailServiceBackgroundWorker, MailServiceBackgroundWorker>();

// Mailkit
builder.Services.AddMailkit();

// Prometheus
builder.Services.AddPrometheus();

builder.Services.AddHttpClient();
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

// Sentry
var sentryDsn = builder.Configuration.GetSection("Sentry").GetValue<string>("Dsn");
if (!string.IsNullOrEmpty(sentryDsn))
{
	SentrySdk.Init(options =>
	{
		options.Dsn = sentryDsn;
		options.Debug = false;
		options.AutoSessionTracking = true;
		options.IsGlobalModeEnabled = false;
		options.TracesSampleRate = 0.1;
	});
}

// ApplicationInsights
builder.Services.AddApplicationInsights(builder.Configuration);

// Logging
builder.Logging.AddJsonConsole(options =>
{
	options.IncludeScopes = false;
	options.TimestampFormat = "HH:mm:ss";
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
	options.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "ErtisAuth.WebAPI",
		Description = "ErtisAuth API Documentation",
		Version = $"v{major}"
	});
	
	options.CustomSchemaIds(x => x.FullName);
});

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
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

// Database
app.CheckDatabaseIndexes();

app.UseMailkit();
app.UseProviders();
app.UseCors(CORS_POLICY_KEY);
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.ConfigureGlobalExceptionHandler();

// Prometheus
app.UsePrometheus();

app.MapControllers();
ResolveRequiredServices(app.Services);

app.Run();