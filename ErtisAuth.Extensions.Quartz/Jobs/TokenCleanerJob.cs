using System;
using System.Threading.Tasks;
using ErtisAuth.Abstractions.Services;
using Microsoft.Extensions.Logging;
using Quartz;

namespace ErtisAuth.Extensions.Quartz.Jobs
{
	public class TokenCleanerJob : IJob, IDisposable
	{
		#region Services
		
		private readonly IMembershipService membershipService;
		private readonly ITokenService tokenService;
		private readonly ITokenCodeService tokenCodeService;
		private readonly ILogger<TokenCleanerJob> logger;

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="membershipService"></param>
		/// <param name="tokenService"></param>
		/// <param name="tokenCodeService"></param>
		/// <param name="logger"></param>
		public TokenCleanerJob(
			IMembershipService membershipService, 
			ITokenService tokenService,
			ITokenCodeService tokenCodeService,
			ILogger<TokenCleanerJob> logger)
		{
			this.membershipService = membershipService;
			this.tokenService = tokenService;
			this.tokenCodeService = tokenCodeService;
			this.logger = logger;
		}
		
		#endregion
		
		#region Methods

		public async Task Execute(IJobExecutionContext context)
		{
			try
			{
				var dataMap = context.JobDetail.JobDataMap;
				var membershipId = dataMap.GetString("membership_id");
				var membership = await this.membershipService.GetAsync(membershipId);
				if (membership != null)
				{
					await this.tokenService.ClearExpiredActiveTokens(membershipId);
					await this.tokenService.ClearRevokedTokens(membershipId);
					await this.tokenCodeService.ClearExpiredTokenCodes(membershipId);
				}
				else
				{
					await Console.Out.WriteLineAsync($"Membership is null! ({membershipId})");	
				}
			}
			catch (Exception ex)
			{
				this.logger.Log(LogLevel.Error, ex, "TokenCleanerJob could not executed");
			}
		}

		#endregion

		#region Disposing

		public void Dispose()
		{
			this.logger.LogInformation("TokenCleanerJob instance disposed");
		}

		#endregion
	}
}