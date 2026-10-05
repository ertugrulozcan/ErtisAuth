using System.Text.Json;
using System.Text.Json.Nodes;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using Ertis.Schema.Dynamics;
using Ertis.MongoDB.Serialization;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Events;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Webhooks;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Extensions;
using ErtisAuth.Dao.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Infrastructure.Services;

public class WebhookService : MembershipBoundedCrudService<Webhook>, IWebhookService
{
	#region Fields
	
	/// <summary>
	/// Event documents may contain ObjectIds (DynamicObjects, e.g. users, carry their own converter).
	/// </summary>
	private static readonly JsonSerializerOptions EventDataSerializerOptions = new()
	{
		Converters =
		{
			new ObjectIdConverter()
		}
	};
	
	/// <summary>
	/// The values written into the url are url-escaped (e.g. '../admin?x=1' can't change the path or the query)
	/// </summary>
	private static readonly Ertis.TemplateEngine.ParserOptions UrlParserOptions = new()
	{
		OpenBrackets = "{{",
		CloseBrackets = "}}",
		ValueEncoder = Uri.EscapeDataString
	};
	
	#endregion
	
	#region Services
	
	private readonly IEventService _eventService;
	private readonly IRestHandler _restHandler;
	private readonly IBackgroundQueue<WebhookCall> _webhookQueue;
	private readonly ILogger<WebhookService> _logger;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="membershipService"></param>
	/// <param name="eventService"></param>
	/// <param name="restHandler"></param>
	/// <param name="webhookQueue"></param>
	/// <param name="repository"></param>
	/// <param name="logger"></param>
	public WebhookService(
		IMembershipService membershipService, 
		IEventService eventService,
		IRestHandler restHandler,
		IBackgroundQueue<WebhookCall> webhookQueue,
		IWebhookRepository repository,
		ILogger<WebhookService> logger) : 
		base(membershipService, repository)
	{
		this._eventService = eventService;
		this._restHandler = restHandler;
		this._webhookQueue = webhookQueue;
		this._logger = logger;
		
		this._eventService.OnEventFired += this.OnEventFired;
		
		this.OnCreated += this.WebhookCreatedEventHandler;
		this.OnUpdated += this.WebhookUpdatedEventHandler;
		this.OnDeleted += this.WebhookDeletedEventHandler;
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
			
			// Typed query: no JSON round trip of the documents. The status is checked in memory,
			// its BSON serializer (NullableEnumMemberBsonSerializer) can not be translated to a LINQ filter
			var webhooks = await this._repository.FindAsync(
				x => x.MembershipId == membershipId && x.Event == eventType,
				sorting: null,
				cancellationToken: cancellationToken);
			
			foreach (var webhook in webhooks.Items.Where(x => x.IsActive))
			{
				this.QueueWebhook(webhook, ertisAuthEvent);
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
			await this._eventService.FireEventAsync(ErtisAuthEventType.WebhookCreated, eventArgs.Utilizer, eventArgs.MembershipId, eventArgs.Resource);
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
			await this._eventService.FireEventAsync(ErtisAuthEventType.WebhookUpdated, eventArgs.Utilizer, eventArgs.MembershipId, eventArgs.Updated, eventArgs.Prior);
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
			await this._eventService.FireEventAsync(ErtisAuthEventType.WebhookDeleted, eventArgs.Utilizer, eventArgs.MembershipId, null, eventArgs.Resource);
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "WebhookService.WebhookDeletedEventHandler occured an error");
		}
	}
	
	#endregion
	
	#region Methods
	
	private async Task<Webhook?> GetBySlugAsync(string slug, string membershipId)
	{
		return await this._repository.FindOneAsync(x => x.Slug == slug && x.MembershipId == membershipId);
	}
	
	/// <summary>
	/// Queues the webhook call; it is executed in the background by the webhook worker
	/// </summary>
	private void QueueWebhook(Webhook webhook, ErtisAuthEvent ertisAuthEvent)
	{
		try
		{
			// The event data is snapshotted now: the call is executed later, when the event's objects may have been changed
			var eventData = JsonSerializer.SerializeToNode(new { document = ertisAuthEvent.Document, prior = ertisAuthEvent.Prior }, EventDataSerializerOptions);
			if (!this._webhookQueue.TryEnqueue(new WebhookCall(webhook, ertisAuthEvent.UtilizerId, ertisAuthEvent.MembershipId, eventData)))
			{
				this._logger.LogError("Webhook {WebhookId} could not be queued: the webhook queue is full or closed", webhook.Id);
			}
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "Webhook {WebhookId} could not be queued", webhook.Id);
		}
	}
	
	public async Task ExecuteWebhookAsync(WebhookCall call, CancellationToken cancellationToken = default)
	{
		var webhook = call.Webhook;
		var utilizerId = call.UtilizerId;
		var membershipId = call.MembershipId;
		try
		{
			if (!webhook.IsActive || webhook.Request == null)
			{
				return;
			}
			
			var tryCount = webhook.TryCount > 0 ? webhook.TryCount : 1;
			
			// Event data is user-controlled: every template target gets context-specific escaping
			var dataNode = call.EventData;
			var data = ToTemplateData(dataNode);
			var formatter = new Ertis.TemplateEngine.Formatter();
			var urlFormatter = new Ertis.TemplateEngine.Formatter(UrlParserOptions);
			var httpMethod = new HttpMethod(webhook.Request.Method);
			var url = urlFormatter.Format(webhook.Request.Url, data);
			var headers = HeaderCollection.Create();
			if (webhook.Request.Headers != null)
			{
				foreach (var webhookRequestHeader in webhook.Request.Headers)
				{
					var headerValue = webhookRequestHeader.Value;
					if (!string.IsNullOrEmpty(webhookRequestHeader.Key) && !string.IsNullOrEmpty(headerValue))
					{
						var formattedHeaderValue = formatter.Format(headerValue, data);
						if (formattedHeaderValue.Contains('\r') || formattedHeaderValue.Contains('\n'))
						{
							// Header injection
							this._logger.LogWarning("Webhook {WebhookId}: the '{HeaderName}' header contains a line break and was not sent", webhook.Id, webhookRequestHeader.Key);
							continue;
						}
						
						headers = headers.Add(webhookRequestHeader.Key, formattedHeaderValue);
					}
				}
			}
			
			var webhookBody = FormatBody(webhook.Request.Body, formatter, data);
			
			// Built as a JSON tree: the rest handler serializes the body with the default options
			var payloadNode = webhookBody != null ? JsonNode.Parse(webhookBody.ToJson()) : null;
			IRequestBody body = webhook.Request.UncoveredBody ?
				new JsonRequestBody(payloadNode ?? new JsonObject()) :
				new JsonRequestBody(new JsonObject
				{
					["document"] = dataNode?["document"]?.DeepClone(),
					["prior"] = dataNode?["prior"]?.DeepClone(),
					["payload"] = payloadNode
				});
			
			var webhookRequest = new WebhookRequest
			{
				Method = webhook.Request.Method,
				Url = webhook.Request.Url,
				Headers = webhook.Request.Headers,
				Body = new DynamicObject(new {
					document = dataNode?["document"]?.DeepClone(),
					prior = dataNode?["prior"]?.DeepClone(),
					payload = webhookBody
				})
			};
			
			for (var i = 0; i < tryCount; i++)
			{
				// Only the request is guarded: a failure of recording the outcome must not count as a failed request (and resend it)
				WebhookExecutionResult webhookExecutionResult;
				try
				{
					var response = await this._restHandler.ExecuteRequestAsync(httpMethod, url, QueryString.Empty, headers, body, cancellationToken: CancellationToken.None);
					var statusCode = response.StatusCode != null ? (int)response.StatusCode : (int?)null;
					webhookExecutionResult = new WebhookExecutionResult
					{
						WebhookId = webhook.Id,
						IsSuccess = response.IsSuccess,
						StatusCode = statusCode,
						TryIndex = i + 1,
						Request = webhookRequest,
						Response = new WebhookResponse
						{
							IsSuccess = response.IsSuccess,
							StatusCode = statusCode,
							Body = response.Json
						}
					};
				}
				catch (Exception ex)
				{
					webhookExecutionResult = new WebhookExecutionResult
					{
						WebhookId = webhook.Id,
						IsSuccess = false,
						StatusCode = 500,
						TryIndex = i + 1,
						Exception = WebhookExecutionError.FromException(ex),
						Request = webhookRequest
					};
				}
				
				var eventType = webhookExecutionResult.IsSuccess ? ErtisAuthEventType.WebhookRequestSent : ErtisAuthEventType.WebhookRequestFailed;
				await this._eventService.FireEventAsync(eventType, utilizerId, membershipId, webhookExecutionResult, cancellationToken: cancellationToken);
				if (webhookExecutionResult.IsSuccess)
				{
					break;
				}
			}
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "WebhookService.ExecuteWebhookAsync occured an error");
		}
	}
	
	/// <summary>
	/// The body template is JSON, so its placeholders are always inside string values: each string value is formatted
	/// separately and stays a string value, the data can't change the structure of the body (JSON injection).
	/// </summary>
	private static DynamicObject? FormatBody(DynamicObject? body, Ertis.TemplateEngine.Formatter formatter, object data)
	{
		if (body == null)
		{
			return null;
		}
		
		var formatted = MapStringValues(JsonNode.Parse(body.ToJson()), x => formatter.Format(x, data));
		return formatted != null ? DynamicObject.Parse(formatted.ToJsonString()) : null;
	}
	
	private static object ToTemplateData(JsonNode? dataNode)
	{
		return DynamicObject.Parse(dataNode?.ToJsonString() ?? "{}").ToDynamic();
	}
	
	/// <summary>
	/// Replaces every string value of the JSON tree (object keys are kept).
	/// </summary>
	private static JsonNode? MapStringValues(JsonNode? node, Func<string, string> map)
	{
		switch (node)
		{
			case JsonObject jsonObject:
				foreach (var property in jsonObject.ToArray())
				{
					jsonObject[property.Key] = MapStringValues(property.Value?.DeepClone(), map);
				}
				
				return jsonObject;
			case JsonArray jsonArray:
				for (var i = 0; i < jsonArray.Count; i++)
				{
					jsonArray[i] = MapStringValues(jsonArray[i]?.DeepClone(), map);
				}
				
				return jsonArray;
			case JsonValue jsonValue when jsonValue.TryGetValue<string>(out var text):
				return JsonValue.Create(map(text));
			default:
				return node;
		}
	}
	
	protected override Task<IEnumerable<string>> ValidateModelAsync(Webhook model, CancellationToken cancellationToken = default)
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
			else if (!httpMethodList.Any(x => string.Equals(x, model.Request.Method, StringComparison.OrdinalIgnoreCase)))
			{
				errorList.Add($"Unknown http method in webhook request. (Supported methods: [{string.Join(", ", httpMethodList)}])");
			}
		}
		
		return Task.FromResult<IEnumerable<string>>(errorList);
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
	
	protected override async Task<bool> IsAlreadyExistAsync(Webhook model, string membershipId, Webhook? exclude = null, CancellationToken cancellationToken = default)
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
	
	protected override ErtisAuthException GetAlreadyExistError(Webhook model)
	{
		return ErtisAuthException.WebhookAlreadyExists(model.Slug);
	}
	
	protected override ErtisAuthException GetNotFoundError(string id)
	{
		return ErtisAuthException.WebhookNotFound(id);
	}
	
	#endregion
}