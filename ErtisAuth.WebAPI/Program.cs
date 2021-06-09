using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sentry;

namespace ErtisAuth.WebAPI
{
	public class Program
	{
		private static IConfigurationRoot BootConfiguration;
		
		public static void Main(string[] args)
		{
			var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
			SetBootConfiguration(environment);
			
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
				if (bool.TryParse(BootConfiguration["Sentry:Enabled"], out bool isSentryEnabled) && isSentryEnabled)
				{
					var sentryDsn = BootConfiguration["Sentry:Dsn"];
					bool.TryParse(BootConfiguration["Sentry:Debug"], out bool isSentryDebuggingEnabled);
					webBuilder.UseSentry(sentry =>
					{
						sentry.Dsn = sentryDsn;
						sentry.Debug = isSentryDebuggingEnabled;
						sentry.TracesSampleRate = 1.0;
						sentry.AddExceptionFilterForType<Ertis.Core.Exceptions.ValidationException>();
						sentry.AddExceptionFilterForType<Ertis.Core.Exceptions.ErtisException>();
						sentry.AddExceptionFilterForType<Ertis.Core.Exceptions.HttpStatusCodeException>();
					});	
				}
			});
		}
		
		private static void SetBootConfiguration(string environment)
		{
			BootConfiguration = new ConfigurationBuilder()
				.AddJsonFile("appsettings.json", false, true)
				.AddJsonFile(
					$"appsettings.{environment}.json",
					optional: true)
				.Build();
		}
	}
}