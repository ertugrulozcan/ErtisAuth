using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ertis.Schema.Dynamics;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Extensions.Hosting;
using ErtisAuth.Extensions.Mailkit.Models;
using ErtisAuth.Extensions.Mailkit.Services.Interfaces;
using Microsoft.Extensions.Logging;

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
	/// <param name="logger"></param>
	public MailServiceBackgroundWorker(
		IMailService mailService,
		IEventService eventService,
		IUserRepository userRepository,
		IBackgroundTaskQueue taskQueue,
		ILogger<MailServiceBackgroundWorker> logger) :
		base(taskQueue, logger)
	{
		this._mailService = mailService;
		this._eventService = eventService;
		this._userRepository = userRepository;
	}

	#endregion
	
	#region Methods

	protected override async ValueTask ExecuteAsync(MailServiceBackgroundWorkerArgs args = null, CancellationToken cancellationToken = default)
	{
		if (args == null)
		{
			return;
		}
		
		var recipients = new List<Recipient>();
		if (args.Mailhook.SendToUtilizer)
		{
			var dto = await this._userRepository.FindOneAsync(args.UserId, cancellationToken: cancellationToken);
			var dynamicObject = dto == null ? null : new DynamicObject(dto);
			var user = dynamicObject?.Deserialize<User>();
			if (user != null)
			{
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
				
				var mailBody = formatter.Format(args.Mailhook.MailTemplate, args.Payload);
				var mailSubject = formatter.Format(args.Mailhook.MailSubject, args.Payload);
				await this._mailService.SendMailAsync(
					args.MailProvider,
					args.Mailhook.FromName,
					args.Mailhook.FromAddress,
					recipients,
					mailSubject,
					mailBody, 
					args.Mailhook.MailTemplate,
					arguments,
					cancellationToken: cancellationToken);
					
				await this._eventService.FireEventAsync(this, new ErtisAuthEvent(
					ErtisAuthEventType.MailhookMailSent,
					args.UserId,
					args.MembershipId,
					new
					{
						recipients
					}
				), cancellationToken: cancellationToken);
				
				Console.WriteLine("The hook mail sent");
			}
			catch (Exception ex)
			{
				Console.WriteLine("The hook mail could not be sent!");
				Console.WriteLine(ex);
					
				await this._eventService.FireEventAsync(this, new ErtisAuthEvent(
					ErtisAuthEventType.MailhookMailFailed,
					args.UserId,
					args.MembershipId,
					new
					{
						recipients,
						error = ex.Message 
					}
				), cancellationToken: cancellationToken);
			}
		}
	}


	#endregion
}