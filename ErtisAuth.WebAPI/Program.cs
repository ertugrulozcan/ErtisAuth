using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ErtisAuth.WebAPI
{
	public class Program
	{
		private static IConfigurationRoot BootConfiguration;
		
		private static readonly Dictionary<string, object> EnvironmentParameters = new();

		public static void Main(string[] args)
		{
			var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
			SetBootConfiguration(environment);
			
			CreateHostBuilder(args).Build().Run();
		}
		
		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
		
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