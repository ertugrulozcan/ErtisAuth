namespace ErtisAuth.Extensions.ApplicationInsights.Configuration;

public interface IApplicationInsightsOptions
{
    string? ConnectionString { get; set; }
}

public class ApplicationInsightsOptions : IApplicationInsightsOptions
{
    public string? ConnectionString { get; set; }
}