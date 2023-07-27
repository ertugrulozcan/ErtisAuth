using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Extensions.Quartz.Jobs;
using ErtisAuth.Extensions.Quartz.Services;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.AspNetCore;

namespace ErtisAuth.Extensions.Quartz.Extensions
{
	public static class ErtisAuthQuartzExtensions
	{
		#region Methods

		public static void AddQuartzJobs(this IServiceCollection services)
		{
			// Quartz
			services.AddQuartz(quartz =>
			{
				quartz.UseMicrosoftDependencyInjectionJobFactory();
			});

			// Quartz on ASP.NET Core hosting
			services.AddQuartzServer(options =>
			{
				// when shutting down we want jobs to complete gracefully
				options.WaitForJobsToComplete = true;
			});
			
			// Quartz Jobs Inject to DI container
			services.AddSingleton<IScheduledJobService, ScheduledJobService>();
			services.AddTransient<TokenCleanerJob>();
		}

		#endregion
	}
}