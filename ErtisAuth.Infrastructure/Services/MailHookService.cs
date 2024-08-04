using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.MongoDB.Queries;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Infrastructure.Mapping;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Mailing;
using ErtisAuth.Events.EventArgs;
using Newtonsoft.Json;

namespace ErtisAuth.Infrastructure.Services
{
    public class MailHookService : MembershipBoundedCrudService<MailHook, MailHookDto>, IMailHookService
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
	    
	    private readonly IEventService _eventService;
	    private readonly IMailServiceBackgroundWorker _mailServiceBackgroundWorker;

	    #endregion
	    
        #region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="membershipService"></param>
		/// <param name="eventService"></param>
		/// <param name="mailServiceBackgroundWorker"></param>
		/// <param name="mailHookRepository"></param>
		public MailHookService(
			IMembershipService membershipService, 
			IEventService eventService,
			IMailServiceBackgroundWorker mailServiceBackgroundWorker, 
			IMailHookRepository mailHookRepository) : base(membershipService, mailHookRepository)
		{
			this._eventService = eventService;
			this._mailServiceBackgroundWorker = mailServiceBackgroundWorker;
			
			this._eventService.EventFired += EventServiceOnEventFired;
			
			this.OnCreated += this.MailhookCreatedEventHandler;
			this.OnUpdated += this.MailhookUpdatedEventHandler;
			this.OnDeleted += this.MailhookDeletedEventHandler;
		}

		#endregion

		#region Event Handlers
		
		private void EventServiceOnEventFired(object sender, ErtisAuthEvent ertisAuthEvent)
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
				var mailHooks = JsonConvert.DeserializeObject<PaginationCollection<MailHook>>(JsonConvert.SerializeObject(mailHooksDynamicCollection));
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
				Console.WriteLine(ex);
			}
		}
		
		private async void MailhookCreatedEventHandler(object sender, CreateResourceEventArgs<MailHook> eventArgs)
		{
			await this._eventService.FireEventAsync(this, new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.MailhookCreated,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Resource,
				MembershipId = eventArgs.MembershipId
			});
		}
		
		private async void MailhookUpdatedEventHandler(object sender, UpdateResourceEventArgs<MailHook> eventArgs)
		{
			await this._eventService.FireEventAsync(this, new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.MailhookUpdated,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Updated,
				Prior = eventArgs.Prior,
				MembershipId = eventArgs.MembershipId
			});
		}
		
		private async void MailhookDeletedEventHandler(object sender, DeleteResourceEventArgs<MailHook> eventArgs)
		{
			await this._eventService.FireEventAsync(this, new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.MailhookDeleted,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Resource,
				MembershipId = eventArgs.MembershipId
			});
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
		
		public async void SendHookMailAsync(MailHook mailHook, string userId, string membershipId, object payload, CancellationToken cancellationToken = default)
		{
			if (mailHook.IsActive)
			{
				var membership = await this.membershipService.GetAsync(mailHook.MembershipId, cancellationToken: cancellationToken);
				var mailProvider = membership?.MailProviders.FirstOrDefault(x => x.Slug == mailHook.MailProvider);
				if (mailProvider != null)
				{
					await this._mailServiceBackgroundWorker.StartAsync(new MailServiceBackgroundWorkerArgs
					{
						Mailhook = mailHook,
						MailProvider = mailProvider,
						UserId = userId,
						MembershipId = membershipId,
						Payload = payload,
						Variables = mailHook.Variables
					});
				}
			}
		}
		
		public async Task<MailHook> GetUserActivationMailHookAsync(string membershipId, CancellationToken cancellationToken = default)
		{
			return await this.GetAsync(membershipId, x =>
					x.Name == USER_ACTIVATION_MAIL_HOOK_NAME &&
					x.MembershipId == membershipId &&
					x.Event == ErtisAuthEventType.UserCreated.ToString() &&
					x.Status == "active",
				cancellationToken: cancellationToken);
		}
		
		public async Task<MailHook> GetResetPasswordMailHookAsync(string membershipId, CancellationToken cancellationToken = default)
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
		
		protected override bool IsAlreadyExist(MailHook model, string membershipId, MailHook exclude = default)
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

		protected override async Task<bool> IsAlreadyExistAsync(MailHook model, string membershipId, MailHook exclude = default)
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
		
		private MailHook GetByName(string name, string membershipId)
		{
			var dto = this.repository.FindOne(x => x.Name == name && x.MembershipId == membershipId);
			return dto == null ? null : Mapper.Current.Map<MailHookDto, MailHook>(dto);
		}
		
		private async Task<MailHook> GetByNameAsync(string name, string membershipId)
		{
			var dto = await this.repository.FindOneAsync(x => x.Name == name && x.MembershipId == membershipId);
			return dto == null ? null : Mapper.Current.Map<MailHookDto, MailHook>(dto);
		}
		
		#endregion
    }
}