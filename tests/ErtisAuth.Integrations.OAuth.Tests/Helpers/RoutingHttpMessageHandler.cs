using System.Net;
using System.Text;

namespace ErtisAuth.Integrations.OAuth.Tests.Helpers;

/// <summary>
/// Answers requests by URL (path without query) and records them. Unknown URLs get 404, so an unexpected call fails visibly.
/// </summary>
internal sealed class RoutingHttpMessageHandler : HttpMessageHandler
{
	#region Fields
	
	private readonly Dictionary<string, (HttpStatusCode StatusCode, string Body)> _routes = new(StringComparer.OrdinalIgnoreCase);
	
	private readonly Dictionary<string, Exception> _failures = new(StringComparer.OrdinalIgnoreCase);
	
	#endregion
	
	#region Properties
	
	public List<RecordedRequest> Requests { get; } = [];
	
	#endregion
	
	#region Methods
	
	public void Respond(string url, string json, HttpStatusCode statusCode = HttpStatusCode.OK)
	{
		this._routes[url] = (statusCode, json);
	}
	
	/// <summary>
	/// Requests to the url fail with the exception (e.g. HttpRequestException for an unreachable host).
	/// </summary>
	public void Fail(string url, Exception exception)
	{
		this._failures[url] = exception;
	}
	
	public RecordedRequest SingleRequestTo(string url)
	{
		return Assert.Single(this.Requests, x => string.Equals(x.Path, url, StringComparison.OrdinalIgnoreCase));
	}
	
	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
		var recorded = new RecordedRequest(request.Method, request.RequestUri!, request.Headers.Authorization?.ToString(), body);
		this.Requests.Add(recorded);
		
		if (this._failures.TryGetValue(recorded.Path, out var exception))
		{
			throw exception;
		}
		
		if (!this._routes.TryGetValue(recorded.Path, out var route))
		{
			return new HttpResponseMessage(HttpStatusCode.NotFound);
		}
		
		return new HttpResponseMessage(route.StatusCode)
		{
			Content = new StringContent(route.Body, Encoding.UTF8, "application/json")
		};
	}
	
	#endregion
}

internal sealed record RecordedRequest(HttpMethod Method, Uri Uri, string? Authorization, string? Body)
{
	public string Path => this.Uri.GetLeftPart(UriPartial.Path);
	
	public string? Query(string name) => System.Web.HttpUtility.ParseQueryString(this.Uri.Query)[name];
	
	public string? Form(string name) => this.Body == null ? null : System.Web.HttpUtility.ParseQueryString(this.Body)[name];
}