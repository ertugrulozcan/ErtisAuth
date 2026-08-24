using Ertis.Schema.Dynamics.Legacy;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Extensions.Hosting;
using ErtisAuth.Extensions.Mailkit.Models;
using ErtisAuth.Extensions.Mailkit.Services.Interfaces;

namespace ErtisAuth.Infrastructure.Services;

public class MailServiceBackgroundWorker : BackgroundWorker<MailServiceBackgroundWorkerArgs>, IMailServiceBackgroundWorker
{
    #region Services
	
    private readonly IMailService _mailService;
    private readonly IEventService _eventService;
    private readonly IUserRepository _userRepository;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="mailService"></param>
	/// <param name="eventService"></param>
	/// <param name="userRepository"></param>
	/// <param name="taskQueue"></param>
	public MailServiceBackgroundWorker(
		IMailService mailService,
		IEventService eventService,
		IUserRepository userRepository,
		IBackgroundTaskQueue taskQueue) :
		base(taskQueue)
	{
		this._mailService = mailService;
		this._eventService = eventService;
		this._userRepository = userRepository;
	}
	
	#endregion
	
	#region Methods
	
	protected override async ValueTask ExecuteAsync(MailServiceBackgroundWorkerArgs? args = null, CancellationToken cancellationToken = default)
	{
		if (args?.Mailhook == null || args.MailProvider == null)
		{
			return;
		}
		
		var recipients = new List<Recipient>();
		if (args.Mailhook.SendToUtilizer && args.UserId != null)
		{
			var dto = await this._userRepository.FindOneAsync(args.UserId, cancellationToken: cancellationToken);
			var dynamicObject = dto == null ? null : new DynamicObject(dto);
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
				await this._mailService.SendMailAsync(
					args.MailProvider,
					args.Mailhook.FromName ?? string.Empty,
					args.Mailhook.FromAddress ?? string.Empty,
					recipients,
					mailSubject,
					mailBody, 
					args.Mailhook.MailTemplate ?? string.Empty,
					arguments,
					cancellationToken: cancellationToken);
				
				var e = new ErtisAuthEvent
				{
					EventType = ErtisAuthEventType.MailhookMailSent,
					UtilizerId = args.UserId!,
					MembershipId = args.MembershipId ?? string.Empty,
					Document = new { recipients }
				};
				
				await this._eventService.FireEventAsync(this, e, cancellationToken: cancellationToken);
				
				Console.WriteLine("The hook mail sent");
			}
			catch (Exception ex)
			{
				var e = new ErtisAuthEvent
				{
					EventType = ErtisAuthEventType.MailhookMailFailed,
					UtilizerId = args.UserId!,
					MembershipId = args.MembershipId ?? string.Empty,
					Document = new { recipients, error = ex.Message }
				};
				
				await this._eventService.FireEventAsync(this, e, cancellationToken: cancellationToken);
				
				Console.WriteLine("The hook mail could not be sent!");
				Console.WriteLine(ex);
			}
		}
	}
	
	#endregion
}