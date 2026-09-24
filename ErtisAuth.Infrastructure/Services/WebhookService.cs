using System.Text.Json;
using Ertis.Core.Collections;
using Ertis.MongoDB.Queries;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Events;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Webhooks;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Dao.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Infrastructure.Services;

public class WebhookService : MembershipBoundedCrudService<Webhook>, IWebhookService
{
	#region Services
	
	private readonly IEventService _eventService;
	private readonly ISystemRestHandler _restHandler;
	private readonly ILogger<WebhookService> _logger;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="membershipService"></param>
	/// <param name="eventService"></param>
	/// <param name="restHandler"></param>
	/// <param name="repository"></param>
	/// <param name="logger"></param>
	public WebhookService(
		IMembershipService membershipService, 
		IEventService eventService,
		ISystemRestHandler restHandler,
		IWebhookRepository repository,
		ILogger<WebhookService> logger) : 
		base(membershipService, repository)
	{
		this._eventService = eventService;
		this._restHandler = restHandler;
		this._logger = logger;
		
		this._eventService.EventFired += EventServiceOnEventFired;
		
		this.OnCreated += this.WebhookCreatedEventHandler;
		this.OnUpdated += this.WebhookUpdatedEventHandler;
		this.OnDeleted += this.WebhookDeletedEventHandler;
	}
	
	#endregion
	
	#region Event Handlers
	
	private void EventServiceOnEventFired(object? sender, ErtisAuthEvent ertisAuthEvent)
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
			var json = JsonSerializer.Serialize(webhooksDynamicCollection);
			var webhooks = JsonSerializer.Deserialize<PaginationCollection<Webhook>>(json);
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
			this._logger.LogError(ex, "WebhookService.OnEventFired occured an error");
		}
	}
	
	private async void WebhookCreatedEventHandler(object? sender, CreateResourceEventArgs<Webhook> eventArgs)
	{
		try
		{
			await this._eventService.FireEventAsync(this, new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.WebhookCreated,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Resource,
				MembershipId = eventArgs.MembershipId ?? eventArgs.Utilizer.MembershipId ?? string.Empty
			});
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "WebhookService.WebhookCreatedEventHandler occured an error");
		}
	}
	
	private async void WebhookUpdatedEventHandler(object? sender, UpdateResourceEventArgs<Webhook> eventArgs)
	{
		try
		{
			await this._eventService.FireEventAsync(this, new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.WebhookUpdated,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Updated,
				Prior = eventArgs.Prior,
				MembershipId = eventArgs.MembershipId ?? eventArgs.Utilizer.MembershipId ?? string.Empty
			});
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "WebhookService.WebhookUpdatedEventHandler occured an error");
		}
	}
	
	private async void WebhookDeletedEventHandler(object? sender, DeleteResourceEventArgs<Webhook> eventArgs)
	{
		try
		{
			await this._eventService.FireEventAsync(this, new ErtisAuthEvent
			{
				EventType = ErtisAuthEventType.WebhookDeleted,
				UtilizerId = eventArgs.Utilizer.Id,
				Document = eventArgs.Resource,
				MembershipId = eventArgs.MembershipId ?? eventArgs.Utilizer.MembershipId ?? string.Empty
			});
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "WebhookService.WebhookDeletedEventHandler occured an error");
		}
	}
	
	#endregion
	
	#region Methods
	
	private async void ExecuteWebhookAsync(Webhook webhook, string utilizerId, string membershipId, object document, object prior)
	{
		try
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
						this._logger.LogError(ex, "Webhook execution occured an exception");
					}
				});
			}
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "WebhookService.ExecuteWebhookAsync occured an error");
		}
	}
	
	private async void ExecuteWebhookRequestAsync(Webhook webhook, string utilizerId, string membershipId, object document, object prior, int tryCount)
	{
		try
		{
			if (webhook.Request == null)
			{
				return;
			}
			
			var httpMethod = new HttpMethod(webhook.Request.Method);
			var url = webhook.Request.Url;
			var headers = HeaderCollection.Create();
			if (webhook.Request.Headers != null)
			{
				foreach (var webhookRequestHeader in webhook.Request.Headers)
				{
					if (!string.IsNullOrEmpty(webhookRequestHeader.Key) && webhookRequestHeader.Value != null &&
						!string.IsNullOrEmpty(webhookRequestHeader.Value.ToString()))
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
				Body = new
				{
					document,
					prior,
					payload = webhook.Request.Body
				}
			};
			
			for (var i = 0; i < tryCount; i++)
			{
				try
				{
					var response = await this._restHandler.ExecuteRequestAsync(httpMethod, url, QueryString.Empty, headers, body);
					var webhookExecutionResult = new WebhookExecutionResult
					{
						WebhookId = webhook.Id,
						IsSuccess = response.IsSuccess,
						StatusCode = response.StatusCode != null ? (int)response.StatusCode : null,
						TryIndex = i + 1,
						Exception = null,
						Request = webhookRequest,
						Response = response
					};
					
					if (response.IsSuccess)
					{
						var e = new ErtisAuthEvent
						{
							EventType = ErtisAuthEventType.WebhookRequestSent,
							UtilizerId = utilizerId,
							MembershipId = membershipId,
							Document = webhookExecutionResult
						};
						
						await this._eventService.FireEventAsync(this, e);
						break;
					}
					else
					{
						var e = new ErtisAuthEvent
						{
							EventType = ErtisAuthEventType.WebhookRequestFailed,
							UtilizerId = utilizerId,
							MembershipId = membershipId,
							Document = webhookExecutionResult
						};
						
						await this._eventService.FireEventAsync(this, e);
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
						Request = webhookRequest
					};
					
					var e = new ErtisAuthEvent
					{
						EventType = ErtisAuthEventType.WebhookRequestFailed,
						UtilizerId = utilizerId,
						MembershipId = membershipId,
						Document = webhookExecutionResult
					};
					
					await this._eventService.FireEventAsync(this, e);
				}
			}
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "WebhookService.ExecuteWebhookRequestAsync occured an error");
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
		
		destination.Status ??= source.Status;
		destination.Request ??= source.Request;
		
		if (destination.TryCount == 0)
		{
			destination.TryCount = source.TryCount;
		}
	}
	
	protected override bool IsAlreadyExist(Webhook model, string membershipId, Webhook? exclude = null)
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
	
	protected override async Task<bool> IsAlreadyExistAsync(Webhook model, string membershipId, Webhook? exclude = null, CancellationToken cancellationToken = default)
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
	
	private Webhook? GetWebhookByName(string name, string membershipId)
	{
		return this._repository.FindOne(x => x.Name == name && x.MembershipId == membershipId);
	}
	
	private async Task<Webhook?> GetWebhookByNameAsync(string name, string membershipId)
	{
		return await this._repository.FindOneAsync(x => x.Name == name && x.MembershipId == membershipId);
	}
	
	#endregion
}