using System.Net;
using System.Web;
using Ertis.Core.Collections;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Sdk.Services.Interfaces;
using ErtisAuth.Sdk.Tests.Helpers;

namespace ErtisAuth.Sdk.Tests.Services;

/// <summary>
/// The generic read/query/create/update/delete requests of membership bounded resources, through the applications service.
/// </summary>
public class MembershipBoundedServiceTests
{
	#region Constants
	
	private const string ApplicationId = "5f8a1b2c3d4e5f6a7b8c9d02";
	
	private const string ApplicationsUrl = $"{SdkTestServices.BaseUrl}/memberships/{SdkTestServices.MembershipId}/applications";
	
	#endregion
	
	#region Fields
	
	private readonly SdkTestServices _sdk = new();
	
	private readonly IApplicationService _applicationService;
	
	private readonly BasicToken _token = new($"{ApplicationId}:secret");
	
	private const string ApplicationJson = """{"_id":"5f8a1b2c3d4e5f6a7b8c9d02","name":"server-app","role":"server","membership_id":"5f8a1b2c3d4e5f6a7b8c9d00"}""";
	
	private const string EmptyCollectionJson = """{"items":[],"count":0}""";
	
	#endregion
	
	#region Constructors
	
	public MembershipBoundedServiceTests()
	{
		this._applicationService = this._sdk.Get<IApplicationService>();
		this._sdk.Handler.ResponseJson = EmptyCollectionJson;
	}
	
	#endregion
	
	#region Helpers
	
	private RecordedRequest LastRequest => this._sdk.Handler.LastRequest;
	
	private System.Collections.Specialized.NameValueCollection LastQuery => HttpUtility.ParseQueryString(this.LastRequest.Query);
	
	private static Application CreateApplication(string? id = ApplicationId)
	{
		return new Application
		{
			Id = id!,
			Name = "server-app",
			Role = "server",
			MembershipId = SdkTestServices.MembershipId
		};
	}
	
	#endregion
	
	#region Read
	
	// Overloads differ only in the sort parameter type (Sorting or string orderBy), so calls without sorting must name one
	
	[Fact]
	public async Task GetAsync_ById_GetsResourceWithAuthorizationHeader()
	{
		this._sdk.Handler.ResponseJson = ApplicationJson;
		
		var response = await this._applicationService.GetAsync(ApplicationId, this._token, TestContext.Current.CancellationToken);
		
		Assert.Equal(HttpMethod.Get, this.LastRequest.Method);
		Assert.Equal($"{ApplicationsUrl}/{ApplicationId}", this.LastRequest.Path);
		Assert.Equal($"Basic {ApplicationId}:secret", this.LastRequest.Header("Authorization"));
		Assert.Equal("server-app", response.Data?.Name);
	}
	
	[Fact]
	public async Task GetAsync_WithPaging_SendsPagingQuery()
	{
		this._sdk.Handler.ResponseJson = """{"items":[{"_id":"5f8a1b2c3d4e5f6a7b8c9d02","name":"server-app","role":"server","membership_id":"5f8a1b2c3d4e5f6a7b8c9d00"}],"count":1}""";
		
		var response = await this._applicationService.GetAsync(this._token, skip: 10, limit: 5, withCount: true, orderBy: "name", sortDirection: SortDirection.Descending, cancellationToken: TestContext.Current.CancellationToken);
		
		Assert.Equal(ApplicationsUrl, this.LastRequest.Path);
		Assert.Equal("10", this.LastQuery["skip"]);
		Assert.Equal("5", this.LastQuery["limit"]);
		Assert.Equal("true", this.LastQuery["with_count"]);
		Assert.Single(response.Data!.Items);
		Assert.Equal(1, response.Data.Count);
	}
	
	[Fact]
	public async Task GetAsync_WithDescendingSort_SendsSortTheApiReadsAsDescending()
	{
		await this._applicationService.GetAsync(this._token, orderBy: "name", sortDirection: SortDirection.Descending, cancellationToken: TestContext.Current.CancellationToken);
		
		// The SDK encodes the space itself ("%20desc"), so after one decode the API receives "name%20desc";
		// the API's sort parser accepts that form as well as "name desc" (verified against Ertis.Extensions.AspNetCore 9.0.5)
		Assert.Equal("name%20desc", this.LastQuery["sort"]);
	}
	
	[Fact]
	public async Task GetAsync_WithAscendingSort_SendsFieldOnly()
	{
		await this._applicationService.GetAsync(this._token, orderBy: "name", sortDirection: SortDirection.Ascending, cancellationToken: TestContext.Current.CancellationToken);
		
		Assert.Equal("name", this.LastQuery["sort"]);
	}
	
	[Fact]
	public async Task GetAsync_WithSortingOfSeveralFields_JoinsThemWithSemicolon()
	{
		var sorting = new Sorting([new SortField("name", SortDirection.Descending), new SortField("slug", SortDirection.Ascending)]);
		
		await this._applicationService.GetAsync(this._token, sorting: sorting, cancellationToken: TestContext.Current.CancellationToken);
		
		Assert.Equal("name%20desc;slug", this.LastQuery["sort"]);
	}
	
	[Fact]
	public async Task GetAsync_WithoutPaging_SendsNoQuery()
	{
		await this._applicationService.GetAsync(this._token, orderBy: null, cancellationToken: TestContext.Current.CancellationToken);
		
		Assert.Equal(ApplicationsUrl, this.LastRequest.Path);
		Assert.Equal(string.Empty, this.LastRequest.Query);
	}
	
	[Theory]
	[InlineData("server app")]
	[InlineData("çağrı")]
	public async Task GetAsync_WithSearchKeyword_CallsSearchEndpoint(string keyword)
	{
		await this._applicationService.GetAsync(this._token, limit: 5, orderBy: null, searchKeyword: keyword, cancellationToken: TestContext.Current.CancellationToken);
		
		Assert.Equal($"{ApplicationsUrl}/search", this.LastRequest.Path);
		Assert.Equal(keyword, this.LastQuery["keyword"]);
		Assert.Equal("5", this.LastQuery["limit"]);
	}
	
	[Fact]
	public async Task GetAsync_WithBlankSearchKeyword_ListsResources()
	{
		await this._applicationService.GetAsync(this._token, orderBy: null, searchKeyword: "  ", cancellationToken: TestContext.Current.CancellationToken);
		
		Assert.Equal(ApplicationsUrl, this.LastRequest.Path);
	}
	
	#endregion
	
	#region Query
	
	[Fact]
	public async Task QueryAsync_PostsQueryToQueryEndpoint()
	{
		const string query = """{"where":{"role":"server"}}""";
		
		await this._applicationService.QueryAsync(this._token, query, limit: 20, orderBy: null, cancellationToken: TestContext.Current.CancellationToken);
		
		Assert.Equal(HttpMethod.Post, this.LastRequest.Method);
		Assert.Equal($"{ApplicationsUrl}/_query", this.LastRequest.Path);
		Assert.Equal("20", this.LastQuery["limit"]);
		// The query is sent as is (not serialized again as a JSON string)
		Assert.Equal(query, this.LastRequest.Body);
	}
	
	#endregion
	
	#region Create, Update & Delete
	
	[Fact]
	public async Task CreateAsync_PostsModel()
	{
		this._sdk.Handler.ResponseStatusCode = HttpStatusCode.Created;
		this._sdk.Handler.ResponseJson = ApplicationJson;
		
		await this._applicationService.CreateAsync(CreateApplication(), this._token, TestContext.Current.CancellationToken);
		
		Assert.Equal(HttpMethod.Post, this.LastRequest.Method);
		Assert.Equal(ApplicationsUrl, this.LastRequest.Path);
		Assert.Contains("\"name\":\"server-app\"", this.LastRequest.Body);
		Assert.Contains("\"role\":\"server\"", this.LastRequest.Body);
	}
	
	[Fact]
	public async Task UpdateAsync_PutsModelToItsUrl()
	{
		this._sdk.Handler.ResponseJson = ApplicationJson;
		
		await this._applicationService.UpdateAsync(CreateApplication(), this._token, TestContext.Current.CancellationToken);
		
		Assert.Equal(HttpMethod.Put, this.LastRequest.Method);
		Assert.Equal($"{ApplicationsUrl}/{ApplicationId}", this.LastRequest.Path);
		Assert.Contains("\"name\":\"server-app\"", this.LastRequest.Body);
	}
	
	[Fact]
	public async Task UpdateAsync_WithoutId_FailsWithoutSendingRequest()
	{
		var response = await this._applicationService.UpdateAsync(CreateApplication(id: string.Empty), this._token, TestContext.Current.CancellationToken);
		
		Assert.False(response.IsSuccess);
		Assert.Empty(this._sdk.Handler.Requests);
	}
	
	[Fact]
	public async Task DeleteAsync_DeletesResourceUrl()
	{
		// Answered with an error: a successful delete currently throws (see the skipped test)
		this._sdk.Handler.ResponseStatusCode = HttpStatusCode.NotFound;
		
		await this._applicationService.DeleteAsync(ApplicationId, this._token, TestContext.Current.CancellationToken);
		
		Assert.Equal(HttpMethod.Delete, this.LastRequest.Method);
		Assert.Equal($"{ApplicationsUrl}/{ApplicationId}", this.LastRequest.Path);
	}
	
	[Fact]
	public async Task BulkDeleteAsync_SendsIdsInBody()
	{
		// Answered with an error: a successful delete currently throws (see the skipped test)
		this._sdk.Handler.ResponseStatusCode = HttpStatusCode.BadRequest;
		
		await this._applicationService.BulkDeleteAsync(["id-1", "id-2"], this._token, TestContext.Current.CancellationToken);
		
		Assert.Equal(HttpMethod.Delete, this.LastRequest.Method);
		Assert.Equal(ApplicationsUrl, this.LastRequest.Path);
		Assert.Equal("""["id-1","id-2"]""", this.LastRequest.Body);
	}
	
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task DeleteAsync_WithNoContentResponse_Succeeds(bool bulk)
	{
		this._sdk.Handler.ResponseStatusCode = HttpStatusCode.NoContent;
		this._sdk.Handler.ResponseJson = null;
		
		var response = bulk
			? await this._applicationService.BulkDeleteAsync([ApplicationId], this._token, TestContext.Current.CancellationToken)
			: await this._applicationService.DeleteAsync(ApplicationId, this._token, TestContext.Current.CancellationToken);
		
		Assert.True(response.IsSuccess);
	}
	
	#endregion
	
	#region Errors
	
	[Theory]
	[InlineData(HttpStatusCode.Forbidden)]
	[InlineData(HttpStatusCode.NotFound)]
	[InlineData(HttpStatusCode.InternalServerError)]
	public async Task GetAsync_WithErrorResponse_ReturnsFailureWithStatusCode(HttpStatusCode statusCode)
	{
		this._sdk.Handler.ResponseStatusCode = statusCode;
		this._sdk.Handler.ResponseJson = """{"message":"error","errorCode":"Error","statusCode":0}""";
		
		var response = await this._applicationService.GetAsync(ApplicationId, this._token, TestContext.Current.CancellationToken);
		
		Assert.False(response.IsSuccess);
		Assert.Equal(statusCode, response.StatusCode);
	}
	
	#endregion
}
