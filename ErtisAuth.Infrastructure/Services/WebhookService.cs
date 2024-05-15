using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.MongoDB.Queries;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Abstractions.Services;
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
				IQuery[] expressions = 
				{
					QueryBuilder.Equals("status", "active"),
					QueryBuilder.Equals("membership_id", ertisAuthEvent.MembershipId),
					QueryBuilder.Equals("event", ertisAuthEvent.EventType.ToString())
				};
				
				var query = QueryBuilder.Where(expressions);
				var webhooksDynamicCollection = this.Query(ertisAuthEvent.MembershipId, query.ToString());
				var webhooks = JsonConvert.DeserializeObject<PaginationCollection<Webhook>>(JsonConvert.SerializeObject(webhooksDynamicCollection));
				if (webhooks != null)
				{
					foreach (var webhook in webhooks.Items)
					{
						this.ExecuteWebhookAsync(webhook, ertisAuthEvent.UtilizerId, ertisAuthEvent.MembershipId, ertisAuthEvent.Document, ertisAuthEvent.Prior);
					}	
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}

		private async void WebhookCreatedEventHandler(object sender, CreateResourceEventArgs<Webhook> eventArgs)
		{
			await this.eventService.FireEventAsync(this, new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.WebhookCreated,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Resource,
				MembershipId = eventArgs.MembershipId
			});
		}
		
		private async void WebhookUpdatedEventHandler(object sender, UpdateResourceEventArgs<Webhook> eventArgs)
		{
			await this.eventService.FireEventAsync(this, new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.WebhookUpdated,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Updated,
				Prior = eventArgs.Prior,
				MembershipId = eventArgs.MembershipId
			});
		}
		
		private async void WebhookDeletedEventHandler(object sender, DeleteResourceEventArgs<Webhook> eventArgs)
		{
			await this.eventService.FireEventAsync(this, new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.WebhookDeleted,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Resource,
				MembershipId = eventArgs.MembershipId
			});
		}

		#endregion
		
		#region Methods
		
		private async void ExecuteWebhookAsync(Webhook webhook, string utilizerId, string membershipId, object document, object prior)
		{
			if (webhook.IsActive)
			{
				await Task.Run(() =>
				{
					var tryCount = webhook.TryCount > 0 ? webhook.TryCount : 1;
					try
					{
						this.ExecuteWebhookRequestAsync(webhook, utilizerId, membershipId, document, prior, tryCount);
					}
					catch (Exception ex)
					{
						Console.WriteLine("Webhook execution occured an exception");
						Console.WriteLine(ex);
					}
				});
			}
		}
		
		private async void ExecuteWebhookRequestAsync(Webhook webhook, string utilizerId, string membershipId, object document, object prior, int tryCount)
		{
			var httpMethod = new HttpMethod(webhook.Request.Method);
			var url = webhook.Request.Url;
			var headers = HeaderCollection.Empty;
			if (webhook.Request.Headers != null)
			{
				foreach (var webhookRequestHeader in webhook.Request.Headers)
				{
					if (!string.IsNullOrEmpty(webhookRequestHeader.Key) && webhookRequestHeader.Value != null && !string.IsNullOrEmpty(webhookRequestHeader.Value.ToString()))
					{
						headers = headers.Add(webhookRequestHeader);	
					}
				}
			}

			IRequestBody body = new JsonRequestBody(new
			{
				document,
				prior,
				payload = webhook.Request.Body
			});

			var webhookRequest = new WebhookRequest
			{
				Method = webhook.Request.Method,
				Url = webhook.Request.Url,
				Headers = webhook.Request.Headers,
				Body = new {
					document,
					prior,
					payload = webhook.Request.Body
				},
			};

			for (var i = 0; i < tryCount; i++)
			{
				try
				{
					var response = await this.restHandler.ExecuteRequestAsync(httpMethod, url, QueryString.Empty, headers, body);
					var webhookExecutionResult = new WebhookExecutionResult
					{
						WebhookId = webhook.Id,
						IsSuccess = response.IsSuccess,
						StatusCode = response.StatusCode != null ? (int) response.StatusCode : null,
						TryIndex = i + 1,
						Exception = null,
						Request = webhookRequest,
						Response = response
					};
					
					if (response.IsSuccess)
					{
						await this.eventService.FireEventAsync(this, new ErtisAuthEvent(
							ErtisAuthEventType.WebhookRequestSent,
							utilizerId,
							membershipId,
							webhookExecutionResult
						));
					
						break;
					}
					else
					{
						await this.eventService.FireEventAsync(this, new ErtisAuthEvent(
							ErtisAuthEventType.WebhookRequestFailed,
							utilizerId,
							membershipId,
							webhookExecutionResult
						));
					}
				}
				catch (Exception ex)
				{
					var webhookExecutionResult = new WebhookExecutionResult
					{
						WebhookId = webhook.Id,
						IsSuccess = false,
						StatusCode = 500,
						TryIndex = i + 1,
						Exception = ex,
						Request = webhookRequest,
						Response = null
					};
					
					await this.eventService.FireEventAsync(this, new ErtisAuthEvent(
						ErtisAuthEventType.WebhookRequestFailed,
						utilizerId,
						membershipId,
						webhookExecutionResult
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

			if (model.Status == null)
			{
				errorList.Add("status is a required field");
			}
			
			if (string.IsNullOrEmpty(model.Event))
			{
				errorList.Add("event is a required field");
			}
			else if (model.EventType == null)
			{
				errorList.Add($"Unknown event type. (Supported events: [{string.Join(", ", Enum.GetNames(typeof(ErtisAuthEventType)))}])");
			}

			if (model.TryCount is < 1 or > 5)
			{
				errorList.Add("try_count is a required field (must be in 1..5 range)");
			}

			if (model.Request == null)
			{
				errorList.Add("request is a required field");
			}
			else
			{
				if (string.IsNullOrEmpty(model.Request.Url))
				{
					errorList.Add("url is a required field for the webhook request");
				}
					
				var httpMethodList = typeof(HttpMethod).GetProperties().Where(x => x.PropertyType == typeof(HttpMethod)).Select(x => x.Name).ToList();
				if (string.IsNullOrEmpty(model.Request.Method))
				{
					errorList.Add("method is a required field for the webhook request");
				}
				else if (!httpMethodList.Any(x => string.Equals(x, model.Request.Method, StringComparison.CurrentCultureIgnoreCase)))
				{
					errorList.Add($"Unknown http method in webhook request. (Supported methods: [{string.Join(", ", httpMethodList)}])");
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
			
			if (destination.Status == null)
			{
				destination.Status = source.Status;
			}
			
			// ReSharper disable once ConvertIfStatementToNullCoalescingAssignment
			if (destination.Request == null)
			{
				destination.Request = source.Request;
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
		
		private Webhook GetWebhookByName(string name, string membershipId)
		{
			var dto = this.repository.FindOne(x => x.Name == name && x.MembershipId == membershipId);
			return dto == null ? null : Mapper.Current.Map<WebhookDto, Webhook>(dto);
		}
		
		private async Task<Webhook> GetWebhookByNameAsync(string name, string membershipId)
		{
			var dto = await this.repository.FindOneAsync(x => x.Name == name && x.MembershipId == membershipId);
			return dto == null ? null : Mapper.Current.Map<WebhookDto, Webhook>(dto);
		}
		
		#endregion
	}
}