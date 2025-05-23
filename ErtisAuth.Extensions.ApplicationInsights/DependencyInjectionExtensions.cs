using Azure.Monitor.OpenTelemetry.AspNetCore;
using ErtisAuth.Extensions.ApplicationInsights.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ErtisAuth.Extensions.ApplicationInsights;

public static class DependencyInjectionExtensions
{
    #region Methods
    
    public static void AddApplicationInsights(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ApplicationInsightsOptions>(configuration.GetSection("ApplicationInsights"));
        services.AddSingleton<IApplicationInsightsOptions>(serviceProvider => serviceProvider.GetRequiredService<IOptions<ApplicationInsightsOptions>>().Value);
        
        var options = configuration.GetSection("ApplicationInsights").Get<ApplicationInsightsOptions>();
        if (options != null && !string.IsNullOrEmpty(options.ConnectionString))
        {
            services.AddOpenTelemetry().UseAzureMonitor(x => x.ConnectionString = options.ConnectionString);
        }
    }
    
    #endregion
}