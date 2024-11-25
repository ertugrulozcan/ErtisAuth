using System;
using System.Threading.Tasks;
using ErtisAuth.Abstractions.Services;
using Microsoft.Extensions.Logging;
using Quartz;

namespace ErtisAuth.Extensions.Quartz.Jobs;

public class OtpCleanerJob : IJob, IDisposable
{
    #region Services
    
    private readonly IMembershipService membershipService;
    private readonly IOneTimePasswordService oneTimePasswordService;
    private readonly ILogger<OtpCleanerJob> logger;

    #endregion
    		
    #region Constructors

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="membershipService"></param>
    /// <param name="oneTimePasswordService"></param>
    /// <param name="logger"></param>
    public OtpCleanerJob(
    	IMembershipService membershipService, 
	    IOneTimePasswordService oneTimePasswordService,
    	ILogger<OtpCleanerJob> logger)
    {
    	this.membershipService = membershipService;
    	this.oneTimePasswordService = oneTimePasswordService;
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
    			await this.oneTimePasswordService.ClearExpiredPasswordsAsync(membershipId, context.CancellationToken);
    		}
    		else
    		{
    			await Console.Out.WriteLineAsync($"Membership is null! ({membershipId})");	
    		}
    	}
    	catch (Exception ex)
    	{
    		this.logger.Log(LogLevel.Error, ex, "OtpCleanerJob could not executed");
    	}
    }

    #endregion

    #region Disposing

    public void Dispose()
    {
    	this.logger.LogInformation("OtpCleanerJob instance disposed");
    }

    #endregion
}