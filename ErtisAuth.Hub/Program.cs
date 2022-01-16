using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ErtisAuth.Hub
{
	// ReSharper disable once ClassNeverInstantiated.Global
	public class Program
	{
		private static readonly Dictionary<string, object> EnvironmentParameters = new();
		
		public static void Main(string[] args)
		{
			SetEnvironmentParameter("BuildTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
			CreateHostBuilder(args).Build().Run();
		}

		// ReSharper disable once MemberCanBePrivate.Global
		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder.ConfigureAppConfiguration((builderContext, config) =>
					{
						var env = builderContext.HostingEnvironment;

						config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
							.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
							.AddCommandLine(args);
					});
					
					// Set max header size (default 32768)
					webBuilder.UseKestrel(options => { options.Limits.MaxRequestHeadersTotalSize = 32768 * 2; });
					webBuilder.UseStartup<Startup>();
				});
		
		// ReSharper disable once MemberCanBePrivate.Global
		public static void SetEnvironmentParameter(string key, object value)
		{
			if (EnvironmentParameters.ContainsKey(key))
				EnvironmentParameters[key] = value;
			else
				EnvironmentParameters.Add(key, value);
		}
		
		public static object GetEnvironmentParameter(string key)
		{
			return EnvironmentParameters.ContainsKey(key) ? EnvironmentParameters[key] : null;
		}
	}
}