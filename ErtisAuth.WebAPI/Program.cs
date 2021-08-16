using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sentry;

namespace ErtisAuth.WebAPI
{
	public class Program
	{
		private static IConfigurationRoot BootConfiguration;
		
		private static readonly Dictionary<string, object> EnvironmentParameters = new Dictionary<string, object>();
		
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
					webBuilder.UseSentry(o =>
					{
						o.Dsn = sentryDsn;
						o.Debug = isSentryDebuggingEnabled;
						o.TracesSampleRate = 0.5;
						o.Environment = BootConfiguration["Environment"];
						o.AddExceptionFilterForType<Ertis.Core.Exceptions.ValidationException>();
						o.AddExceptionFilterForType<Ertis.Core.Exceptions.ErtisException>();
						o.AddExceptionFilterForType<Ertis.Core.Exceptions.HttpStatusCodeException>();
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

			SetEnvironmentParameter("Environment", BootConfiguration["Environment"]);
		}
		
		public static void SetEnvironmentParameter(string key, object value)
		{
			if (EnvironmentParameters.ContainsKey(key))
				EnvironmentParameters[key] = value;
			else
				EnvironmentParameters.Add(key, value);
		}
		
		public static object GetEnvironmentParameter(string key)
		{
			if (EnvironmentParameters.ContainsKey(key))
				return EnvironmentParameters[key];

			return null;
		}
	}
}