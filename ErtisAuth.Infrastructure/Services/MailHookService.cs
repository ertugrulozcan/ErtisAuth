using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.MongoDB.Queries;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Infrastructure.Mapping;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Mailing;
using Newtonsoft.Json;

namespace ErtisAuth.Infrastructure.Services
{
    public class MailHookService : MembershipBoundedCrudService<MailHook, MailHookDto>, IMailHookService
    {
        #region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="membershipService"></param>
		/// <param name="eventService"></param>
		/// <param name="mailHookRepository"></param>
		public MailHookService(
			IMembershipService membershipService, 
			IEventService eventService,
			IMailHookRepository mailHookRepository) : base(membershipService, mailHookRepository)
		{
			eventService.EventFired += EventServiceOnEventFired;
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
				var mailHooksDynamicCollection = this.Query(query.ToString());
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

		#endregion
		
		#region Methods
		
		private async void SendHookMailAsync(MailHook mailHook, ErtisAuthEvent ertisAuthEvent)
		{
			if (mailHook.IsActive)
			{
				await Task.Run(() =>
				{
					try
					{
						
					}
					catch (Exception ex)
					{
						Console.WriteLine("Hook mail could not sent!");
						Console.WriteLine(ex);
					}
				});		
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
			
			if (string.IsNullOrEmpty(destination.MailTemplate))
			{
				destination.MailTemplate = source.MailTemplate;
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