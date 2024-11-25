using System;
using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Constants;
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
						await this.ScheduleTokenCleanerJobAsync(scheduler, membership, cancellationToken: cancellationToken);
					}
				}
			}
			catch (Exception ex)
			{
				this.logger.Log(LogLevel.Error, ex, "TokenCleanerJob could not scheduled");
			}
		}

		private static string GenerateJobName(string job, Membership membership)
		{
			return $"Quartz:{job}:{membership.Name}";
		}

		private async Task ScheduleAllJobsAsync(IScheduler scheduler, Membership membership, CancellationToken cancellationToken = default)
		{
			await this.ScheduleTokenCleanerJobAsync(scheduler, membership, cancellationToken);
			await this.ScheduleOtpCleanerJobAsync(scheduler, membership, cancellationToken);
		}
		
		private async Task ScheduleTokenCleanerJobAsync(IScheduler scheduler, Membership membership, CancellationToken cancellationToken = default)
		{
			try
			{
				var jobName = GenerateJobName(nameof(TokenCleanerJob), membership);
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
		
		private async Task ScheduleOtpCleanerJobAsync(IScheduler scheduler, Membership membership, CancellationToken cancellationToken = default)
		{
			try
			{
				var jobName = GenerateJobName(nameof(OtpCleanerJob), membership);
				var job = JobBuilder.Create<OtpCleanerJob>()
					.WithIdentity(new JobKey(jobName))
					.UsingJobData("membership_id", membership.Id)
					.Build();

				var trigger = TriggerBuilder.Create()
					.WithIdentity(jobName + "-trigger")
					.WithSimpleSchedule(x => x
						.WithIntervalInSeconds((int)TTLs.RESET_PASSWORD_TOKEN_TTL.TotalSeconds)
						.RepeatForever())
					.StartNow()
					.Build();
	  
				await scheduler.ScheduleJob(job, trigger, cancellationToken: cancellationToken);
			
				// ReSharper disable once PositionalPropertyUsedProblem
				this.logger.Log(LogLevel.Information, "Job scheduled ({0})", jobName);
			}
			catch (Exception ex)
			{
				this.logger.Log(LogLevel.Error, ex, "OtpCleanerJob could not scheduled");
			}
		}

		private async Task RescheduleAllJobsAsync(IScheduler scheduler, Membership membership, CancellationToken cancellationToken = default)
		{
			this.logger.Log(LogLevel.Information, "Job re-scheduling...");
			await this.DeleteAllJobsAsync(scheduler, membership, cancellationToken: cancellationToken);
			await this.ScheduleAllJobsAsync(scheduler, membership, cancellationToken: cancellationToken);
		}
		
		private async Task DeleteAllJobsAsync(IScheduler scheduler, Membership membership, CancellationToken cancellationToken = default)
		{
			await this.DeleteJobAsync(scheduler, nameof(TokenCleanerJob), membership, cancellationToken);
			await this.DeleteJobAsync(scheduler, nameof(OtpCleanerJob), membership, cancellationToken);
		}
		
		private async Task DeleteJobAsync(IScheduler scheduler, string job, Membership membership, CancellationToken cancellationToken = default)
		{
			try
			{
				var jobName = GenerateJobName(job, membership);
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
			await this.ScheduleAllJobsAsync(scheduler, e.Resource);
		}
		
		private void OnMembershipUpdated(object sender, UpdateResourceEventArgs<Membership> e) =>
			this.OnMembershipUpdatedAsync(e).ConfigureAwait(false).GetAwaiter().GetResult();
		
		private async Task OnMembershipUpdatedAsync(UpdateResourceEventArgs<Membership> e)
		{
			var scheduler = await this.schedulerFactory.GetScheduler();
			await this.RescheduleAllJobsAsync(scheduler, e.Updated);
		}
		
		private void OnMembershipDeleted(object sender, DeleteResourceEventArgs<Membership> e) =>
			this.OnMembershipDeletedAsync(e).ConfigureAwait(false).GetAwaiter().GetResult();
		
		private async Task OnMembershipDeletedAsync(DeleteResourceEventArgs<Membership> e)
		{
			var scheduler = await this.schedulerFactory.GetScheduler();
			await this.DeleteAllJobsAsync(scheduler, e.Resource);
		}

		#endregion
	}
}