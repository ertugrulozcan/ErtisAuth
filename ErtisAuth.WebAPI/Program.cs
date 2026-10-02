using ErtisAuth.Extensions.ApplicationInsights;
using ErtisAuth.Extensions.Database;
using ErtisAuth.Extensions.Prometheus;
using ErtisAuth.Integrations.OAuth.Extensions;
using ErtisAuth.Extensions.AspNetCore.Extensions;
using ErtisAuth.Extensions.Mailing.Extensions;
using ErtisAuth.WebAPI.Extensions;
using Microsoft.AspNetCore.ResponseCompression;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddMongoDB(builder.Configuration);

// Services
builder.Services.AddServices();

// Providers
builder.Services.AddProviders();

// Mail Providers
builder.Services.AddMailProviders();

// MemoryCache
builder.Services.AddMemoryCache();

// Prometheus
builder.Services.AddPrometheus();

// HttpClient
builder.Services.AddHttpClient();

// CORS
builder.Services.AddCORS();

// Authentication & Authorization
builder.Services.AddErtisAuth();

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

// Controllers & JSON Options
builder.Services
	.AddControllers(options => options.AddQueryActionFilter())
	.AddJsonSerialization();

// Graceful shutdown
builder.ConfigureShutdown();

var app = builder.Build();

// OpenAPI
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.MapScalarApiReference("documentation", options =>
	{
		options.WithTitle("ErtisAuth");
	});
}

// Compression
app.UseResponseCompression();

// Database
await app.UseMongoDB();

app.UseCORS();
app.UseHttpsRedirection();
app.UseRouting();
app.UseGlobalExceptionHandler();
app.UseWwwAuthenticateChallenge();
app.UseAuthentication();
app.UseAuthorization();

// Prometheus
app.UsePrometheus();

app.MapControllers();
app.UseServices();

app.Run();

// Exposes the entry point to WebApplicationFactory (for ErtisAuth.IntegrationTests)
#pragma warning disable ASP0027
public partial class Program;
#pragma warning restore ASP0027
