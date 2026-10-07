using System.Net;
using System.Text.Json.Nodes;
using Ertis.Core.Exceptions;
using Ertis.Core.Models;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using Ertis.Schema.Dynamics;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Webhooks;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using NSubstitute;

namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// Webhooks: CRUD rules, which webhooks an event triggers, and the request sent. Event data (e.g. a user's own name)
/// is user-controlled, so it must not break or change the templated URL, headers and body.
/// </summary>
public class WebhookServiceTests
{
	#region Constants
	
	private const string MembershipId = "5f8a1b2c3d4e5f6a7b8c9d00";
	private const string UtilizerId = "5f8a1b2c3d4e5f6a7b8c9d01";
	
	#endregion
	
	#region Fields
	
	private readonly IMembershipService _membershipService = Substitute.For<IMembershipService>();
	
	private readonly IEventService _eventService = Substitute.For<IEventService>();
	
	private readonly IRestHandler _restHandler = Substitute.For<IRestHandler>();
	
	private readonly IWebhookRepository _repository = Substitute.For<IWebhookRepository>();
	
	private readonly BackgroundQueue<WebhookCall> _webhookQueue = new();
	
	private readonly List<Webhook> _webhooks;
	
	private readonly List<SentRequest> _sentRequests = [];
	
	private readonly List<ErtisAuthEventType> _firedEvents = [];
	
	private HttpStatusCode _responseStatusCode = HttpStatusCode.OK;
	
	#endregion
	
	#region Constructors
	
	public WebhookServiceTests()
	{
		// ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
		this._webhooks = InMemoryRepository.Setup(this._repository, x => x.Id ??= ObjectId.GenerateNewId().ToString());
		
		var membership = TestServiceFactory.CreateMembership();
		membership.Id = MembershipId;
		this._membershipService.GetAsync(MembershipId, Arg.Any<CancellationToken>()).Returns(membership);
		
		this._restHandler
			.ExecuteRequestAsync(Arg.Any<HttpMethod>(), Arg.Any<string>(), Arg.Any<IQueryString>(), Arg.Any<IHeaderCollection>(), Arg.Any<IRequestBody>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				lock (this._sentRequests)
				{
					this._sentRequests.Add(new SentRequest(
						callInfo.ArgAt<HttpMethod>(0),
						callInfo.ArgAt<string>(1),
						callInfo.ArgAt<IHeaderCollection>(3).ToDictionary().ToDictionary(x => x.Key, x => x.Value.ToString()),
						JsonNode.Parse(((JsonRequestBody) callInfo.ArgAt<IRequestBody>(4)).Json ?? "null")));
				}
				
				return new ResponseResult(this._responseStatusCode);
			});
		
		this._eventService
			.FireEventAsync(Arg.Any<ErtisAuthEventType>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<object?>(), Arg.Any<object?>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				lock (this._firedEvents)
				{
					this._firedEvents.Add(callInfo.ArgAt<ErtisAuthEventType>(0));
				}
				
				return new ErtisAuthEvent { EventType = callInfo.ArgAt<ErtisAuthEventType>(0), UtilizerId = UtilizerId, MembershipId = MembershipId };
			});
	}
	
	#endregion
	
	#region Helpers
	
	private WebhookService CreateService()
	{
		return new WebhookService(this._membershipService, this._eventService, this._restHandler, this._webhookQueue, this._repository, NullLogger<WebhookService>.Instance);
	}
	
	private static Webhook CreateWebhook(
		string name = "User created hook",
		string url = "https://hooks.example.com/users",
		Dictionary<string, string>? headers = null,
		string? bodyJson = null,
		bool uncoveredBody = false,
		int tryCount = 1,
		WebhookStatus status = WebhookStatus.Active)
	{
		return new Webhook
		{
			Name = name,
			Event = nameof(ErtisAuthEventType.UserCreated),
			Status = status,
			TryCount = tryCount,
			MembershipId = MembershipId,
			Request = new WebhookRequest
			{
				Method = "POST",
				Url = url,
				Headers = headers,
				Body = bodyJson != null ? DynamicObject.Parse(bodyJson) : null,
				UncoveredBody = uncoveredBody
			}
		};
	}
	
	private void AddWebhook(Webhook webhook)
	{
		// ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
		webhook.Id ??= ObjectId.GenerateNewId().ToString();
		this._webhooks.Add(webhook);
	}
	
	private void FireUserCreated(object document)
	{
		this._eventService.OnEventFired += Raise.Event<EventHandler<ErtisAuthEvent>>(this._eventService, new ErtisAuthEvent
		{
			EventType = ErtisAuthEventType.UserCreated,
			UtilizerId = UtilizerId,
			MembershipId = MembershipId,
			Document = document
		});
	}
	
	private async Task WaitForQueuedWebhooksAsync(int count)
	{
		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (this._webhookQueue.Count < count && DateTime.UtcNow < deadline)
		{
			await Task.Delay(20, TestContext.Current.CancellationToken);
		}
	}
	
	/// <summary>
	/// The event handler queues the webhook calls in the background: waits for the queued calls, then executes them like the webhook worker does.
	/// </summary>
	private async Task ExecuteQueuedWebhooksAsync(WebhookService service, int expectedCount)
	{
		await this.WaitForQueuedWebhooksAsync(expectedCount);
		this._webhookQueue.Complete();
		await foreach (var call in this._webhookQueue.ReadAllAsync(TestContext.Current.CancellationToken))
		{
			await service.ExecuteWebhookAsync(call, TestContext.Current.CancellationToken);
		}
	}
	
	private List<ErtisAuthEventType> OutcomeEvents()
	{
		lock (this._firedEvents)
		{
			return this._firedEvents.Where(x => x is ErtisAuthEventType.WebhookRequestSent or ErtisAuthEventType.WebhookRequestFailed).ToList();
		}
	}
	
	private async Task<SentRequest> SendSingleRequestAsync(Webhook webhook, object document)
	{
		this.AddWebhook(webhook);
		var service = this.CreateService();
		this.FireUserCreated(document);
		await this.ExecuteQueuedWebhooksAsync(service, 1);
		return Assert.Single(this._sentRequests);
	}
	
	private sealed record SentRequest(HttpMethod Method, string Url, Dictionary<string, string?> Headers, JsonNode? Body);
	
	#endregion
	
	#region Create & Update
	
	[Fact]
	public async Task CreateAsync_WithValidWebhook_InsertsAndFiresWebhookCreated()
	{
		var created = await this.CreateService().CreateAsync(CreateWebhook(), MembershipId, Utilizer.GetSystemUtilizer(MembershipId), TestContext.Current.CancellationToken);
		
		Assert.Equal(created.Id, Assert.Single(this._webhooks).Id);
		await this._eventService.Received(1).FireEventAsync(ErtisAuthEventType.WebhookCreated, Arg.Any<Utilizer>(), MembershipId, Arg.Any<object?>(), Arg.Any<object?>(), Arg.Any<CancellationToken>());
	}
	
	[Theory]
	[InlineData("UnknownEvent", 1, "POST", "Unknown event type.")]
	[InlineData("UserCreated", 0, "POST", "Try count is required (must be in 1..5 range)")]
	[InlineData("UserCreated", 6, "POST", "Try count is required (must be in 1..5 range)")]
	[InlineData("UserCreated", 1, "FETCH", "Unknown http method in webhook request.")]
	public async Task CreateAsync_WithInvalidWebhook_ThrowsValidationError(string eventName, int tryCount, string method, string expectedErrorStart)
	{
		var webhook = CreateWebhook(tryCount: tryCount);
		webhook.Event = eventName;
		webhook.Request!.Method = method;
		
		var exception = await Assert.ThrowsAsync<ValidationException>(() => this.CreateService().CreateAsync(webhook, MembershipId, Utilizer.GetSystemUtilizer(MembershipId), TestContext.Current.CancellationToken));
		
		Assert.Contains(exception.Errors!, x => x.StartsWith(expectedErrorStart));
		Assert.Empty(this._webhooks);
	}
	
	[Fact]
	public async Task CreateAsync_WithoutStatus_ThrowsValidationError()
	{
		var webhook = CreateWebhook();
		webhook.Status = null;
		
		var exception = await Assert.ThrowsAsync<ValidationException>(() => this.CreateService().CreateAsync(webhook, MembershipId, Utilizer.GetSystemUtilizer(MembershipId), TestContext.Current.CancellationToken));
		
		Assert.Contains("Status is required", exception.Errors!);
	}
	
	[Fact]
	public async Task CreateAsync_WithExistingName_ThrowsWebhookWithSameNameAlreadyExists()
	{
		var service = this.CreateService();
		await service.CreateAsync(CreateWebhook(), MembershipId, Utilizer.GetSystemUtilizer(MembershipId), TestContext.Current.CancellationToken);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => service.CreateAsync(CreateWebhook(), MembershipId, Utilizer.GetSystemUtilizer(MembershipId), TestContext.Current.CancellationToken));
		
		Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
		Assert.Single(this._webhooks);
	}
	
	[Fact]
	public async Task UpdateAsync_RenamingToAnExistingName_Throws()
	{
		var service = this.CreateService();
		await service.CreateAsync(CreateWebhook(name: "First"), MembershipId, Utilizer.GetSystemUtilizer(MembershipId), TestContext.Current.CancellationToken);
		var second = await service.CreateAsync(CreateWebhook(name: "Second"), MembershipId, Utilizer.GetSystemUtilizer(MembershipId), TestContext.Current.CancellationToken);
		
		var update = CreateWebhook(name: "First");
		update.Id = second.Id;
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => service.UpdateAsync(update, MembershipId, Utilizer.GetSystemUtilizer(MembershipId), TestContext.Current.CancellationToken));
		
		Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
	}
	
	[Fact]
	public async Task UpdateAsync_KeepsOmittedFields()
	{
		var service = this.CreateService();
		var created = await service.CreateAsync(CreateWebhook(tryCount: 3), MembershipId, Utilizer.GetSystemUtilizer(MembershipId), TestContext.Current.CancellationToken);
		var update = new Webhook
		{
			Id = created.Id,
			Name = created.Name,
			Event = created.Event,
			Description = "Updated description",
			MembershipId = MembershipId
		};
		
		var updated = await service.UpdateAsync(update, MembershipId, Utilizer.GetSystemUtilizer(MembershipId), TestContext.Current.CancellationToken);
		
		Assert.Equal("Updated description", updated.Description);
		Assert.Equal(WebhookStatus.Active, updated.Status);
		Assert.Equal(3, updated.TryCount);
		Assert.Equal("https://hooks.example.com/users", updated.Request?.Url);
	}
	
	#endregion
	
	#region Triggering
	
	[Fact]
	public async Task OnEventFired_ExecutesOnlyTheActiveWebhooksOfTheEventInTheMembership()
	{
		this.AddWebhook(CreateWebhook(url: "https://hooks.example.com/matching"));
		
		var otherEvent = CreateWebhook(url: "https://hooks.example.com/other-event");
		otherEvent.Event = nameof(ErtisAuthEventType.UserDeleted);
		this.AddWebhook(otherEvent);
		
		var otherMembership = CreateWebhook(url: "https://hooks.example.com/other-membership");
		otherMembership.MembershipId = "other-membership";
		this.AddWebhook(otherMembership);
		
		this.AddWebhook(CreateWebhook(url: "https://hooks.example.com/passive", status: WebhookStatus.Passive));
		var service = this.CreateService();
		
		this.FireUserCreated(new { username = "john.doe" });
		
		// Gives the other webhooks the time to be (wrongly) queued
		await Task.Delay(300, TestContext.Current.CancellationToken);
		await this.ExecuteQueuedWebhooksAsync(service, 1);
		
		Assert.Equal("https://hooks.example.com/matching", Assert.Single(this._sentRequests).Url);
	}
	
	/// <summary>
	/// Event documents are DynamicObjects (e.g. users): regression guard, they were sent as '{}'.
	/// </summary>
	[Fact]
	public async Task OnEventFired_WithDynamicObjectDocument_SendsAndTemplatesItsFields()
	{
		this.AddWebhook(CreateWebhook(url: "https://hooks.example.com/{{document.username}}"));
		var service = this.CreateService();
		
		this.FireUserCreated(DynamicObject.Parse("""{ "_id": "user-1", "username": "john.doe" }"""));
		await this.ExecuteQueuedWebhooksAsync(service, 1);
		
		var request = Assert.Single(this._sentRequests);
		Assert.Equal("https://hooks.example.com/john.doe", request.Url);
		Assert.Equal("john.doe", request.Body?["document"]?["username"]?.GetValue<string>());
	}
	
	[Fact]
	public async Task OnEventFired_DoesNotExecutePassiveWebhooks()
	{
		this.AddWebhook(CreateWebhook(status: WebhookStatus.Passive));
		this.CreateService();
		
		this.FireUserCreated(new { username = "john.doe" });
		await Task.Delay(300, TestContext.Current.CancellationToken);
		
		Assert.Equal(0, this._webhookQueue.Count);
	}
	
	[Fact]
	public async Task OnEventFired_QueuesASnapshotOfTheEventData()
	{
		// The call is executed later: a change of the event's objects after the event must not reach the request
		this.AddWebhook(CreateWebhook(url: "https://hooks.example.com/{{document.username}}"));
		var service = this.CreateService();
		var document = new Dictionary<string, object?> { ["username"] = "john.doe" };
		
		this.FireUserCreated(document);
		await this.WaitForQueuedWebhooksAsync(1);
		document["username"] = "changed";
		await this.ExecuteQueuedWebhooksAsync(service, 1);
		
		var request = Assert.Single(this._sentRequests);
		Assert.Equal("https://hooks.example.com/john.doe", request.Url);
		Assert.Equal("john.doe", request.Body?["document"]?["username"]?.GetValue<string>());
	}
	
	#endregion
	
	#region Request
	
	[Fact]
	public async Task OnEventFired_SendsTheDocumentPriorAndPayload()
	{
		var request = await this.SendSingleRequestAsync(CreateWebhook(bodyJson: """{"source":"erAuth"}"""), new { username = "john.doe" });
		
		Assert.Equal(HttpMethod.Post, request.Method);
		Assert.Equal("https://hooks.example.com/users", request.Url);
		Assert.Equal("john.doe", request.Body?["document"]?["username"]?.GetValue<string>());
		Assert.Equal("erAuth", request.Body?["payload"]?["source"]?.GetValue<string>());
		Assert.Equal(ErtisAuthEventType.WebhookRequestSent, Assert.Single(this.OutcomeEvents()));
	}
	
	[Fact]
	public async Task OnEventFired_WithUncoveredBody_SendsOnlyThePayload()
	{
		var request = await this.SendSingleRequestAsync(CreateWebhook(bodyJson: """{"source":"erAuth"}""", uncoveredBody: true), new { username = "john.doe" });
		
		Assert.Equal("""{"source":"erAuth"}""", request.Body?.ToJsonString());
	}
	
	[Fact]
	public async Task OnEventFired_SendsHeaderValuesAsTheyAre()
	{
		// Regression: header values were HTML- and URL-encoded ("Bearer%20abc.def%2Bghi%2Fjkl%3D")
		var headers = new Dictionary<string, string> { ["Authorization"] = "Bearer abc.def+ghi/jkl=", ["X-User"] = "{{document.first_name}} & co" };
		
		var request = await this.SendSingleRequestAsync(CreateWebhook(headers: headers), new { first_name = "John" });
		
		Assert.Equal("Bearer abc.def+ghi/jkl=", request.Headers["Authorization"]);
		Assert.Equal("John & co", request.Headers["X-User"]);
	}
	
	[Fact]
	public async Task OnEventFired_WithLineBreakInATemplatedHeader_DropsOnlyThatHeader()
	{
		var headers = new Dictionary<string, string> { ["Authorization"] = "Bearer token", ["X-User"] = "{{document.first_name}}" };
		
		var request = await this.SendSingleRequestAsync(CreateWebhook(headers: headers), new { first_name = "John\r\nX-Injected: yes" });
		
		Assert.Equal("Bearer token", request.Headers["Authorization"]);
		Assert.False(request.Headers.ContainsKey("X-User"));
	}
	
	[Fact]
	public async Task OnEventFired_TemplatedBodyValuesCanNotChangeTheBodyStructure()
	{
		// Attack: a user sets their own username to break out of the JSON string
		const string username = "evil\", \"role\": \"admin";
		
		var request = await this.SendSingleRequestAsync(CreateWebhook(bodyJson: """{"name":"{{document.username}}","fixed":"x"}"""), new { username });
		
		var payload = request.Body?["payload"]?.AsObject();
		Assert.NotNull(payload);
		Assert.Equal(username, payload["name"]?.GetValue<string>());
		Assert.False(payload.ContainsKey("role"));
		Assert.Equal("x", payload["fixed"]?.GetValue<string>());
	}
	
	[Fact]
	public async Task OnEventFired_TemplatedUrlValuesAreEscaped()
	{
		var request = await this.SendSingleRequestAsync(CreateWebhook(url: "https://hooks.example.com/users/{{document.username}}?source=erAuth"), new { username = "../admin?x=1#" });
		
		Assert.Equal("https://hooks.example.com/users/..%2Fadmin%3Fx%3D1%23?source=erAuth", request.Url);
	}
	
	#endregion
	
	#region Retries
	
	[Fact]
	public async Task OnEventFired_WhenTheReceiverFails_RetriesTryCountTimes()
	{
		this._responseStatusCode = HttpStatusCode.InternalServerError;
		this.AddWebhook(CreateWebhook(tryCount: 3));
		var service = this.CreateService();
		
		this.FireUserCreated(new { username = "john.doe" });
		await this.ExecuteQueuedWebhooksAsync(service, 1);
		
		Assert.Equal(3, this._sentRequests.Count);
		Assert.All(this.OutcomeEvents(), x => Assert.Equal(ErtisAuthEventType.WebhookRequestFailed, x));
	}
	
	[Fact]
	public async Task OnEventFired_WhenTheReceiverSucceeds_DoesNotRetry()
	{
		this.AddWebhook(CreateWebhook(tryCount: 3));
		var service = this.CreateService();
		
		this.FireUserCreated(new { username = "john.doe" });
		await this.ExecuteQueuedWebhooksAsync(service, 1);
		
		Assert.Single(this._sentRequests);
		Assert.Equal(ErtisAuthEventType.WebhookRequestSent, Assert.Single(this.OutcomeEvents()));
	}
	
	#endregion
}