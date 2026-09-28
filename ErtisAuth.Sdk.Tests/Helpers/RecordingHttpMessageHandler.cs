using System.Net;
using System.Text;

namespace ErtisAuth.Sdk.Tests.Helpers;

/// <summary>
/// Stands in for the network: records every request the SDK sends and answers with a prepared response.
/// </summary>
internal sealed class RecordingHttpMessageHandler : HttpMessageHandler
{
	#region Properties
	
	public List<RecordedRequest> Requests { get; } = [];
	
	public RecordedRequest LastRequest => Assert.Single(this.Requests);
	
	public HttpStatusCode ResponseStatusCode { get; set; } = HttpStatusCode.OK;
	
	/// <summary>
	/// Null answers without a body (e.g. 204 No Content).
	/// </summary>
	public string? ResponseJson { get; set; } = "{}";
	
	#endregion
	
	#region Methods
	
	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
		var headers = request.Headers
			.Concat(request.Content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>())
			.ToDictionary(x => x.Key, x => string.Join(",", x.Value), StringComparer.OrdinalIgnoreCase);
		
		this.Requests.Add(new RecordedRequest(request.Method, request.RequestUri!, headers, body));
		
		var response = new HttpResponseMessage(this.ResponseStatusCode);
		if (this.ResponseJson != null)
		{
			response.Content = new StringContent(this.ResponseJson, Encoding.UTF8, "application/json");
		}
		
		return response;
	}
	
	#endregion
}

internal sealed record RecordedRequest(HttpMethod Method, Uri Uri, IReadOnlyDictionary<string, string> Headers, string? Body)
{
	/// <summary>
	/// The url without the query string.
	/// </summary>
	public string Path => this.Uri.GetLeftPart(UriPartial.Path);
	
	/// <summary>
	/// The query string exactly as sent (still encoded).
	/// </summary>
	public string Query => this.Uri.Query.TrimStart('?');
	
	public string? Header(string name) => this.Headers.GetValueOrDefault(name);
}
