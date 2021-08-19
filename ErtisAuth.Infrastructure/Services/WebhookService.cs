using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.MongoDB.Queries;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Webhooks;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Webhooks;
using ErtisAuth.Events.EventArgs;
using ErtisAuth.Infrastructure.Mapping;
using Newtonsoft.Json;

namespace ErtisAuth.Infrastructure.Services
{
	public class WebhookService : MembershipBoundedCrudService<Webhook, WebhookDto>, IWebhookService
	{
		#region Services

		private readonly IEventService eventService;
		private readonly IRestHandler restHandler;

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="membershipService"></param>
		/// <param name="eventService"></param>
		/// <param name="restHandler"></param>
		/// <param name="webhookRepository"></param>
		public WebhookService(
			IMembershipService membershipService, 
			IEventService eventService,
			IRestHandler restHandler,
			IWebhookRepository webhookRepository) : base(membershipService, webhookRepository)
		{
			this.eventService = eventService;
			this.restHandler = restHandler;
			
			this.eventService.EventFired += EventServiceOnEventFired;
			
			this.OnCreated += this.WebhookCreatedEventHandler;
			this.OnUpdated += this.WebhookUpdatedEventHandler;
			this.OnDeleted += this.WebhookDeletedEventHandler;
		}

		#endregion

		#region Event Handlers
		
		private void EventServiceOnEventFired(object sender, ErtisAuthEvent ertisAuthEvent)
		{
			try
			{
				var query = QueryBuilder.Where(new QueryGroup
				{
					new Query("status", "active"),
					new Query("membership_id", ertisAuthEvent.MembershipId),
					new Query("event", ertisAuthEvent.EventType.ToString())
				});
			
				var webhooksDynamicCollection = this.Query(query.Value.ToString());
				var webhooks = JsonConvert.DeserializeObject<PaginationCollection<Webhook>>(JsonConvert.SerializeObject(webhooksDynamicCollection));
				foreach (var webhook in webhooks.Items)
				{
					this.ExecuteWebhookAsync(webhook, ertisAuthEvent.UtilizerId, ertisAuthEvent.MembershipId, ertisAuthEvent.Document, ertisAuthEvent.Prior);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}

		private void WebhookCreatedEventHandler(object sender, CreateResourceEventArgs<Webhook> eventArgs)
		{
			this.eventService.FireEventAsync(this, new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.WebhookCreated,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Resource,
				MembershipId = eventArgs.Utilizer.MembershipId
			});
		}
		
		private void WebhookUpdatedEventHandler(object sender, UpdateResourceEventArgs<Webhook> eventArgs)
		{
			this.eventService.FireEventAsync(this, new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.WebhookUpdated,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Updated,
				Prior = eventArgs.Prior,
				MembershipId = eventArgs.Utilizer.MembershipId
			});
		}
		
		private void WebhookDeletedEventHandler(object sender, DeleteResourceEventArgs<Webhook> eventArgs)
		{
			this.eventService.FireEventAsync(this, new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.WebhookDeleted,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Resource,
				MembershipId = eventArgs.Utilizer.MembershipId
			});
		}

		#endregion
		
		#region Methods
		
		private async void ExecuteWebhookAsync(Webhook webhook, string utilizerId, string membershipId, object document, object prior)
		{
			await Task.Run(() =>
			{
				if (webhook.IsActive)
				{
					int tryCount = webhook.TryCount > 0 ? webhook.TryCount : 1;
				
					foreach (var webhookRequest in webhook.RequestList)
					{
						this.ExecuteWebhookRequestAsync(webhookRequest, utilizerId, membershipId, document, prior, tryCount);
					}	
				}
			});
		}
		
		private async void ExecuteWebhookRequestAsync(WebhookRequest webhookRequest, string utilizerId, string membershipId, object document, object prior, int tryCount)
		{
			var httpMethod = new HttpMethod(webhookRequest.Method);
			var url = webhookRequest.Url;
			var headers = HeaderCollection.Empty;
			if (webhookRequest.Headers != null)
			{
				foreach (var webhookRequestHeader in webhookRequest.Headers)
				{
					headers = headers.Add(webhookRequestHeader);
				}
			}

			var body = new JsonRequestBody(new
			{
				document,
				prior,
				payload = webhookRequest.Body
			});

			for (int i = 0; i < tryCount; i++)
			{
				var response = await this.restHandler.ExecuteRequestAsync(httpMethod, url, QueryString.Empty, headers, body);
				if (response.IsSuccess)
				{
					await this.eventService.FireEventAsync(this, new ErtisAuthEvent(
						ErtisAuthEventType.WebhookRequestSent,
						utilizerId,
						membershipId,
						response
					));
					
					break;
				}
				else
				{
					await this.eventService.FireEventAsync(this, new ErtisAuthEvent(
						ErtisAuthEventType.WebhookRequestFailed,
						utilizerId,
						membershipId,
						response
					));
				}	
			}
		}

		protected override bool ValidateModel(Webhook model, out IEnumerable<string> errors)
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

			if (model.TryCount < 1 || model.TryCount > 5)
			{
				errorList.Add("try_count is a required field (must be in 1..5 range)");
			}

			if (model.RequestList == null || !model.RequestList.Any())
			{
				errorList.Add("requests is a required field");
			}
			else
			{
				foreach (var webhookRequest in model.RequestList)
				{
					if (string.IsNullOrEmpty(webhookRequest.Url))
					{
						errorList.Add("url is a required field for the webhook request");
					}
					
					var httpMethodList = typeof(HttpMethod).GetProperties().Where(x => x.PropertyType == typeof(HttpMethod)).Select(x => x.Name).ToList();
					if (string.IsNullOrEmpty(webhookRequest.Method))
					{
						errorList.Add("method is a required field for the webhook request");
					}
					else if (!httpMethodList.Any(x => string.Equals(x, webhookRequest.Method, StringComparison.CurrentCultureIgnoreCase)))
					{
						errorList.Add($"Unknown http method in webhook request. (Supported methods: [{string.Join(", ", httpMethodList)}])");
					}
				}
			}
			
			errors = errorList;
			return !errors.Any();
		}

		protected override void Overwrite(Webhook destination, Webhook source)
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
			
			if (destination.RequestList == null)
			{
				destination.RequestList = source.RequestList;
			}
			
			if (destination.TryCount == 0)
			{
				destination.TryCount = source.TryCount;
			}
		}
		
		protected override bool IsAlreadyExist(Webhook model, string membershipId, Webhook exclude = default)
		{
			if (exclude == null)
			{
				return this.GetWebhookByName(model.Name, membershipId) != null;	
			}
			else
			{
				var current = this.GetWebhookByName(model.Name, membershipId);
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

		protected override async Task<bool> IsAlreadyExistAsync(Webhook model, string membershipId, Webhook exclude = default)
		{
			if (exclude == null)
			{
				return await this.GetWebhookByNameAsync(model.Name, membershipId) != null;	
			}
			else
			{
				var current = await this.GetWebhookByNameAsync(model.Name, membershipId);
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
		
		protected override ErtisAuthException GetAlreadyExistError(Webhook model)
		{
			return ErtisAuthException.WebhookWithSameNameAlreadyExists(model.Name);
		}
		
		protected override ErtisAuthException GetNotFoundError(string id)
		{
			return ErtisAuthException.WebhookNotFound(id);
		}
		
		public Webhook GetWebhookByName(string name, string membershipId)
		{
			var dto = this.repository.FindOne(x => x.Name == name && x.MembershipId == membershipId);
			if (dto == null)
			{
				return null;
			}
			
			return Mapper.Current.Map<WebhookDto, Webhook>(dto);
		}
		
		public async Task<Webhook> GetWebhookByNameAsync(string name, string membershipId)
		{
			var dto = await this.repository.FindOneAsync(x => x.Name == name && x.MembershipId == membershipId);
			if (dto == null)
			{
				return null;
			}
			
			return Mapper.Current.Map<WebhookDto, Webhook>(dto);
		}
		
		#endregion
	}
}