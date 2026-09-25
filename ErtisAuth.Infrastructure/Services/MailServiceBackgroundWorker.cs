using Ertis.Schema.Dynamics;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dao.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Infrastructure.Services;

public class MailServiceBackgroundWorker : IMailServiceBackgroundWorker
{
    #region Services
	
    private readonly IEventService _eventService;
    private readonly IUserRepository _userRepository;
	private readonly ILogger<MailServiceBackgroundWorker> _logger;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="eventService"></param>
	/// <param name="userRepository"></param>
	/// <param name="logger"></param>
	public MailServiceBackgroundWorker(
		IEventService eventService,
		IUserRepository userRepository,
		ILogger<MailServiceBackgroundWorker> logger)
	{
		this._eventService = eventService;
		this._userRepository = userRepository;
		this._logger = logger;
	}
	
	#endregion
	
	#region Methods
	
	private async ValueTask ExecuteAsync(MailServiceBackgroundWorkerArgs? args = null, CancellationToken cancellationToken = default)
	{
		if (args?.Mailhook == null || args.MailProvider == null)
		{
			return;
		}
		
		var recipients = new List<Recipient>();
		if (args.Mailhook.SendToUtilizer && args.UserId != null)
		{
			var model = await this._userRepository.FindOneAsync(args.UserId, cancellationToken: cancellationToken);
			var dynamicObject = model == null ? null : new DynamicObject(model);
			var user = dynamicObject?.Deserialize<User>();
			if (user != null)
			{
				if (string.IsNullOrEmpty(user.EmailAddress))
				{
					throw ErtisAuthException.InvalidUtilizer("The utilizer does not have an email address");
				}
				
				recipients.Add(new Recipient
				{
					DisplayName = $"{user.FirstName} {user.LastName}",
					EmailAddress = user.EmailAddress
				});	
			}
		}
		
		var formatter = new Ertis.TemplateEngine.Formatter();
		if (args.Mailhook.Recipients != null)
		{
			recipients.AddRange(args.Mailhook.Recipients.Select(x => new Recipient
			{
				DisplayName = formatter.Format(x.DisplayName, args.Payload),
				EmailAddress = formatter.Format(x.EmailAddress, args.Payload)
			}));
		}
		
		recipients = recipients.DistinctBy(x => x.EmailAddress).ToList();
		if (recipients.Any())
		{
			try
			{
				IDictionary<string, string> arguments = new Dictionary<string, string>();
				if (args.Payload != null)
				{
					// ReSharper disable once MergeIntoPattern
					if (args.Variables != null)
					{
						foreach (var pair in args.Variables)
						{
							if (!string.IsNullOrEmpty(pair.Key))
							{
								if (!string.IsNullOrEmpty(pair.Value))
								{
									if (!arguments.ContainsKey(pair.Key))
									{
										arguments.Add(pair.Key, formatter.Format(pair.Value, args.Payload));
									}
								}
								else
								{
									arguments.Add(pair.Key, string.Empty);
								}
							}
						}
					}
				}
				
				var mailBody = formatter.Format(args.Mailhook.MailTemplate ?? string.Empty, args.Payload);
				var mailSubject = formatter.Format(args.Mailhook.MailSubject ?? string.Empty, args.Payload);
				await this.SendMailAsync(
					args.MailProvider,
					args.Mailhook.FromName ?? string.Empty,
					args.Mailhook.FromAddress ?? string.Empty,
					recipients,
					mailSubject,
					mailBody, 
					args.Mailhook.MailTemplate ?? string.Empty,
					arguments,
					cancellationToken: cancellationToken);
				
				await this._eventService.FireEventAsync(ErtisAuthEventType.MailhookMailSent, args.UserId!, args.MembershipId, new { recipients }, cancellationToken: cancellationToken);
				
				this._logger.LogInformation("The hook mail sent");
			}
			catch (Exception ex)
			{
				await this._eventService.FireEventAsync(ErtisAuthEventType.MailhookMailFailed, args.UserId!, args.MembershipId, new { recipients, error = ex.Message }, cancellationToken: cancellationToken);
				
				this._logger.LogError(ex, "The hook mail could not be sent!");
			}
		}
	}
	
	private async Task SendMailAsync(
		IMailProvider mailProvider,
		string fromName,
		string fromAddress,
		IEnumerable<Recipient> recipients,
		string subject,
		string htmlBody,
		string templateId,
		IDictionary<string, string> arguments,
		CancellationToken cancellationToken = default)
	{
		switch (mailProvider.DeliveryMode)
		{
			case DeliveryMode.Default:
			case DeliveryMode.Raw:
				await mailProvider.SendMailAsync(
					fromName,
					fromAddress,
					recipients,
					subject,
					htmlBody,
					cancellationToken: cancellationToken);
			break;
			case DeliveryMode.Template:
				await mailProvider.SendMailWithTemplateAsync(
					fromName,
					fromAddress,
					recipients,
					subject,
					templateId,
					arguments,
					cancellationToken: cancellationToken);
			break;
		}
	}
	
	#endregion
}

public class MailServiceBackgroundWorkerArgs
{
	#region Properties
	
	public MailHook? Mailhook { get; init; }
	
	public IMailProvider? MailProvider { get; init; }
	
	public string? UserId { get; init; }
	
	public string? MembershipId { get; init; }
	
	public object? Payload { get; init; }
	
	public MailHookVariable[]? Variables { get; init; }
	
	#endregion
}