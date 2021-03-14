
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Sentry;

namespace ErtisAuth.WebAPI
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var hostBuilder = CreateHostBuilder(args);
			var host = hostBuilder.Build();
			host.Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args)
		{
			return Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder =>
			{
				webBuilder.UseStartup<Startup>();

				// Sentry.io integration
				webBuilder.UseSentry(sentry =>
				{
					sentry.AddExceptionFilterForType<Ertis.Core.Exceptions.ValidationException>();
					sentry.AddExceptionFilterForType<Ertis.Core.Exceptions.ErtisException>();
					sentry.AddExceptionFilterForType<Ertis.Core.Exceptions.HttpStatusCodeException>();
				});
			});
		}
	}
}