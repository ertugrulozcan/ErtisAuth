using System.Net;
using System.Text.Json;
using Ertis.Schema.Dynamics;
using Ertis.MongoDB.Serialization;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Core.Events;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Extensions;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Extensions.Mailing.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Infrastructure.Services;

public class MailHookService : MembershipBoundedCrudService<MailHook>, IMailHookService
{
    #region Constants

    private const string USER_ACTIVATION_MAIL_HOOK_NAME = "User Activation";
    private const string USER_ACTIVATION_MAIL_HOOK_SLUG = "user-activation";
    private const string RESET_PASSWORD_MAIL_HOOK_NAME = "Reset Password";
    private const string RESET_PASSWORD_MAIL_HOOK_SLUG = "reset-password";

    /// <summary>
    /// Event documents may contain ObjectIds (DynamicObjects, e.g. users, carry their own converter).
    /// </summary>
    private static readonly JsonSerializerOptions TemplateDataSerializerOptions = new()
    {
	    Converters =
	    {
		    new ObjectIdConverter()
	    }
    };

    private static readonly Ertis.TemplateEngine.ParserOptions HtmlParserOptions = new()
    {
	    OpenBrackets = "{{",
	    CloseBrackets = "}}",
	    ValueEncoder = WebUtility.HtmlEncode
    };
    
    private static readonly string[] PredefinedAutonomouslyMailHooks =
    {
	    USER_ACTIVATION_MAIL_HOOK_SLUG,
	    RESET_PASSWORD_MAIL_HOOK_SLUG
    };

    #endregion

    #region Services
	
	private readonly IEventService _eventService;
	private readonly IEnumerable<IMailService> _mailServices;
	private readonly IBackgroundQueue<HookMail> _mailHookQueue;
	private readonly IUserRepository _userRepository;
	private readonly ILogger<MailHookService> _logger;

    #endregion

    #region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="membershipService"></param>
	/// <param name="eventService"></param>
	/// <param name="mailServices"></param>
	/// <param name="mailHookQueue"></param>
	/// <param name="mailHookRepository"></param>
	/// <param name="userRepository"></param>
	/// <param name="logger"></param>
	public MailHookService(
		IMembershipService membershipService,
		IEventService eventService,
		IEnumerable<IMailService> mailServices,
		IBackgroundQueue<HookMail> mailHookQueue,
		IMailHookRepository mailHookRepository,
		IUserRepository userRepository,
		ILogger<MailHookService> logger) : base(membershipService, mailHookRepository)
	{
		this._eventService = eventService;
		this._mailServices = mailServices;
		this._mailHookQueue = mailHookQueue;
		this._userRepository = userRepository;
		this._logger = logger;
		
		this._eventService.OnEventFired += this.OnEventFired;
		
		this.OnCreated += this.MailhookCreatedEventHandler;
		this.OnUpdated += this.MailhookUpdatedEventHandler;
		this.OnDeleted += this.MailhookDeletedEventHandler;
	}
	
	#endregion
	
	#region Event Handlers
	
	/// <summary>
	/// Raised synchronously by EventService, so the hook lookup must not block the request that fired the event.
	/// async void is safe here: OnEventFiredAsync catches and logs every exception.
	/// </summary>
	// ReSharper disable once AsyncVoidMethod
	private async void OnEventFired(object? _, ErtisAuthEvent ertisAuthEvent)
	{
		await this.OnEventFiredAsync(ertisAuthEvent);
	}
	
	private async Task OnEventFiredAsync(ErtisAuthEvent ertisAuthEvent, CancellationToken cancellationToken = default)
	{
		try
		{
			var membershipId = ertisAuthEvent.MembershipId;
			var eventType = ertisAuthEvent.EventType.ToString();
			var mailHooks = await this._repository.FindAsync(
				x => x.MembershipId == membershipId && x.Status == "active" && x.Event == eventType,
				sorting: null,
				cancellationToken: cancellationToken);
			
			foreach (var mailHook in mailHooks.Items)
			{
				if (PredefinedAutonomouslyMailHooks.All(x => x != mailHook.Slug))
				{
					this.QueueHookMail(
						mailHook,
						ertisAuthEvent.UtilizerId,
						ertisAuthEvent.MembershipId,
						ertisAuthEvent);
				}
			}
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "MailhookService.OnEventFired occured an error");
		}
	}
	
	private async void MailhookCreatedEventHandler(object? sender, CreateResourceEventArgs<MailHook> eventArgs)
	{
		try
		{
			await this._eventService.FireEventAsync(ErtisAuthEventType.MailhookCreated, eventArgs.Utilizer, eventArgs.MembershipId, eventArgs.Resource);
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "MailhookService.MailhookCreatedEventHandler occured an error");
		}
	}
	
	private async void MailhookUpdatedEventHandler(object? sender, UpdateResourceEventArgs<MailHook> eventArgs)
	{
		try
		{
			await this._eventService.FireEventAsync(ErtisAuthEventType.MailhookUpdated, eventArgs.Utilizer, eventArgs.MembershipId, eventArgs.Updated, eventArgs.Prior);
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "MailhookService.MailhookUpdatedEventHandler occured an error");
		}
	}
	
	private async void MailhookDeletedEventHandler(object? sender, DeleteResourceEventArgs<MailHook> eventArgs)
	{
		try
		{
			await this._eventService.FireEventAsync(ErtisAuthEventType.MailhookDeleted, eventArgs.Utilizer, eventArgs.MembershipId, null, eventArgs.Resource);
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "MailhookService.MailhookDeletedEventHandler occured an error");
		}
	}
	
	#endregion
	
	#region Methods
	
	public void QueueHookMail(MailHook mailHook, string userId, string membershipId, object? payload)
	{
		try
		{
			// The payload is snapshotted now: the mail is sent later, when the caller may have changed its objects
			var mail = new HookMail(mailHook, userId, membershipId, ToTemplateData(payload));
			if (!this._mailHookQueue.TryEnqueue(mail))
			{
				this._logger.LogError("The hook mail '{MailHook}' could not be queued: the mail queue is full or closed", mailHook.Name);
			}
		}
		catch (Exception ex)
		{
			// Queuing is best effort like the sending: the flow which queues the mail (e.g. a password reset) does not fail
			this._logger.LogError(ex, "The hook mail '{MailHook}' could not be queued", mailHook.Name);
		}
	}
	
	public async Task SendHookMailAsync(HookMail mail, CancellationToken cancellationToken = default)
	{
		try
		{
			if (mail.MailHook.IsActive)
			{
				var membership = await this._membershipService.GetAsync(mail.MailHook.MembershipId, cancellationToken: cancellationToken);
				var mailProvider = membership?.MailProviders?.FirstOrDefault(x => x.Slug == mail.MailHook.MailProvider);
				if (mailProvider != null)
				{
					await this.SendMailAsync(mail.MailHook, mailProvider, mail.UserId, mail.MembershipId, mail.Payload, cancellationToken: cancellationToken);
				}
			}
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "MailhookService.SendHookMailAsync occured an error");
		}
	}
	
	private async Task SendMailAsync(MailHook mailhook, IMailProvider mailProvider, string userId, string membershipId, object? payload, CancellationToken cancellationToken = default)
	{
		payload = ToTemplateData(payload);
		var recipients = new List<Recipient>();
		if (mailhook.SendToUtilizer)
		{
			var model = await this._userRepository.FindOneAsync(userId, cancellationToken: cancellationToken);
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
		if (mailhook.Recipients != null)
		{
			recipients.AddRange(mailhook.Recipients.Select(x => new Recipient
			{
				DisplayName = formatter.Format(x.DisplayName, payload),
				EmailAddress = formatter.Format(x.EmailAddress, payload)
			}));
		}
		
		recipients = recipients.DistinctBy(x => x.EmailAddress).ToList();
		if (recipients.Any())
		{
			try
			{
				IDictionary<string, string> arguments = new Dictionary<string, string>();
				if (payload != null)
				{
					// ReSharper disable once MergeIntoPattern
					if (mailhook.Variables != null)
					{
						foreach (var pair in mailhook.Variables)
						{
							if (!string.IsNullOrEmpty(pair.Key))
							{
								if (!string.IsNullOrEmpty(pair.Value))
								{
									if (!arguments.ContainsKey(pair.Key))
									{
										arguments.Add(pair.Key, formatter.Format(pair.Value, payload));
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
				
				var mailBody = FormatHtml(mailhook.MailTemplate ?? string.Empty, payload);
				var mailSubject = FormatSingleLine(formatter, mailhook.MailSubject ?? string.Empty, payload);
				await this.SendMailAsync(
					mailProvider,
					mailhook.FromName ?? string.Empty,
					mailhook.FromAddress ?? string.Empty,
					recipients,
					mailSubject,
					mailBody, 
					mailhook.MailTemplate ?? string.Empty,
					arguments,
					cancellationToken: cancellationToken);
				
				await this._eventService.FireEventAsync(ErtisAuthEventType.MailhookMailSent, userId, membershipId, new { recipients }, cancellationToken: cancellationToken);
				
				this._logger.LogInformation("The hook mail sent");
			}
			catch (Exception ex)
			{
				await this._eventService.FireEventAsync(ErtisAuthEventType.MailhookMailFailed, userId, membershipId, new { recipients, error = ex.Message }, cancellationToken: cancellationToken);
				
				this._logger.LogError(ex, "The hook mail could not be sent!");
			}
		}
	}
	
	/// <summary>
	/// The template engine resolves placeholders by the JSON names of the payload ({{document.firstname}}, {{event_type}}),
	/// but can not walk into a DynamicObject (e.g. the document of an event): the payload is converted to plain dynamic data.
	/// </summary>
	private static object? ToTemplateData(object? payload)
	{
		if (payload == null)
		{
			return null;
		}
		
		var json = JsonSerializer.Serialize(payload, TemplateDataSerializerOptions);
		return DynamicObject.Parse(json).ToDynamic();
	}
	
	/// <summary>
	/// The mail body is HTML and payload values (e.g. a user's own name) are user-controlled: each resolved value is
	/// HTML-encoded, the template's own markup and the unresolved placeholders are kept as they are.
	/// </summary>
	private static string FormatHtml(string template, object? payload)
	{
		return new Ertis.TemplateEngine.Formatter(HtmlParserOptions).Format(template, payload);
	}
	
	/// <summary>
	/// The subject is plain text; line breaks from payload values are removed (header injection).
	/// </summary>
	private static string FormatSingleLine(Ertis.TemplateEngine.Formatter formatter, string template, object? payload)
	{
		return formatter.Format(template, payload).ReplaceLineEndings(" ");
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
		// The failures are thrown (not only logged), so the hook mail is reported as failed instead of sent
		var service = this._mailServices.FirstOrDefault(x => x.GetProviderType() == mailProvider.Type);
		if (service == null)
		{
			throw new InvalidOperationException($"No mail service is registered for the {mailProvider.Type} provider");
		}
		
		switch (mailProvider.DeliveryMode)
		{
			case DeliveryMode.Default:
			case DeliveryMode.Raw:
				if (service is IRawMailService rawMailService)
				{
					await rawMailService.SendMailAsync(
						mailProvider,
						fromName,
						fromAddress,
						recipients,
						subject,
						htmlBody,
						cancellationToken: cancellationToken);
				}
				else
				{
					throw new InvalidOperationException($"The {mailProvider.Type} mail service does not support raw mails");
				}
			break;
			case DeliveryMode.Template:
				if (service is ITemplateMailService templateMailService)
				{
					await templateMailService.SendMailWithTemplateAsync(
						mailProvider,
						fromName,
						fromAddress,
						recipients,
						subject,
						templateId,
						arguments,
						cancellationToken: cancellationToken);
				}
				else
				{
					throw new InvalidOperationException($"The {mailProvider.Type} mail service does not support template mails");
				}
			break;
		}
	}
	
	public async Task<MailHook?> GetUserActivationMailHookAsync(string membershipId, CancellationToken cancellationToken = default)
	{
		return await this.GetAsync(x =>
				x.Name == USER_ACTIVATION_MAIL_HOOK_NAME &&
				x.MembershipId == membershipId &&
				x.Event == ErtisAuthEventType.UserCreated.ToString() &&
				x.Status == "active", 
			membershipId, 
			cancellationToken: cancellationToken);
	}
	
	public async Task<MailHook?> GetResetPasswordMailHookAsync(string membershipId, CancellationToken cancellationToken = default)
	{
		return await this.GetAsync(x =>
				x.Name == RESET_PASSWORD_MAIL_HOOK_NAME &&
				x.MembershipId == membershipId &&
				x.Event == ErtisAuthEventType.UserPasswordReset.ToString() &&
				x.Status == "active", 
			membershipId, 
			cancellationToken: cancellationToken);
	}
	
	protected override Task<IEnumerable<string>> ValidateModelAsync(MailHook model, CancellationToken cancellationToken = default)
	{
		var errorList = new List<string>();
		if (string.IsNullOrEmpty(model.Name))
		{
			errorList.Add("name is a required field");
		}
		
		if (!model.Slug.IsValidSlug(out var error))
		{
			errorList.Add(error!);
		}
		
		if (string.IsNullOrEmpty(model.MembershipId))
		{
			errorList.Add("membership_id is a required field");
		}
		
		if (string.IsNullOrEmpty(model.MailProvider))
		{
			errorList.Add("Mail provider is a required field");
		}
		
		if (string.IsNullOrEmpty(model.Status))
		{
			errorList.Add("status is a required field");
		}
		else if (model.Status != "active" && model.Status != "passive")
		{
			errorList.Add("Status should be 'active' or 'passive'");
		}
		
		if (string.IsNullOrEmpty(model.Event))
		{
			errorList.Add("event is a required field");
		}
		else if (model.EventType == null)
		{
			errorList.Add($"Unknown event type. (Supported events: [{string.Join(", ", Enum.GetNames(typeof(ErtisAuthEventType)))}])");
		}
		
		// Required on create as well as on update: without them no mail can be sent
		// (no recipients at all unless the mail goes to the utilizer)
		if (string.IsNullOrEmpty(model.MailSubject))
		{
			errorList.Add("MailSubject is a required field");
		}
		
		if (!model.SendToUtilizer && (model.Recipients == null || !model.Recipients.Any()))
		{
			errorList.Add("Recipients list is empty");
		}
		
		if (string.IsNullOrEmpty(model.FromName))
		{
			errorList.Add("FromName is a required field");
		}
		
		if (string.IsNullOrEmpty(model.FromAddress))
		{
			errorList.Add("FromAddress is a required field");
		}
		
		return Task.FromResult<IEnumerable<string>>(errorList);
	}
	
	protected override void Overwrite(MailHook destination, MailHook source)
	{
		destination.Id = source.Id;
		destination.MembershipId = source.MembershipId;
		destination.Sys = source.Sys;
		
		if (this.IsIdentical(destination, source))
		{
			throw ErtisAuthException.IdenticalDocument();
		}
		
		if (string.IsNullOrEmpty(destination.Name))
		{
			destination.Name = source.Name;
		}
		
		if (string.IsNullOrEmpty(destination.Slug))
		{
			destination.Slug = source.Slug;
		}
		
		if (string.IsNullOrEmpty(destination.Description))
		{
			destination.Description = source.Description;
		}
		
		if (string.IsNullOrEmpty(destination.Event))
		{
			destination.Event = source.Event;
		}
		
		if (string.IsNullOrEmpty(destination.Status))
		{
			destination.Status = source.Status;
		}
		
		if (string.IsNullOrEmpty(destination.MailSubject))
		{
			destination.MailSubject = source.MailSubject;
		}
		
		if (string.IsNullOrEmpty(destination.MailTemplate))
		{
			destination.MailTemplate = source.MailTemplate;
		}
		
		if (string.IsNullOrEmpty(destination.FromName))
		{
			destination.FromName = source.FromName;
		}
		
		if (string.IsNullOrEmpty(destination.FromAddress))
		{
			destination.FromAddress = source.FromAddress;
		}
		
		if (string.IsNullOrEmpty(destination.MailProvider))
		{
			destination.MailProvider = source.MailProvider;
		}
	}
	
	protected override async Task<bool> IsAlreadyExistAsync(MailHook model, string membershipId, MailHook? exclude = null, CancellationToken cancellationToken = default)
	{
		if (exclude == null)
		{
			return await this.GetByNameAsync(model.Name, membershipId) != null;	
		}
		else
		{
			var current = await this.GetByNameAsync(model.Name, membershipId);
			if (current != null)
			{
				return current.Name != exclude.Name;	
			}
			else
			{
				return false;
			}
		}
	}
	
	protected override ErtisAuthException GetAlreadyExistError(MailHook model)
	{
		return ErtisAuthException.MailHookWithSameNameAlreadyExists(model.Name);
	}
	
	protected override ErtisAuthException GetNotFoundError(string id)
	{
		return ErtisAuthException.MailHookNotFound(id);
	}
	
	private async Task<MailHook?> GetByNameAsync(string name, string membershipId)
	{
		return await this._repository.FindOneAsync(x => x.Name == name && x.MembershipId == membershipId);
	}
	
	#endregion
}