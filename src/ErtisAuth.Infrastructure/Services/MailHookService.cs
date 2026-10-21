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
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Infrastructure.Services;

public class MailHookService : MembershipBoundedCrudService<MailHook>, IMailHookService
{
    #region Constants
	
	internal const string USER_ACTIVATION_MAIL_HOOK_SLUG = "user-activation";
	
	// ReSharper disable once MemberCanBePrivate.Global
	internal const string RESET_PASSWORD_MAIL_HOOK_SLUG = "reset-password";
	
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
	private readonly IMailService _mailService;
	private readonly IUserRepository _userRepository;
	private readonly ILogger<MailHookService> _logger;
	
    #endregion
	
    #region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="membershipService"></param>
	/// <param name="eventService"></param>
	/// <param name="mailService"></param>
	/// <param name="mailHookRepository"></param>
	/// <param name="userRepository"></param>
	/// <param name="logger"></param>
	public MailHookService(
		IMembershipService membershipService,
		IEventService eventService,
		IMailService mailService,
		IMailHookRepository mailHookRepository,
		IUserRepository userRepository,
		ILogger<MailHookService> logger) : base(membershipService, mailHookRepository)
	{
		this._eventService = eventService;
		this._mailService = mailService;
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
					await this.QueueHookMailAsync(
						mailHook,
						ertisAuthEvent.UtilizerId,
						ertisAuthEvent.MembershipId,
						ertisAuthEvent,
						cancellationToken: cancellationToken);
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
	
	private async Task<MailHook?> GetBySlugAsync(string slug, string membershipId)
	{
		return await this._repository.FindOneAsync(x => x.Slug == slug && x.MembershipId == membershipId);
	}
	
	public async Task QueueHookMailAsync(MailHook mailHook, string userId, string membershipId, object? payload, CancellationToken cancellationToken = default)
	{
		if (!mailHook.IsActive)
		{
			return;
		}
		
		try
		{
			// The mail is rendered now: the payload is a snapshot of the caller's objects, which may change before the mail is sent
			var mail = await this.RenderMailAsync(mailHook, userId, payload, cancellationToken: cancellationToken);
			if (mail == null)
			{
				this._logger.LogWarning("The hook mail '{MailHook}' has no recipients", mailHook.Name);
				return;
			}
			
			this._mailService.QueueMail(mail, error => this.OnHookMailCompletedAsync(mail, userId, membershipId, error));
		}
		catch (Exception ex)
		{
			// Queuing is best effort like the sending: the flow which queues the mail (e.g. a password reset) does not fail
			this._logger.LogError(ex, "The hook mail '{MailHook}' could not be queued", mailHook.Name);
		}
	}
	
	/// <summary>
	/// Called by the mail service after the sending; the failure itself is logged by the mail service
	/// </summary>
	private async Task OnHookMailCompletedAsync(MailMessage mail, string userId, string membershipId, Exception? error)
	{
		var recipients = mail.Recipients;
		if (error == null)
		{
			await this._eventService.FireEventAsync(ErtisAuthEventType.MailhookMailSent, userId, membershipId, new { recipients });
			
			this._logger.LogInformation("The hook mail sent");
		}
		else
		{
			await this._eventService.FireEventAsync(ErtisAuthEventType.MailhookMailFailed, userId, membershipId, new { recipients, error = error.Message });
		}
	}
	
	/// <summary>
	/// Returns null when the mail has no recipients
	/// </summary>
	private async Task<MailMessage?> RenderMailAsync(MailHook mailhook, string userId, object? payload, CancellationToken cancellationToken = default)
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
		if (!recipients.Any())
		{
			return null;
		}
		
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
		
		return new MailMessage
		{
			MembershipId = mailhook.MembershipId,
			MailProvider = mailhook.MailProvider ?? string.Empty,
			FromName = mailhook.FromName ?? string.Empty,
			FromAddress = mailhook.FromAddress ?? string.Empty,
			Recipients = recipients,
			Subject = FormatSingleLine(formatter, mailhook.MailSubject ?? string.Empty, payload),
			HtmlBody = FormatHtml(mailhook.MailTemplate ?? string.Empty, payload),
			TemplateId = mailhook.MailTemplate ?? string.Empty,
			TemplateArguments = arguments
		};
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
	
	public async Task<MailHook?> GetUserActivationMailHookAsync(string membershipId, CancellationToken cancellationToken = default)
	{
		return await this.GetAsync(x =>
				x.Slug == USER_ACTIVATION_MAIL_HOOK_SLUG &&
				x.MembershipId == membershipId &&
				x.Event == ErtisAuthEventType.UserCreated.ToString() &&
				x.Status == "active", 
			membershipId, 
			cancellationToken: cancellationToken);
	}
	
	public async Task<MailHook?> GetResetPasswordMailHookAsync(string membershipId, CancellationToken cancellationToken = default)
	{
		return await this.GetAsync(x =>
				x.Slug == RESET_PASSWORD_MAIL_HOOK_SLUG &&
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
			errorList.Add("Name is required");
		}
		
		if (!model.Slug.IsValidSlug(out var error))
		{
			errorList.Add(error!);
		}
		
		if (string.IsNullOrEmpty(model.MembershipId))
		{
			errorList.Add("Membership id is required");
		}
		
		if (string.IsNullOrEmpty(model.Status))
		{
			errorList.Add("Status is required");
		}
		else if (model.Status != "active" && model.Status != "passive")
		{
			errorList.Add("Status should be 'active' or 'passive'");
		}
		
		if (model.IsActive)
		{
			if (string.IsNullOrEmpty(model.Event))
			{
				errorList.Add("Event type is required");
			}
			else if (model.EventType == null)
			{
				errorList.Add($"Unknown event type. (Supported events: [{string.Join(", ", Enum.GetNames(typeof(ErtisAuthEventType)))}])");
			}
			
			if (string.IsNullOrEmpty(model.MailProvider))
			{
				errorList.Add("Mail provider is required");
			}
			
			if (string.IsNullOrEmpty(model.MailSubject))
			{
				errorList.Add("Mail subject is required");
			}
			
			if (!model.SendToUtilizer && (model.Recipients == null || !model.Recipients.Any()))
			{
				errorList.Add("Recipients list is empty");
			}
			
			if (string.IsNullOrEmpty(model.FromName))
			{
				errorList.Add("From name is required");
			}
			
			if (string.IsNullOrEmpty(model.FromAddress))
			{
				errorList.Add("From address is required");
			}
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
			return await this.GetBySlugAsync(model.Slug, membershipId) != null;	
		}
		else
		{
			var current = await this.GetBySlugAsync(model.Slug, membershipId);
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
		return ErtisAuthException.MailHookAlreadyExists(model.Slug);
	}
	
	protected override ErtisAuthException GetNotFoundError(string id)
	{
		return ErtisAuthException.MailHookNotFound(id);
	}
	
	#endregion
}