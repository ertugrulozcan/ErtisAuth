using System.Text.Json;
using Ertis.Core.Collections;
using Ertis.Schema.Dynamics;
using Ertis.MongoDB.Queries;
using Ertis.MongoDB.Serialization;
using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Core.Events;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Dao.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Infrastructure.Services;

public class MailHookService : MembershipBoundedCrudService<MailHook>, IMailHookService
{
    #region Constants
	
    private const string USER_ACTIVATION_MAIL_HOOK_NAME = "User Activation";
    private const string USER_ACTIVATION_MAIL_HOOK_SLUG = "user-activation";
    private const string RESET_PASSWORD_MAIL_HOOK_NAME = "Reset Password";
    private const string RESET_PASSWORD_MAIL_HOOK_SLUG = "reset-password";
	
    private static readonly string[] PredefinedAutonomouslyMailHooks =
    {
	    USER_ACTIVATION_MAIL_HOOK_SLUG,
	    RESET_PASSWORD_MAIL_HOOK_SLUG
    };
	
    #endregion
    
    #region Services
	
	private readonly ISystemRestHandler _restHandler;
    private readonly IEventService _eventService;
	private readonly IUserRepository _userRepository;
	private readonly ILogger<MailHookService> _logger;
	
    #endregion
    
    #region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="membershipService"></param>
	/// <param name="restHandler"></param>
	/// <param name="eventService"></param>
	/// <param name="mailHookRepository"></param>
	/// <param name="userRepository"></param>
	/// <param name="logger"></param>
	public MailHookService(
		IMembershipService membershipService,
		ISystemRestHandler restHandler,
		IEventService eventService,
		IMailHookRepository mailHookRepository,
		IUserRepository userRepository,
		ILogger<MailHookService> logger) : base(membershipService, mailHookRepository)
	{
		this._restHandler = restHandler;
		this._eventService = eventService;
		this._userRepository = userRepository;
		this._logger = logger;
		
		this._eventService.OnEventFired += this.OnEventFired;
		
		this.OnCreated += this.MailhookCreatedEventHandler;
		this.OnUpdated += this.MailhookUpdatedEventHandler;
		this.OnDeleted += this.MailhookDeletedEventHandler;
	}
	
	#endregion
	
	#region Event Handlers
	
	private void OnEventFired(object? _, ErtisAuthEvent ertisAuthEvent)
	{
		try
		{
			IQuery[] expressions = 
			{
				QueryBuilder.Equals("status", "active"),
				QueryBuilder.Equals("membership_id", ertisAuthEvent.MembershipId),
				QueryBuilder.Equals("event", ertisAuthEvent.EventType.ToString())
			};
			
			var query = QueryBuilder.Where(expressions);
			var mailHooksDynamicCollection = this.Query(ertisAuthEvent.MembershipId, query.ToString());
			var json = JsonSerializer.Serialize(mailHooksDynamicCollection, new JsonSerializerOptions
			{
				WriteIndented = false,
				Converters =
				{
					new ObjectIdConverter()
				}
			});
			
			var mailHooks = JsonSerializer.Deserialize<PaginationCollection<MailHook>>(json);
			if (mailHooks != null)
			{
				foreach (var mailHook in mailHooks.Items)
				{
					if (PredefinedAutonomouslyMailHooks.All(x => x != mailHook.Slug))
					{
						this.SendHookMail(mailHook, ertisAuthEvent);
					}
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
	
	private void SendHookMail(MailHook mailHook, IErtisAuthEvent ertisAuthEvent, CancellationToken cancellationToken = default)
	{
		this.SendHookMailAsync(
			mailHook, 
			ertisAuthEvent.UtilizerId, 
			ertisAuthEvent.MembershipId, 
			ertisAuthEvent,
			cancellationToken: cancellationToken);
	}
	
	public async void SendHookMailAsync(MailHook mailhook, string userId, string membershipId, object? payload, CancellationToken cancellationToken = default)
	{
		try
		{
			if (mailhook.IsActive)
			{
				var membership = await this._membershipService.GetAsync(mailhook.MembershipId, cancellationToken: cancellationToken);
				var mailProvider = membership?.MailProviders?.FirstOrDefault(x => x.Slug == mailhook.MailProvider);
				if (mailProvider != null)
				{
					await this.SendMailAsync(mailhook, mailProvider, userId, membershipId, payload, cancellationToken: cancellationToken);
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
				
				var mailBody = formatter.Format(mailhook.MailTemplate ?? string.Empty, payload);
				var mailSubject = formatter.Format(mailhook.MailSubject ?? string.Empty, payload);
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
					this._restHandler, 
					fromName,
					fromAddress,
					recipients,
					subject,
					htmlBody,
					cancellationToken: cancellationToken);
			break;
			case DeliveryMode.Template:
				await mailProvider.SendMailWithTemplateAsync(
					this._restHandler, 
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
	
	public async Task<MailHook?> GetUserActivationMailHookAsync(string membershipId, CancellationToken cancellationToken = default)
	{
		return await this.GetAsync(membershipId, x =>
				x.Name == USER_ACTIVATION_MAIL_HOOK_NAME &&
				x.MembershipId == membershipId &&
				x.Event == ErtisAuthEventType.UserCreated.ToString() &&
				x.Status == "active",
			cancellationToken: cancellationToken);
	}
	
	public async Task<MailHook?> GetResetPasswordMailHookAsync(string membershipId, CancellationToken cancellationToken = default)
	{
		return await this.GetAsync(membershipId, x =>
				x.Name == RESET_PASSWORD_MAIL_HOOK_NAME &&
				x.MembershipId == membershipId &&
				x.Event == ErtisAuthEventType.UserPasswordReset.ToString() &&
				x.Status == "active",
			cancellationToken: cancellationToken);
	}
	
	protected override bool ValidateModel(MailHook model, out IEnumerable<string> errors)
	{
		var errorList = new List<string>();
		if (string.IsNullOrEmpty(model.Name))
		{
			errorList.Add("name is a required field");
		}
		
		if (string.IsNullOrEmpty(model.Slug))
		{
			errorList.Add("slug is a required field");
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
		
		if (!string.IsNullOrEmpty(model.Id))
		{
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
		}
		
		errors = errorList;
		return !errors.Any();
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
	
	protected override bool IsAlreadyExist(MailHook model, string membershipId, MailHook? exclude = null)
	{
		if (exclude == null)
		{
			return this.GetByName(model.Name, membershipId) != null;	
		}
		else
		{
			var current = this.GetByName(model.Name, membershipId);
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
	
	private MailHook? GetByName(string name, string membershipId)
	{
		return this._repository.FindOne(x => x.Name == name && x.MembershipId == membershipId);
	}
	
	private async Task<MailHook?> GetByNameAsync(string name, string membershipId)
	{
		return await this._repository.FindOneAsync(x => x.Name == name && x.MembershipId == membershipId);
	}
	
	#endregion
}