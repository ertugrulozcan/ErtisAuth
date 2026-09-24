using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace ErtisAuth.Extensions.AspNetCore.Extensions;

public static class ApplicationHostExtensions
{
	#region Methods
	
	public static void ConfigureShutdown(this WebApplicationBuilder builder)
	{
		builder.Host.ConfigureHostOptions(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(30));
	}
	
	#endregion
}