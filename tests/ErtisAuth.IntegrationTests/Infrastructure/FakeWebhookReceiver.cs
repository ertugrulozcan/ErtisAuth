using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

// ReSharper disable MemberCanBePrivate.Global
namespace ErtisAuth.IntegrationTests.Infrastructure;

/// <summary>
/// An HTTP server on a random loopback port that records every request (the webhook calls of ErtisAuth).
/// Each request path can be answered with a sequence of status codes (e.g. 500 then 200 to test retries); 200 otherwise.
/// </summary>
public sealed class FakeWebhookReceiver : IAsyncDisposable
{
	#region Fields
	
	private readonly WebApplication _application;
	
	private readonly ConcurrentQueue<ReceivedRequest> _requests = new();
	
	private readonly ConcurrentDictionary<string, ConcurrentQueue<int>> _statusCodes = new();
	
	#endregion
	
	#region Properties
	
	public Uri BaseAddress { get; }
	
	public IReadOnlyCollection<ReceivedRequest> Requests => this._requests.ToArray();
	
	#endregion
	
	#region Constructors
	
	private FakeWebhookReceiver(WebApplication application, Uri baseAddress)
	{
		this._application = application;
		this.BaseAddress = baseAddress;
	}
	
	#endregion
	
	#region Methods
	
	public static async Task<FakeWebhookReceiver> StartAsync()
	{
		var builder = WebApplication.CreateSlimBuilder();
		builder.WebHost.UseKestrel(options => options.Listen(System.Net.IPAddress.Loopback, 0));
		builder.Logging.ClearProviders();
		
		var application = builder.Build();
		FakeWebhookReceiver? receiver = null;
		application.Run(async context =>
		{
			using var reader = new StreamReader(context.Request.Body);
			var body = await reader.ReadToEndAsync();
			var path = context.Request.Path.Value ?? "/";
			
			// ReSharper disable once AccessToModifiedClosure
			receiver!._requests.Enqueue(new ReceivedRequest(
				context.Request.Method,
				path,
				context.Request.QueryString.Value ?? string.Empty,
				context.Request.Headers.ToDictionary(x => x.Key, x => x.Value.ToString(), StringComparer.OrdinalIgnoreCase),
				body));
			
			// ReSharper disable once AccessToModifiedClosure
			context.Response.StatusCode = receiver._statusCodes.TryGetValue(path, out var statusCodes) && statusCodes.TryDequeue(out var statusCode)
				? statusCode
				: StatusCodes.Status200OK;
		});
		
		await application.StartAsync();
		receiver = new FakeWebhookReceiver(application, new Uri(application.Urls.First()));
		return receiver;
	}
	
	/// <summary>
	/// The next requests to the path are answered with these status codes, in order.
	/// </summary>
	public void RespondWith(string path, params int[] statusCodes)
	{
		this._statusCodes[path] = new ConcurrentQueue<int>(statusCodes);
	}
	
	public string Url(string pathAndQuery) => $"{this.BaseAddress.ToString().TrimEnd('/')}{pathAndQuery}";
	
	public IEnumerable<ReceivedRequest> RequestsTo(string path) => this.Requests.Where(x => x.Path == path);
	
	/// <summary>
	/// Waits for the given number of requests to the path (webhooks are sent in the background).
	/// </summary>
	public async Task<ReceivedRequest[]> WaitForRequestsAsync(string path, int count = 1, TimeSpan? timeout = null)
	{
		var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
		while (DateTime.UtcNow < deadline)
		{
			var requests = this.RequestsTo(path).ToArray();
			if (requests.Length >= count)
			{
				return requests;
			}
			
			await Task.Delay(50, TestContext.Current.CancellationToken);
		}
		
		throw new TimeoutException($"{this.RequestsTo(path).Count()} of {count} requests to {path} within the timeout. Received: {string.Join(", ", this.Requests.Select(x => x.Path))}");
	}
	
	public async ValueTask DisposeAsync()
	{
		await this._application.StopAsync();
		await this._application.DisposeAsync();
	}
	
	#endregion
	
	#region Nested Types
	
	public sealed record ReceivedRequest(string Method, string Path, string QueryString, IReadOnlyDictionary<string, string> Headers, string Body)
	{
		public JsonNode? Json => string.IsNullOrWhiteSpace(this.Body) ? null : JsonNode.Parse(this.Body);
	}
	
	#endregion
}