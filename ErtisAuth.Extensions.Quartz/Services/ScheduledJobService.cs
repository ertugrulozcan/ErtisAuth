using System;
using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Events.EventArgs;
using ErtisAuth.Extensions.Quartz.Jobs;
using Microsoft.Extensions.Logging;
using Quartz;

namespace ErtisAuth.Extensions.Quartz.Services
{
	public class ScheduledJobService : IScheduledJobService
	{
		#region Services

		private readonly ISchedulerFactory schedulerFactory;
		private readonly IMembershipService membershipService;
		private readonly ILogger<ScheduledJobService> logger;
		
		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="schedulerFactory"></param>
		/// <param name="membershipService"></param>
		/// <param name="logger"></param>
		public ScheduledJobService(
			ISchedulerFactory schedulerFactory, 
			IMembershipService membershipService,
			ILogger<ScheduledJobService> logger)
		{
			this.schedulerFactory = schedulerFactory;
			this.membershipService = membershipService;
			this.logger = logger;
			
			this.membershipService.OnCreated += this.OnMembershipCreated;
			this.membershipService.OnUpdated += this.OnMembershipUpdated;
			this.membershipService.OnDeleted += this.OnMembershipDeleted;
		}

		#endregion

		#region Methods

		public async ValueTask ScheduleTokenCleanerJobsAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				var scheduler = await this.schedulerFactory.GetScheduler(cancellationToken: cancellationToken);
				await scheduler.Start(cancellationToken: cancellationToken);
			
				var memberships = await this.membershipService.GetAsync(cancellationToken: cancellationToken);
				if (memberships?.Items != null)
				{
					foreach (var membership in memberships.Items)
					{
						await this.ScheduleJobAsync(scheduler, membership, cancellationToken: cancellationToken);
					}
				}
			}
			catch (Exception ex)
			{
				this.logger.Log(LogLevel.Error, ex, "TokenCleanerJob could not scheduled");
			}
		}

		private static string GenerateJobName(Membership membership)
		{
			return $"Quartz:{membership.Name}";
		}

		private async Task ScheduleJobAsync(IScheduler scheduler, Membership membership, CancellationToken cancellationToken = default)
		{
			try
			{
				var jobName = GenerateJobName(membership);
				var job = JobBuilder.Create<TokenCleanerJob>()
					.WithIdentity(new JobKey(jobName))
					.UsingJobData("membership_id", membership.Id)
					.Build();

				var trigger = TriggerBuilder.Create()
					.WithIdentity(jobName + "-trigger")
					.WithSimpleSchedule(x => x
						.WithIntervalInSeconds(membership.ExpiresIn)
						.RepeatForever())
					.StartNow()
					.Build();
	  
				await scheduler.ScheduleJob(job, trigger, cancellationToken: cancellationToken);
			
				// ReSharper disable once PositionalPropertyUsedProblem
				this.logger.Log(LogLevel.Information, "Job scheduled ({0})", jobName);
			}
			catch (Exception ex)
			{
				this.logger.Log(LogLevel.Error, ex, "TokenCleanerJob could not scheduled");
			}
		}

		private async Task RescheduleJobAsync(IScheduler scheduler, Membership membership, CancellationToken cancellationToken = default)
		{
			this.logger.Log(LogLevel.Information, "Job re-scheduling...");
			await this.DeleteJobAsync(scheduler, membership, cancellationToken: cancellationToken);
			await this.ScheduleJobAsync(scheduler, membership, cancellationToken: cancellationToken);
		}

		private async Task DeleteJobAsync(IScheduler scheduler, Membership membership, CancellationToken cancellationToken = default)
		{
			try
			{
				var jobName = GenerateJobName(membership);
				await scheduler.DeleteJob(new JobKey(jobName), cancellationToken: cancellationToken);
			
				// ReSharper disable once PositionalPropertyUsedProblem
				this.logger.Log(LogLevel.Information, "Job deleted ({0})", jobName);
			}
			catch (Exception ex)
			{
				this.logger.Log(LogLevel.Error, ex, "TokenCleanerJob could not deleted");
			}
		}

		#endregion

		#region Event Handlers

		private void OnMembershipCreated(object sender, CreateResourceEventArgs<Membership> e) =>
			this.OnMembershipCreatedAsync(e).ConfigureAwait(false).GetAwaiter().GetResult();
		
		private async Task OnMembershipCreatedAsync(CreateResourceEventArgs<Membership> e)
		{
			var scheduler = await this.schedulerFactory.GetScheduler();
			await this.ScheduleJobAsync(scheduler, e.Resource);
		}
		
		private void OnMembershipUpdated(object sender, UpdateResourceEventArgs<Membership> e) =>
			this.OnMembershipUpdatedAsync(e).ConfigureAwait(false).GetAwaiter().GetResult();
		
		private async Task OnMembershipUpdatedAsync(UpdateResourceEventArgs<Membership> e)
		{
			var scheduler = await this.schedulerFactory.GetScheduler();
			await this.RescheduleJobAsync(scheduler, e.Updated);
		}
		
		private void OnMembershipDeleted(object sender, DeleteResourceEventArgs<Membership> e) =>
			this.OnMembershipDeletedAsync(e).ConfigureAwait(false).GetAwaiter().GetResult();
		
		private async Task OnMembershipDeletedAsync(DeleteResourceEventArgs<Membership> e)
		{
			var scheduler = await this.schedulerFactory.GetScheduler();
			await this.DeleteJobAsync(scheduler, e.Resource);
		}

		#endregion
	}
}