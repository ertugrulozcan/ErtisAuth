using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace ErtisAuth.IntegrationTests.Resources;

/// <summary>
/// The CRUD endpoints of one resource (create, get, list, _query, search, update, delete, bulk delete) with the status
/// code and body shape asserted on each call. Failure messages carry the status code and the response body.
/// </summary>
public sealed class ResourceClient
{
	#region Fields
	
	private readonly HttpClient _client;
	
	#endregion
	
	#region Properties
	
	public string Url { get; }
	
	private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;
	
	#endregion
	
	#region Constructors
	
	public ResourceClient(HttpClient client, string url)
	{
		this._client = client;
		this.Url = url;
	}
	
	#endregion
	
	#region Methods
	
	public async Task<JsonObject> CreateAsync(object body)
	{
		using var response = await this._client.PostAsJsonAsync(this.Url, body, CancellationToken);
		var created = await AssertStatusAsync(response, HttpStatusCode.Created);
		var id = created!["_id"]!.GetValue<string>();
		Assert.Equal($"{this._client.BaseAddress!.ToString().TrimEnd('/')}{this.Url}/{id}", response.Headers.Location?.ToString());
		return created.AsObject();
	}
	
	public async Task<JsonObject> GetAsync(string id)
	{
		using var response = await this._client.GetAsync($"{this.Url}/{id}", CancellationToken);
		return (await AssertStatusAsync(response, HttpStatusCode.OK))!.AsObject();
	}
	
	public async Task<JsonArray> ListAsync(string? queryString = null)
	{
		using var response = await this._client.GetAsync($"{this.Url}{queryString}", CancellationToken);
		return ItemsOf(await AssertStatusAsync(response, HttpStatusCode.OK));
	}
	
	public async Task<JsonArray> QueryAsync(object query)
	{
		using var response = await this._client.PostAsJsonAsync($"{this.Url}/_query", query, CancellationToken);
		return ItemsOf(await AssertStatusAsync(response, HttpStatusCode.OK));
	}
	
	public async Task<JsonArray> SearchAsync(string keyword)
	{
		using var response = await this._client.GetAsync($"{this.Url}/search?keyword={Uri.EscapeDataString(keyword)}", CancellationToken);
		return ItemsOf(await AssertStatusAsync(response, HttpStatusCode.OK));
	}
	
	public async Task<JsonObject> UpdateAsync(string id, object body)
	{
		using var response = await this._client.PutAsJsonAsync($"{this.Url}/{id}", body, CancellationToken);
		return (await AssertStatusAsync(response, HttpStatusCode.OK))!.AsObject();
	}
	
	public async Task DeleteAsync(string id)
	{
		using var response = await this._client.DeleteAsync($"{this.Url}/{id}", CancellationToken);
		await AssertStatusAsync(response, HttpStatusCode.NoContent);
	}
	
	public async Task<HttpResponseMessage> BulkDeleteAsync(params string[] ids)
	{
		using var request = new HttpRequestMessage(HttpMethod.Delete, this.Url);
		request.Content = JsonContent.Create(ids);
		return await this._client.SendAsync(request, CancellationToken);
	}
	
	public async Task AssertNotFoundAsync(string id, string errorCode)
	{
		using var getResponse = await this._client.GetAsync($"{this.Url}/{id}", CancellationToken);
		var getBody = await AssertStatusAsync(getResponse, HttpStatusCode.NotFound);
		Assert.Equal(errorCode, getBody!["errorCode"]!.GetValue<string>());
		
		using var deleteResponse = await this._client.DeleteAsync($"{this.Url}/{id}", CancellationToken);
		var deleteBody = await AssertStatusAsync(deleteResponse, HttpStatusCode.NotFound);
		Assert.Equal(errorCode, deleteBody!["errorCode"]!.GetValue<string>());
	}
	
	#endregion
	
	#region Helpers
	
	public static async Task<JsonNode?> AssertStatusAsync(HttpResponseMessage response, HttpStatusCode expected)
	{
		var content = await response.Content.ReadAsStringAsync(CancellationToken);
		Assert.True(response.StatusCode == expected, $"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri?.PathAndQuery}: expected {(int) expected}, got {(int) response.StatusCode}: {content}");
		if (string.IsNullOrWhiteSpace(content))
		{
			return null;
		}
		
		// A string result is written as text/plain
		return response.Content.Headers.ContentType?.MediaType == "application/json" ? JsonNode.Parse(content) : JsonValue.Create(content);
	}
	
	private static JsonArray ItemsOf(JsonNode? page)
	{
		Assert.NotNull(page);
		return page["items"]!.AsArray();
	}
	
	public static string[] IdsOf(JsonArray items) => items.Select(x => x!["_id"]!.GetValue<string>()).ToArray();
	
	#endregion
}