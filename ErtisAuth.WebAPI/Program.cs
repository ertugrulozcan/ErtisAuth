using Microsoft.AspNetCore.Authentication;
using Ertis.Schema.Serialization;
using ErtisAuth.Extensions.ApplicationInsights;
using ErtisAuth.Extensions.Authorization.Constants;
using ErtisAuth.Extensions.Database;
using ErtisAuth.Extensions.Mailkit.Extensions;
using ErtisAuth.Extensions.Mailkit.Serialization;
using ErtisAuth.Extensions.Prometheus.Extensions;
using ErtisAuth.Integrations.OAuth.Extensions;
using ErtisAuth.WebAPI.Auth;
using ErtisAuth.WebAPI.Extensions;
using Microsoft.AspNetCore.ResponseCompression;
using Scalar.AspNetCore;

const string CORS_POLICY_KEY = "cors-policy";

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddMongoDB(builder.Configuration);

// Services
builder.Services.AddServices();

// Providers
builder.Services.AddProviders();

// MemoryCache
builder.Services.AddMemoryCache();

// Mailkit
builder.Services.AddMailkit();

// Prometheus
builder.Services.AddPrometheus();

// HttpClient
builder.Services.AddHttpClient();

// CORS
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

// Authentication
builder.Services
	.AddAuthentication()
	.AddScheme<AuthenticationSchemeOptions, ErtisAuthAuthenticationHandler>(Policies.ErtisAuthAuthorizationPolicyName, _ => {});

// Authorization
builder.Services.AddAuthorization(options =>
	options.AddPolicy(Policies.ErtisAuthAuthorizationPolicyName, policy =>
	{
		policy.AddAuthenticationSchemes(Policies.ErtisAuthAuthorizationPolicyName);
		policy.AddRequirements(new ErtisAuthAuthorizationRequirement());
	}));

// ApplicationInsights
builder.Services.AddApplicationInsights(builder.Configuration);

// Compression
builder.Services.AddResponseCompression(options =>
{
	options.EnableForHttps = true;
	options.Providers.Add<BrotliCompressionProvider>();
	options.Providers.Add<GzipCompressionProvider>();
});

// OpenAPI
builder.Services.AddOpenApi();

// Logging
builder.Logging.AddJsonConsole(options =>
{
	options.IncludeScopes = false;
	options.TimestampFormat = "HH:mm:ss";
});

builder.Services
	.AddControllers()
	.AddNewtonsoftJson(options =>
	{
		options.SerializerSettings.Converters.Add(new DynamicObjectJsonConverter());
		options.SerializerSettings.Converters.Add(new MailProviderJsonConverter());
		options.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
	});

// Graceful shutdown
builder.ConfigureShutdown();

var app = builder.Build();

// OpenAPI
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.MapScalarApiReference("/documentation", options =>
	{
		options.WithTitle("ErtisAuth");
	});
}

// Compression
app.UseResponseCompression();

// Database
app.UseMongoDB();

app.UseMailkit();
app.UseProviders();
app.UseCors(CORS_POLICY_KEY);
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseGlobalExceptionHandler();

// Prometheus
app.UsePrometheus();

app.MapControllers();
app.UseServices();

app.Run();