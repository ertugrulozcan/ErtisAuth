namespace ErtisAuth.WebAPI.Extensions;

public static class ApplicationHostExtensions
{
	#region Methods
	
	public static void ConfigureShutdown(this WebApplicationBuilder builder)
	{
		builder.Host.ConfigureHostOptions(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(30));
	}
	
	#endregion
}