using ErtisAuth.Abstractions.Services;

namespace ErtisAuth.WebAPI.BackgroundServices;

/// <summary>
/// Sends the queued hook mails in the background with a bounded concurrency (the mail providers rate limit the connections).
/// On shutdown the queue is closed and drained within the host's shutdown timeout; the mails still being sent when the timeout is over are cancelled.
/// </summary>
public class MailHookBackgroundService : BackgroundService
{
	#region Constants
	
	private const int MaxConcurrentMails = 4;
	
	#endregion
	
	#region Services
	
	private readonly IMailHookQueue _mailHookQueue;
	private readonly IMailHookService _mailHookService;
	private readonly ILogger<MailHookBackgroundService> _logger;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="mailHookQueue"></param>
	/// <param name="mailHookService"></param>
	/// <param name="logger"></param>
	public MailHookBackgroundService(
		IMailHookQueue mailHookQueue,
		IMailHookService mailHookService,
		ILogger<MailHookBackgroundService> logger)
	{
		this._mailHookQueue = mailHookQueue;
		this._mailHookService = mailHookService;
		this._logger = logger;
	}
	
	#endregion
	
	#region Methods
	
	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		// The queue is read until it is completed and drained (not until stopping), so the queued mails are still sent on shutdown;
		// stoppingToken cancels only the sending, when the shutdown timeout is over
		return Parallel.ForEachAsync(
			this._mailHookQueue.ReadAllAsync(CancellationToken.None),
			new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrentMails },
			async (mail, _) =>
			{
				try
				{
					await this._mailHookService.SendHookMailAsync(mail, stoppingToken);
				}
				catch (Exception ex)
				{
					// A failed mail must not stop the worker
					this._logger.LogError(ex, "The hook mail '{MailHook}' could not be sent", mail.MailHook.Name);
				}
			});
	}
	
	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		this._mailHookQueue.Complete();
		
		try
		{
			if (this.ExecuteTask != null)
			{
				await this.ExecuteTask.WaitAsync(cancellationToken);
			}
		}
		catch (OperationCanceledException)
		{
			this._logger.LogWarning("The mail queue could not be drained within the shutdown timeout, {Count} hook mails are not sent", this._mailHookQueue.Count);
		}
		
		await base.StopAsync(cancellationToken);
	}
	
	#endregion
}
