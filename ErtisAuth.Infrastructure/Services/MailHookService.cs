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
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Mailing;
using ErtisAuth.Events.EventArgs;
using ErtisAuth.Extensions.Mailkit.Services.Interfaces;
using Newtonsoft.Json;

namespace ErtisAuth.Infrastructure.Services
{
    public class MailHookService : MembershipBoundedCrudService<MailHook, MailHookDto>, IMailHookService
    {
	    #region Services

	    private readonly IUserService userService;
	    private readonly IMailService mailService;
	    private readonly IEventService eventService;

	    #endregion
	    
        #region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="membershipService"></param>
		/// <param name="eventService"></param>
		/// <param name="userService"></param>
		/// <param name="mailService"></param>
		/// <param name="mailHookRepository"></param>
		public MailHookService(
			IMembershipService membershipService, 
			IEventService eventService,
			IUserService userService,
			IMailService mailService,
			IMailHookRepository mailHookRepository) : base(membershipService, mailHookRepository)
		{
			this.userService = userService;
			this.mailService = mailService;
			this.eventService = eventService;
			
			this.eventService.EventFired += EventServiceOnEventFired;
			
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
						this.SendHookMailAsync(mailHook, ertisAuthEvent);
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
			await this.eventService.FireEventAsync(this, new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.MailhookCreated,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Resource,
				MembershipId = eventArgs.Utilizer.MembershipId
			});
		}
		
		private async void MailhookUpdatedEventHandler(object sender, UpdateResourceEventArgs<MailHook> eventArgs)
		{
			await this.eventService.FireEventAsync(this, new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.MailhookUpdated,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Updated,
				Prior = eventArgs.Prior,
				MembershipId = eventArgs.Utilizer.MembershipId
			});
		}
		
		private async void MailhookDeletedEventHandler(object sender, DeleteResourceEventArgs<MailHook> eventArgs)
		{
			await this.eventService.FireEventAsync(this, new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.MailhookDeleted,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Resource,
				MembershipId = eventArgs.Utilizer.MembershipId
			});
		}

		#endregion
		
		#region Methods
		
		private async void SendHookMailAsync(MailHook mailHook, IErtisAuthEvent ertisAuthEvent, CancellationToken cancellationToken = default)
		{
			if (mailHook.IsActive)
			{
				var membership = await this.membershipService.GetAsync(mailHook.MembershipId, cancellationToken: cancellationToken);
				var mailProvider = membership?.MailProviders.FirstOrDefault(x => x.Slug == mailHook.MailProvider);
				if (mailProvider != null)
				{
					var dynamicObject = await this.userService.GetAsync(membership.Id, ertisAuthEvent.UtilizerId, cancellationToken: cancellationToken);
					var user = dynamicObject.Deserialize<User>();
					if (user != null)
					{
						await Task.Run(async () =>
						{
							try
							{
								var formatter = new Ertis.TemplateEngine.Formatter();
								var mailBody = formatter.Format(mailHook.MailTemplate, ertisAuthEvent);
								var mailSubject = formatter.Format(mailHook.MailSubject, ertisAuthEvent);
								await this.mailService.SendMailAsync(
									mailProvider,
									mailHook.FromName,
									mailHook.FromAddress,
									$"{user.FirstName} {user.LastName}",
									user.EmailAddress,
									mailSubject,
									mailBody, 
									cancellationToken: cancellationToken
								);
								
								await this.eventService.FireEventAsync(this, new ErtisAuthEvent(
									ErtisAuthEventType.MailhookMailSent,
									ertisAuthEvent.UtilizerId,
									membership.Id
								), cancellationToken: cancellationToken);
							}
							catch (Exception ex)
							{
								Console.WriteLine("The hook mail could not be sent!");
								Console.WriteLine(ex);
								
								await this.eventService.FireEventAsync(this, new ErtisAuthEvent(
									ErtisAuthEventType.MailhookMailFailed,
									ertisAuthEvent.UtilizerId,
									membership.Id
								), cancellationToken: cancellationToken);
							}
						}, cancellationToken);	
					}
				}
			}
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
			
			if (string.IsNullOrEmpty(model.MailSubject))
			{
				errorList.Add("MailSubject is a required field");
			}
			
			if (string.IsNullOrEmpty(model.FromName))
			{
				errorList.Add("FromName is a required field");
			}
			
			if (string.IsNullOrEmpty(model.FromAddress))
			{
				errorList.Add("FromAddress is a required field");
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