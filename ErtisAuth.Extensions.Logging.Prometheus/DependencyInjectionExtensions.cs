using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;

namespace ErtisAuth.Extensions.Logging.Prometheus;

public static class DependencyInjectionExtensions
{
    #region Methods
    
    public static void AddPrometheus(this IServiceCollection services)
    {
        services.UseHttpClientMetrics();
    }
    
    public static void UsePrometheus(this IApplicationBuilder app)
    {
        app.UseHttpMetrics();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapMetrics();
        });
    }
    
    #endregion
}