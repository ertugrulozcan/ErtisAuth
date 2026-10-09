using System.Text.Json.Nodes;
using ErtisAuth.IntegrationTests.Infrastructure;
using ErtisAuth.IntegrationTests.Resources;
using MongoDB.Bson;

namespace ErtisAuth.IntegrationTests.Webhooks;

/// <summary>
/// Webhooks fired by real events and delivered over HTTP to a <see cref="FakeWebhookReceiver"/>:
/// templating (url, headers, body), escaping of user-controlled data, retries, execution events and membership isolation.
/// Every test uses its own path, so the webhooks of the tests do not see each other's requests.
/// </summary>
public class WebhookDeliveryTests : IClassFixture<ErtisAuthInstance>
{
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Properties
	
	private string MembershipUrl => $"/memberships/{this._instance.MembershipId}";
	
	private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="instance"></param>
	public WebhookDeliveryTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private async Task<ResourceClient> AdminResourceClientAsync(string resource) =>
		new(await this._instance.CreateAdminClientAsync(), $"{this.MembershipUrl}/{resource}");
	
	private static string NewPath() => $"/hooks/{Guid.NewGuid():N}";
	
	private async Task<string> CreateWebhookAsync(string eventType, string url, object? headers = null, object? body = null, int tryCount = 1, bool uncoveredBody = false, string status = "active")
	{
		var webhooks = await this.AdminResourceClientAsync("webhooks");
		var webhook = await webhooks.CreateAsync(new
		{
			name = $"Hook {Guid.NewGuid():N}",
			@event = eventType,
			status,
			request = new
			{
				method = "POST",
				url,
				headers = headers ?? new { },
				body = body ?? new { },
				uncoveredBody
			},
			try_count = tryCount
		});
		
		return webhook["_id"]!.GetValue<string>();
	}
	
	private async Task<JsonObject> CreateUserAsync(string? lastname = null)
	{
		var username = $"user{Guid.NewGuid():N}";
		var users = await this.AdminResourceClientAsync("users");
		return await users.CreateAsync(new
		{
			username,
			firstname = "Jane",
			lastname = lastname ?? "Doe",
			email_address = $"{username}@example.com",
			password = "Webhook-P@ssw0rd!",
			role = "admin",
			user_type = "user"
		});
	}
	
	/// <summary>
	/// The webhooks run in the background: waits until the execution events are recorded
	/// </summary>
	private async Task<JsonArray> WaitForWebhookEventsAsync(string webhookId, string eventType, int count)
	{
		var deadline = DateTime.UtcNow.AddSeconds(10);
		var events = await this.QueryWebhookEventsAsync(webhookId, eventType);
		while (events.Count < count && DateTime.UtcNow < deadline)
		{
			await Task.Delay(100, CancellationToken);
			events = await this.QueryWebhookEventsAsync(webhookId, eventType);
		}
		
		return events;
	}
	
	private async Task<JsonArray> QueryWebhookEventsAsync(string webhookId, string eventType)
	{
		var events = await this.AdminResourceClientAsync("events");
		return await events.QueryAsync(new Dictionary<string, object>
		{
			["where"] = new Dictionary<string, object>
			{
				["event_type"] = eventType,
				["document.webhook_id"] = webhookId
			}
		});
	}
	
	#endregion
	
	#region Delivery
	
	[Fact]
	public async Task UserCreated_SendsTheTemplatedRequest()
	{
		await using var receiver = await FakeWebhookReceiver.StartAsync();
		var path = NewPath();
		await this.CreateWebhookAsync(
			"UserCreated",
			receiver.Url($"{path}?user={{{{document.username}}}}"),
			headers: new Dictionary<string, string> { ["Authorization"] = "Bearer crm-token", ["X-User"] = "{{document.username}}" },
			body: new { user = "{{document.username}}", source = "ErtisAuth", nested = new { count = 1 } });
		
		var user = await this.CreateUserAsync();
		var username = user["username"]!.GetValue<string>();
		
		var request = (await receiver.WaitForRequestsAsync(path)).Single();
		Assert.Equal("POST", request.Method);
		Assert.Equal($"?user={username}", request.QueryString);
		Assert.Equal("Bearer crm-token", request.Headers["Authorization"]);
		Assert.Equal(username, request.Headers["X-User"]);
		
		var body = request.Json!;
		Assert.Equal(username, body["document"]!["username"]!.GetValue<string>());
		Assert.Null(body["prior"]);
		Assert.Equal(username, body["payload"]!["user"]!.GetValue<string>());
		Assert.Equal("ErtisAuth", body["payload"]!["source"]!.GetValue<string>());
		Assert.Equal(1, body["payload"]!["nested"]!["count"]!.GetValue<int>());
		Assert.DoesNotContain("password", request.Body);
	}
	
	[Fact]
	public async Task UncoveredBody_SendsOnlyThePayload()
	{
		await using var receiver = await FakeWebhookReceiver.StartAsync();
		var path = NewPath();
		await this.CreateWebhookAsync("UserCreated", receiver.Url(path), body: new { user = "{{document.username}}" }, uncoveredBody: true);
		
		var user = await this.CreateUserAsync();
		
		var request = (await receiver.WaitForRequestsAsync(path)).Single();
		var body = request.Json!.AsObject();
		Assert.Equal(["user"], body.Select(x => x.Key));
		Assert.Equal(user["username"]!.GetValue<string>(), body["user"]!.GetValue<string>());
	}
	
	[Fact]
	public async Task UserUpdatedAndDeleted_SendTheDocumentAndThePrior()
	{
		await using var receiver = await FakeWebhookReceiver.StartAsync();
		var updatedPath = NewPath();
		var deletedPath = NewPath();
		await this.CreateWebhookAsync("UserUpdated", receiver.Url(updatedPath));
		await this.CreateWebhookAsync("UserDeleted", receiver.Url(deletedPath));
		var user = await this.CreateUserAsync();
		var id = user["_id"]!.GetValue<string>();
		var users = await this.AdminResourceClientAsync("users");
		
		var update = user.DeepClone().AsObject();
		update.Remove("_id");
		update["lastname"] = "Smith";
		await users.UpdateAsync(id, update);
		await users.DeleteAsync(id);
		
		var updated = (await receiver.WaitForRequestsAsync(updatedPath)).Single().Json!;
		Assert.Equal("Smith", updated["document"]!["lastname"]!.GetValue<string>());
		Assert.Equal("Doe", updated["prior"]!["lastname"]!.GetValue<string>());
		
		var deleted = (await receiver.WaitForRequestsAsync(deletedPath)).Single().Json!;
		Assert.Equal(id, deleted["prior"]!["_id"]!.GetValue<string>());
	}
	
	[Fact]
	public async Task PassiveWebhook_IsNotSent()
	{
		await using var receiver = await FakeWebhookReceiver.StartAsync();
		var path = NewPath();
		await this.CreateWebhookAsync("UserCreated", receiver.Url(path), status: "passive");
		
		await this.CreateUserAsync();
		
		await Task.Delay(TimeSpan.FromSeconds(1), CancellationToken);
		Assert.Empty(receiver.RequestsTo(path));
	}
	
	/// <summary>
	/// A webhook of another membership (inserted directly) is not fired by this membership's events.
	/// </summary>
	[Fact]
	public async Task WebhookOfAnotherMembership_IsNotSent()
	{
		await using var receiver = await FakeWebhookReceiver.StartAsync();
		var path = NewPath();
		await this._instance.Database.GetCollection<BsonDocument>("webhooks").InsertOneAsync(new BsonDocument
		{
			{ "name", "Other" },
			{ "event", "UserCreated" },
			{ "status", "active" },
			{ "request", new BsonDocument { { "method", "POST" }, { "url", receiver.Url(path) } } },
			{ "try_count", 1 },
			{ "membership_id", "other-membership" }
		}, cancellationToken: CancellationToken);
		
		await this.CreateUserAsync();
		
		await Task.Delay(TimeSpan.FromSeconds(1), CancellationToken);
		Assert.Empty(receiver.RequestsTo(path));
	}
	
	#endregion
	
	#region Escaping
	
	/// <summary>
	/// User-controlled data can not change the structure of the body (JSON injection), add headers (CR/LF) or
	/// break out of its url segment.
	/// </summary>
	[Fact]
	public async Task UserControlledData_IsEscapedForEachTarget()
	{
		await using var receiver = await FakeWebhookReceiver.StartAsync();
		var path = NewPath();
		const string lastname = "Doe\", \"admin\": true, \"x\": \"/a?b=c&d\r\nX-Injected: 1";
		await this.CreateWebhookAsync(
			"UserCreated",
			receiver.Url($"{path}?lastname={{{{document.lastname}}}}"),
			headers: new Dictionary<string, string> { ["X-Lastname"] = "{{document.lastname}}", ["X-Static"] = "static" },
			body: new { lastname = "{{document.lastname}}" },
			uncoveredBody: true);
		
		await this.CreateUserAsync(lastname);
		
		var request = (await receiver.WaitForRequestsAsync(path)).Single();
		
		// Body: still a single string value
		var body = request.Json!.AsObject();
		Assert.Equal(["lastname"], body.Select(x => x.Key));
		Assert.Equal(lastname, body["lastname"]!.GetValue<string>());
		
		// Headers: the templated header with a line break is dropped, nothing is injected
		Assert.False(request.Headers.ContainsKey("X-Lastname"));
		Assert.False(request.Headers.ContainsKey("X-Injected"));
		Assert.Equal("static", request.Headers["X-Static"]);
		
		// Url: a single, escaped query value
		Assert.Equal($"?lastname={Uri.EscapeDataString(lastname)}", request.QueryString);
	}
	
	#endregion
	
	#region Retries
	
	[Fact]
	public async Task FailedRequest_IsRetriedUntilItSucceeds()
	{
		await using var receiver = await FakeWebhookReceiver.StartAsync();
		var path = NewPath();
		receiver.RespondWith(path, 500, 503, 200);
		var webhookId = await this.CreateWebhookAsync("UserCreated", receiver.Url(path), tryCount: 5);
		
		await this.CreateUserAsync();
		
		await receiver.WaitForRequestsAsync(path, count: 3);
		await Task.Delay(TimeSpan.FromSeconds(1), CancellationToken);
		Assert.Equal(3, receiver.RequestsTo(path).Count());
		
		var failed = await this.QueryWebhookEventsAsync(webhookId, "WebhookRequestFailed");
		var sent = await this.QueryWebhookEventsAsync(webhookId, "WebhookRequestSent");
		Assert.Equal([1, 2], failed.Select(x => x!["document"]!["tryIndex"]!.GetValue<int>()).Order());
		Assert.Equal(3, Assert.Single(sent)!["document"]!["tryIndex"]!.GetValue<int>());
	}
	
	[Fact]
	public async Task FailedRequest_IsTriedAtMostTryCountTimes()
	{
		await using var receiver = await FakeWebhookReceiver.StartAsync();
		var path = NewPath();
		receiver.RespondWith(path, 500, 500, 500, 500);
		var webhookId = await this.CreateWebhookAsync("UserCreated", receiver.Url(path), tryCount: 2);
		
		await this.CreateUserAsync();
		
		await receiver.WaitForRequestsAsync(path, count: 2);
		await Task.Delay(TimeSpan.FromSeconds(1), CancellationToken);
		Assert.Equal(2, receiver.RequestsTo(path).Count());
		var failed = await this.QueryWebhookEventsAsync(webhookId, "WebhookRequestFailed");
		Assert.Equal(2, failed.Count);
		Assert.All(failed, x => Assert.Equal(500, x!["document"]!["response"]!["statusCode"]!.GetValue<int>()));
		Assert.Empty(await this.QueryWebhookEventsAsync(webhookId, "WebhookRequestSent"));
	}
	
	/// <summary>
	/// A request which can not be sent (connection refused, timeout) is retried and each failure is recorded with the error
	/// (regression: the exception could not be serialized into the event, the retries were skipped and nothing was recorded).
	/// </summary>
	[Fact]
	public async Task UnreachableReceiver_IsRetriedAndEachFailureIsRecorded()
	{
		// A receiver which is stopped: its port refuses the connections
		var receiver = await FakeWebhookReceiver.StartAsync();
		var url = receiver.Url(NewPath());
		await receiver.DisposeAsync();
		var webhookId = await this.CreateWebhookAsync("UserCreated", url, tryCount: 2);
		
		await this.CreateUserAsync();
		
		var failed = await this.WaitForWebhookEventsAsync(webhookId, "WebhookRequestFailed", count: 2);
		Assert.Equal([1, 2], failed.Select(x => x!["document"]!["tryIndex"]!.GetValue<int>()).Order());
		Assert.All(failed, x =>
		{
			var exception = x!["document"]!["exception"]!.AsObject();
			Assert.False(string.IsNullOrEmpty(exception["type"]?.GetValue<string>()));
			Assert.False(string.IsNullOrEmpty(exception["message"]?.GetValue<string>()));
			
			// The stack trace is not exposed
			Assert.Equal(["message", "type"], exception.Select(property => property.Key).Order());
		});
	}
	
	#endregion
}