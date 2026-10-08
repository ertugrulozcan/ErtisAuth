using System.Net;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Extensions.Authorization;
using ErtisAuth.Extensions.Authorization.Attributes;
using ErtisAuth.Extensions.Authorization.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace ErtisAuth.Extensions.AspNetCore.Tests.Authorization;

public class RbacSegmentResolverTests
{
	#region Helpers
	
	private static DefaultHttpContext CreateHttpContext(string? queryString = null, Dictionary<string, string>? headers = null)
	{
		var httpContext = new DefaultHttpContext();
		httpContext.Request.RouteValues["id"] = "item-1";
		if (queryString != null)
		{
			httpContext.Request.QueryString = new QueryString(queryString);
		}
		
		if (headers != null)
		{
			foreach (var (name, value) in headers)
			{
				httpContext.Request.Headers.Append(name, value);
			}
		}
		
		return httpContext;
	}
	
	/// <summary>
	/// Sets an environment variable with a unique name for the test, the tests run in parallel in the same process
	/// </summary>
	private static string SetEnvironmentVariable(string? value)
	{
		var name = $"ERTISAUTH_TEST_{Guid.NewGuid():N}";
		Environment.SetEnvironmentVariable(name, value);
		return name;
	}
	
	private static void AssertAccessDenied(Action action)
	{
		var exception = Assert.Throws<ErtisAuthException>(action);
		Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
	}
	
	#endregion
	
	#region Route
	
	[Theory]
	[InlineData("{id}")]
	[InlineData("{route::id}")]
	public void RoutePlaceholder_IsResolvedFromRouteValues(string segmentValue)
	{
		Assert.Equal("item-1", RbacSegmentResolver.Resolve(segmentValue, CreateHttpContext()));
	}
	
	[Theory]
	[InlineData("{id}", "*")]
	[InlineData("{id}", "__all__")]
	[InlineData("{id}", "a%2Eb")]
	[InlineData("{id}", "a%2eb")]
	[InlineData("{id}", "a b")]
	[InlineData("{id}", " a")]
	[InlineData("{id}", "a\n")]
	[InlineData("{id}", "")]
	[InlineData("{route::id}", "*")]
	[InlineData("{route::id}", "a%2Eb")]
	[InlineData("{route::id}", "a b")]
	public void InvalidRouteValue_DeniesAccess(string segmentValue, string routeValue)
	{
		var httpContext = CreateHttpContext();
		httpContext.Request.RouteValues["id"] = routeValue;
		
		AssertAccessDenied(() => RbacSegmentResolver.Resolve(segmentValue, httpContext));
	}
	
	[Fact]
	public void RouteValueWithDot_IsResolved()
	{
		// The dots are encoded as %2E later, they can not split the segments
		var httpContext = CreateHttpContext();
		httpContext.Request.RouteValues["id"] = "a.b";
		
		Assert.Equal("a.b", RbacSegmentResolver.Resolve("{id}", httpContext));
	}
	
	[Fact]
	public void UndefinedRoutePlaceholder_IsWrittenAsItIs()
	{
		// Unchanged behavior of the route placeholders
		Assert.Equal("{key}", RbacSegmentResolver.Resolve("{key}", CreateHttpContext()));
	}
	
	[Theory]
	[InlineData("{ID}")]
	[InlineData("{route::ID}")]
	public void RoutePlaceholder_IsCaseSensitive(string segmentValue)
	{
		// ERTISAUTH601 relies on this, the placeholder must be written as the route parameter
		Assert.StartsWith("{", RbacSegmentResolver.Resolve(segmentValue, CreateHttpContext()));
	}
	
	#endregion
	
	#region Query
	
	[Fact]
	public void QueryPlaceholder_IsResolvedFromQueryString()
	{
		Assert.Equal("item-2", RbacSegmentResolver.Resolve("{query::id}", CreateHttpContext("?id=item-2")));
	}
	
	[Theory]
	[InlineData(null)]
	[InlineData("?other=x")]
	[InlineData("?id=")]
	[InlineData("?id=%20")]
	[InlineData("?id=a&id=b")]
	[InlineData("?id=*")]
	[InlineData("?id=__all__")]
	[InlineData("?id=a%252Eb")]
	[InlineData("?id=a%252eb")]
	[InlineData("?id=a%20b")]
	[InlineData("?id=a+b")]
	[InlineData("?id=a%09b")]
	[InlineData("?id=a%0A")]
	public void InvalidQueryValue_DeniesAccess(string? queryString)
	{
		AssertAccessDenied(() => RbacSegmentResolver.Resolve("{query::id}", CreateHttpContext(queryString)));
	}
	
	#endregion
	
	#region Header
	
	[Theory]
	[InlineData("X-Username")]
	[InlineData("x-username")]
	public void HeaderPlaceholder_IsResolvedCaseInsensitively(string headerName)
	{
		var httpContext = CreateHttpContext(headers: new Dictionary<string, string> { [headerName] = "john" });
		Assert.Equal("john", RbacSegmentResolver.Resolve("{header::X-Username}", httpContext));
	}
	
	[Theory]
	[InlineData("John Doe")]
	[InlineData("john%2Edoe")]
	[InlineData("*")]
	public void InvalidHeaderValue_DeniesAccess(string headerValue)
	{
		var httpContext = CreateHttpContext(headers: new Dictionary<string, string> { ["X-Username"] = headerValue });
		AssertAccessDenied(() => RbacSegmentResolver.Resolve("{header::X-Username}", httpContext));
	}
	
	[Fact]
	public void MissingHeader_DeniesAccess()
	{
		AssertAccessDenied(() => RbacSegmentResolver.Resolve("{header::X-Username}", CreateHttpContext()));
	}
	
	[Fact]
	public void RepeatedHeader_DeniesAccess()
	{
		var httpContext = CreateHttpContext();
		httpContext.Request.Headers.Append("X-Username", "john");
		httpContext.Request.Headers.Append("X-Username", "jane");
		
		AssertAccessDenied(() => RbacSegmentResolver.Resolve("{header::X-Username}", httpContext));
	}
	
	#endregion
	
	#region Environment variable
	
	[Fact]
	public void EnvironmentVariablePlaceholder_IsCombinedWithRawText()
	{
		var name = SetEnvironmentVariable("foo");
		Assert.Equal("foo:advertisers", RbacSegmentResolver.Resolve($"{{env::{name}}}:advertisers", CreateHttpContext()));
	}
	
	[Theory]
	[InlineData(null)]
	[InlineData("*")]
	[InlineData("foo bar")]
	[InlineData("foo%2Ebar")]
	public void InvalidEnvironmentVariable_DeniesAccess(string? value)
	{
		var name = SetEnvironmentVariable(value);
		AssertAccessDenied(() => RbacSegmentResolver.Resolve($"{{env::{name}}}:advertisers", CreateHttpContext()));
	}
	
	#endregion
	
	#region Sources
	
	[Fact]
	public void MultipleSources_AreResolvedInOneSegment()
	{
		var name = SetEnvironmentVariable("foo");
		var httpContext = CreateHttpContext("?key=k1", new Dictionary<string, string> { ["X-Username"] = "john" });
		
		var value = RbacSegmentResolver.Resolve($"{{env::{name}}}-{{header::X-Username}}-{{query::key}}-{{id}}", httpContext);
		
		Assert.Equal("foo-john-k1-item-1", value);
	}
	
	[Theory]
	[InlineData("{args::x}")]
	[InlineData("{ENV::X}")]
	[InlineData("{::x}")]
	[InlineData("{query::}")]
	public void InvalidPrefixedPlaceholder_DeniesAccess(string segmentValue)
	{
		AssertAccessDenied(() => RbacSegmentResolver.Resolve(segmentValue, CreateHttpContext("?x=1")));
	}
	
	[Theory]
	[InlineData("{query::user}")]
	[InlineData("{header::X-User}")]
	public void SubjectFromClientRequest_DeniesAccess(string segmentValue)
	{
		var httpContext = CreateHttpContext("?user=john", new Dictionary<string, string> { ["X-User"] = "john" });
		AssertAccessDenied(() => RbacSegmentResolver.Resolve(segmentValue, httpContext, isSubject: true));
	}
	
	[Fact]
	public void SubjectFromEnvironmentVariable_IsResolved()
	{
		var name = SetEnvironmentVariable("service-account");
		Assert.Equal("service-account", RbacSegmentResolver.Resolve($"{{env::{name}}}", CreateHttpContext(), isSubject: true));
	}
	
	#endregion
	
	#region Rbac definition
	
	[Fact]
	public void RbacDefinition_ResolvesPlaceholdersOfAllAttributes()
	{
		var organization = SetEnvironmentVariable("foo");
		var httpContext = CreateHttpContext("?key=k1", new Dictionary<string, string> { ["X-Action"] = "export.csv" });
		object[] metadata =
		[
			new RbacResourceAttribute($"{{env::{organization}}}:advertisers"),
			new RbacActionAttribute("{header::X-Action}"),
			new RbacObjectAttribute("{id}-{query::key}")
		];
		
		httpContext.SetEndpoint(new RouteEndpoint(_ => Task.CompletedTask, RoutePatternFactory.Parse("advertisers/{id}"), 0, new EndpointMetadataCollection(metadata), "test-endpoint"));
		
		var rbac = httpContext.GetRbacDefinition("user-1");
		
		Assert.NotNull(rbac);
		Assert.Equal("user-1", rbac.Subject.Value);
		Assert.Equal("foo:advertisers", rbac.Resource.Value);
		Assert.Equal("export%2Ecsv", rbac.Action.Value);
		Assert.Equal("item-1-k1", rbac.Object.Value);
	}
	
	[Fact]
	public void RbacDefinition_DeniesClientSourcedSubject()
	{
		var httpContext = CreateHttpContext(headers: new Dictionary<string, string> { ["X-User"] = "admin" });
		object[] metadata = [new RbacSubjectAttribute("{header::X-User}")];
		httpContext.SetEndpoint(new RouteEndpoint(_ => Task.CompletedTask, RoutePatternFactory.Parse("items/{id}"), 0, new EndpointMetadataCollection(metadata), "test-endpoint"));
		
		AssertAccessDenied(() => httpContext.GetRbacDefinition("user-1"));
	}
	
	/// <summary>
	/// Endpoint metadata lists the controller's attributes first: an [RbacResource] action of an [RbacResource] controller.
	/// </summary>
	[Fact]
	public void RbacDefinition_ActionAttributeOverridesControllerAttribute()
	{
		var httpContext = CreateHttpContext();
		object[] metadata =
		[
			new RbacResourceAttribute("users"),
			new RbacObjectAttribute("{id}"),
			new RbacResourceAttribute("otp"),
			new RbacActionAttribute("create")
		];
		
		httpContext.SetEndpoint(new RouteEndpoint(_ => Task.CompletedTask, RoutePatternFactory.Parse("users/{id}/generate-otp"), 0, new EndpointMetadataCollection(metadata), "test-endpoint"));
		
		var rbac = httpContext.GetRbacDefinition("user-1");
		
		Assert.NotNull(rbac);
		Assert.Equal("user-1.otp.create.item-1", rbac.ToString());
	}
	
	[RbacResource("base")]
	private class BaseController;
	
	[RbacResource("derived")]
	private class DerivedController : BaseController;
	
	[Fact]
	public void RbacResourceOfDerivedController_HidesTheBaseControllerAttribute()
	{
		// MVC collects the controller attributes with inheritance; RbacResource allows a single instance, so the derived one hides the base one
		var attributes = typeof(DerivedController).GetCustomAttributes(typeof(RbacResourceAttribute), inherit: true);
		
		var attribute = Assert.IsType<RbacResourceAttribute>(Assert.Single(attributes));
		Assert.Equal("derived", attribute.Value.Value);
	}
	
	#endregion
}
