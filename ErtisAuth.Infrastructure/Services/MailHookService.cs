using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.MongoDB.Queries;
using Ertis.Schema.Dynamics;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Infrastructure.Mapping;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Mailing;
using ErtisAuth.Events.EventArgs;
using ErtisAuth.Extensions.Mailkit.Models;
using ErtisAuth.Extensions.Mailkit.Services.Interfaces;
using Newtonsoft.Json;

namespace ErtisAuth.Infrastructure.Services
{
    public class MailHookService : MembershipBoundedCrudService<MailHook, MailHookDto>, IMailHookService
    {
	    #region Constants

	    private const string USER_ACTIVATION_MAIL_HOOK_NAME = "User Activation";

	    #endregion
	    
	    #region Services
	    
	    private readonly IMailService _mailService;
	    private readonly IEventService _eventService;
	    private readonly IUserRepository _userRepository;

	    #endregion
	    
        #region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="membershipService"></param>
		/// <param name="eventService"></param>
		/// <param name="mailService"></param>
		/// <param name="userRepository"></param>
		/// <param name="mailHookRepository"></param>
		public MailHookService(
			IMembershipService membershipService, 
			IEventService eventService,
			IMailService mailService,
			IUserRepository userRepository,
			IMailHookRepository mailHookRepository) : base(membershipService, mailHookRepository)
		{
			this._userRepository = userRepository;
			this._mailService = mailService;
			this._eventService = eventService;
			
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
						this.SendHookMail(mailHook, ertisAuthEvent);
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
					var recipients = new List<Recipient>();
					if (mailHook.SendToUtilizer)
					{
						var dto = await this._userRepository.FindOneAsync(userId, cancellationToken: cancellationToken);
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
					if (mailHook.Recipients != null)
					{
						recipients.AddRange(mailHook.Recipients.Select(x => new Recipient
						{
							DisplayName = formatter.Format(x.DisplayName, payload),
							EmailAddress = formatter.Format(x.EmailAddress, payload)
						}));
					}

					recipients = recipients.DistinctBy(x => x.EmailAddress).ToList();
					if (recipients.Any())
					{
						await Task.Run(async () =>
						{
							try
							{
								var mailBody = formatter.Format(mailHook.MailTemplate, payload);
								var mailSubject = formatter.Format(mailHook.MailSubject, payload);
								await this._mailService.SendMailAsync(
									mailProvider,
									mailHook.FromName,
									mailHook.FromAddress,
									recipients,
									mailSubject,
									mailBody, 
									cancellationToken: cancellationToken
								);
								
								await this._eventService.FireEventAsync(this, new ErtisAuthEvent(
									ErtisAuthEventType.MailhookMailSent,
									userId,
									membershipId,
									new
									{
										recipients
									}
								), cancellationToken: cancellationToken);
							}
							catch (Exception ex)
							{
								Console.WriteLine("The hook mail could not be sent!");
								Console.WriteLine(ex);
								
								await this._eventService.FireEventAsync(this, new ErtisAuthEvent(
									ErtisAuthEventType.MailhookMailFailed,
									userId,
									membershipId,
									new
									{
										recipients,
										error = ex.Message 
									}
								), cancellationToken: cancellationToken);
							}
						}, cancellationToken);
					}
				}
			}
		}
		
		public async Task<MailHook> GetUserActivationMailHookAsync(string membershipId, CancellationToken cancellationToken = default)
		{
			return await this.GetAsync(membershipId, x =>
					x.Name == USER_ACTIVATION_MAIL_HOOK_NAME &&
					x.MembershipId == membershipId &&
					x.Event == ErtisAuthEventType.UserCreated.ToString() &&
					x.Status == "active" &&
					x.SendToUtilizer == true,
				cancellationToken: cancellationToken);
		}

		protected override bool ValidateModel(MailHook model, out IEnumerable<string> errors)
		{
			var errorList = new List<string>();
			if (string.IsNullOrEmpty(model.Name))
			{
				errorList.Add("name is a required field");
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