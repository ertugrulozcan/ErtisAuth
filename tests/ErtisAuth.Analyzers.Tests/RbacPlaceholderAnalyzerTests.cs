using System.Collections.Immutable;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Extensions.AspNetCore.Attributes;
using ErtisAuth.Extensions.Authorization.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ErtisAuth.Analyzers.Tests;

public class RbacPlaceholderAnalyzerTests
{
	#region Helpers
	
	private const string Usings = """
		using ErtisAuth.Extensions.Authorization.Attributes;
		using ErtisAuth.Extensions.AspNetCore.Attributes;
		using Microsoft.AspNetCore.Mvc;
		""";
	
	private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source)
	{
		var trustedAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		var references = trustedAssemblies
			.Concat([typeof(RbacObjectAttribute).Assembly.Location, typeof(MembershipRouteAttribute).Assembly.Location, typeof(Rbac).Assembly.Location])
			.Distinct()
			.Select(x => MetadataReference.CreateFromFile(x));
		
		var compilation = CSharpCompilation.Create(
			"AnalyzerTestAssembly",
			[CSharpSyntaxTree.ParseText(Usings + source, cancellationToken: TestContext.Current.CancellationToken)],
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		
		var compilationErrors = compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(x => x.Severity == DiagnosticSeverity.Error).ToArray();
		Assert.Empty(compilationErrors);
		
		return await compilation
			.WithAnalyzers([new RbacPlaceholderAnalyzer()])
			.GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);
	}
	
	private static string[] GetMissingPlaceholders(ImmutableArray<Diagnostic> diagnostics)
	{
		Assert.All(diagnostics, x => Assert.Equal(RbacPlaceholderAnalyzer.RouteParameterDiagnosticId, x.Id));
		return diagnostics.Select(x => x.GetMessage().Split('\'')[1]).ToArray();
	}
	
	#endregion
	
	#region Tests
	
	[Fact]
	public async Task MatchingPlaceholder_NoDiagnostic()
	{
		var diagnostics = await AnalyzeAsync("""
			[Route("items")]
			public class ItemsController : ControllerBase
			{
				[HttpGet("{id}")]
				[RbacObject("{id}")]
				public IActionResult Get(string id) => this.Ok();
			}
			""");
		
		Assert.Empty(diagnostics);
	}
	
	[Fact]
	public async Task MismatchedPlaceholder_ReportsDiagnostic()
	{
		var diagnostics = await AnalyzeAsync("""
			[Route("items")]
			public class ItemsController : ControllerBase
			{
				[HttpGet("{id}")]
				[RbacObject("{itemId}")]
				public IActionResult Get(string id) => this.Ok();
			}
			""");
		
		Assert.Equal(["itemId"], GetMissingPlaceholders(diagnostics));
		Assert.Equal(DiagnosticSeverity.Warning, diagnostics[0].Severity);
		
		// ReSharper disable once MethodHasAsyncOverload
		Assert.Contains("RbacObject(\"{itemId}\")", diagnostics[0].Location.SourceTree!.GetText(TestContext.Current.CancellationToken).ToString(diagnostics[0].Location.SourceSpan));
	}
	
	[Fact]
	public async Task PlaceholderNotPassedToMethod_NoDiagnostic()
	{
		// Placeholders are resolved from the route values, the method parameters do not matter
		var diagnostics = await AnalyzeAsync("""
			[Route("items")]
			public class ItemsController : ControllerBase
			{
				[HttpGet("{id}")]
				[RbacObject("{id}")]
				public IActionResult Get() => this.Ok();
			}
			""");
		
		Assert.Empty(diagnostics);
	}
	
	[Fact]
	public async Task WithoutRbacObject_NoDiagnostic()
	{
		var diagnostics = await AnalyzeAsync("""
			[Route("items")]
			public class ItemsController : ControllerBase
			{
				[HttpGet("{id}")]
				public IActionResult Get(string id) => this.Ok();
			}
			""");
		
		Assert.Empty(diagnostics);
	}
	
	[Theory]
	[InlineData("{env::OBJECT_NAME}")]
	[InlineData("{query::x}")]
	[InlineData("{header::X-Name}")]
	[InlineData("{env::PREFIX}-{query::x}-{header::X-Name}")]
	[InlineData("prefix-{env::OBJECT_NAME}-suffix")]
	[InlineData("users")]
	public async Task SourcePrefixedPlaceholdersAndLiterals_AreIgnored(string objectName)
	{
		var diagnostics = await AnalyzeAsync($$"""
			[Route("items")]
			public class ItemsController : ControllerBase
			{
				[HttpGet("{id}")]
				[RbacObject("{{objectName}}")]
				public IActionResult Get(string id) => this.Ok();
			}
			""");
		
		Assert.Empty(diagnostics);
	}
	
	[Fact]
	public async Task SourcePrefixedPlaceholder_DoesNotHideMismatchedPlaceholder()
	{
		var diagnostics = await AnalyzeAsync("""
			[Route("items")]
			public class ItemsController : ControllerBase
			{
				[HttpGet("{id}")]
				[RbacObject("{env::PREFIX}-{key}")]
				public IActionResult Get(string id) => this.Ok();
			}
			""");
		
		Assert.Equal(["key"], GetMissingPlaceholders(diagnostics));
	}
	
	[Theory]
	[InlineData("{id:int}")]
	[InlineData("{id?}")]
	[InlineData("{id=default}")]
	[InlineData("{*id}")]
	[InlineData("{**id}")]
	public async Task RouteParameterSyntaxVariants_AreMatched(string routeParameter)
	{
		var diagnostics = await AnalyzeAsync($$"""
			[Route("items")]
			public class ItemsController : ControllerBase
			{
				[HttpGet("{{routeParameter}}")]
				[RbacObject("{id}")]
				public IActionResult Get() => this.Ok();
			}
			""");
		
		Assert.Empty(diagnostics);
	}
	
	[Theory]
	[InlineData("{id}")]
	[InlineData("{route::id}")]
	public async Task RouteParameterWithDifferentCase_ReportsDiagnostic(string objectName)
	{
		// The placeholders are resolved case-sensitively at runtime
		var diagnostics = await AnalyzeAsync($$"""
			[Route("items")]
			public class ItemsController : ControllerBase
			{
				[HttpGet("{ID}")]
				[RbacObject("{{objectName}}")]
				public IActionResult Get() => this.Ok();
			}
			""");
		
		Assert.Equal(["id"], GetMissingPlaceholders(diagnostics));
	}
	
	[Fact]
	public async Task EscapedBracesInRoute_AreNotRouteParameters()
	{
		var diagnostics = await AnalyzeAsync("""
			[Route("items")]
			public class ItemsController : ControllerBase
			{
				[HttpGet("{{id}}")]
				[RbacObject("{id}")]
				public IActionResult Get() => this.Ok();
			}
			""");
		
		Assert.Equal(["id"], GetMissingPlaceholders(diagnostics));
	}
	
	[Fact]
	public async Task PlaceholderFromControllerRoute_NoDiagnostic()
	{
		var diagnostics = await AnalyzeAsync("""
			[Route("tenants/{tenantId}/items")]
			public class ItemsController : ControllerBase
			{
				[HttpGet]
				[RbacObject("{tenantId}")]
				public IActionResult Get() => this.Ok();
			}
			""");
		
		Assert.Empty(diagnostics);
	}
	
	[Fact]
	public async Task PlaceholderFromBaseControllerRoute_NoDiagnostic()
	{
		var diagnostics = await AnalyzeAsync("""
			[Route("tenants/{tenantId}")]
			public abstract class TenantControllerBase : ControllerBase
			{
			}
			
			public class ItemsController : TenantControllerBase
			{
				[HttpGet("items")]
				[RbacObject("{tenantId}")]
				public IActionResult Get() => this.Ok();
			}
			""");
		
		Assert.Empty(diagnostics);
	}
	
	[Fact]
	public async Task MembershipRoute_ProvidesMembershipIdParameter()
	{
		var diagnostics = await AnalyzeAsync("""
			[MembershipRoute("items")]
			public class ItemsController : ControllerBase
			{
				[HttpGet("{id}")]
				[RbacObject("{membershipId}.{id}")]
				public IActionResult Get() => this.Ok();
				
				[HttpDelete("{id}")]
				[RbacObject("{itemId}")]
				public IActionResult Delete() => this.Ok();
			}
			""");
		
		Assert.Equal(["itemId"], GetMissingPlaceholders(diagnostics));
	}
	
	[Fact]
	public async Task AbsoluteActionRoute_IgnoresControllerRoute()
	{
		var diagnostics = await AnalyzeAsync("""
			[Route("tenants/{tenantId}/items")]
			public class ItemsController : ControllerBase
			{
				[HttpGet("/items/{id}")]
				[RbacObject("{tenantId}")]
				public IActionResult Get() => this.Ok();
			}
			""");
		
		Assert.Equal(["tenantId"], GetMissingPlaceholders(diagnostics));
	}
	
	[Fact]
	public async Task PlaceholderMissingInOneOfMultipleRoutes_ReportsDiagnostic()
	{
		var diagnostics = await AnalyzeAsync("""
			[Route("items")]
			public class ItemsController : ControllerBase
			{
				[HttpGet("{id}")]
				[HttpGet("latest")]
				[RbacObject("{id}")]
				public IActionResult Get() => this.Ok();
			}
			""");
		
		Assert.Equal(["id"], GetMissingPlaceholders(diagnostics));
	}
	
	[Fact]
	public async Task HttpMethodAttributeWithoutTemplate_CombinesWithActionRoute()
	{
		var diagnostics = await AnalyzeAsync("""
			[Route("items")]
			public class ItemsController : ControllerBase
			{
				[HttpGet]
				[Route("{id}")]
				[RbacObject("{id}")]
				public IActionResult Get() => this.Ok();
			}
			""");
		
		Assert.Empty(diagnostics);
	}
	
	[Fact]
	public async Task ActionWithoutRouteAttribute_IsSkipped()
	{
		var diagnostics = await AnalyzeAsync("""
			public class ItemsController : ControllerBase
			{
				[RbacObject("{id}")]
				public IActionResult Get() => this.Ok();
			}
			""");
		
		Assert.Empty(diagnostics);
	}
	
	[Fact]
	public async Task CustomRouteAttribute_IsSkipped()
	{
		// The template of a custom route attribute can not be resolved statically
		var diagnostics = await AnalyzeAsync("""
			public class TenantRouteAttribute() : RouteAttribute("tenants/{tenantId}");
			
			[TenantRoute]
			public class ItemsController : ControllerBase
			{
				[HttpGet("items")]
				[RbacObject("{tenantId}")]
				public IActionResult Get() => this.Ok();
			}
			""");
		
		Assert.Empty(diagnostics);
	}
	
	[Fact]
	public async Task MultipleMismatchedPlaceholders_ReportsEach()
	{
		var diagnostics = await AnalyzeAsync("""
			[Route("items")]
			public class ItemsController : ControllerBase
			{
				[HttpGet("{id}")]
				[RbacObject("{first}.{id}.{second}")]
				public IActionResult Get() => this.Ok();
			}
			""");
		
		Assert.Equal(["first", "second"], GetMissingPlaceholders(diagnostics).Order());
	}
	
	[Fact]
	public async Task RoutePrefixedPlaceholder_IsMatchedWithRouteParameters()
	{
		var diagnostics = await AnalyzeAsync("""
			[Route("items")]
			public class ItemsController : ControllerBase
			{
				[HttpGet("{id}")]
				[RbacObject("{route::id}-{route::key}")]
				public IActionResult Get() => this.Ok();
			}
			""");
		
		Assert.Equal(["key"], GetMissingPlaceholders(diagnostics));
	}
	
	[Theory]
	[InlineData("RbacSubject")]
	[InlineData("RbacResource")]
	[InlineData("RbacAction")]
	[InlineData("RbacObject")]
	public async Task AllRbacAttributes_AreAnalyzed(string attributeName)
	{
		var diagnostics = await AnalyzeAsync($$"""
			[Route("items")]
			public class ItemsController : ControllerBase
			{
				[HttpGet("{id}")]
				[{{attributeName}}("{key}")]
				public IActionResult Get() => this.Ok();
			}
			""");
		
		Assert.Equal(["key"], GetMissingPlaceholders(diagnostics));
	}
	
	[Fact]
	public async Task RbacActionWithCrudAction_NoDiagnostic()
	{
		var diagnostics = await AnalyzeAsync("""
			using ErtisAuth.Core.Models.Roles;
			
			[Route("items")]
			public class ItemsController : ControllerBase
			{
				[HttpGet("{id}")]
				[RbacAction(Rbac.CrudActions.Read)]
				public IActionResult Get() => this.Ok();
			}
			""");
		
		Assert.Empty(diagnostics);
	}
	
	[Fact]
	public async Task EachRbacAttribute_IsReportedSeparately()
	{
		var diagnostics = await AnalyzeAsync("""
			[Route("items")]
			public class ItemsController : ControllerBase
			{
				[HttpGet("{id}")]
				[RbacResource("{resource}")]
				[RbacObject("{object}")]
				public IActionResult Get() => this.Ok();
			}
			""");
		
		Assert.Equal(["object", "resource"], GetMissingPlaceholders(diagnostics).Order());
	}
	
	[Theory]
	[InlineData("{args::x}")]
	[InlineData("{anything::x}")]
	[InlineData("{ENV::X}")]
	[InlineData("{::x}")]
	[InlineData("{query::}")]
	[InlineData("{env::}")]
	public async Task InvalidPrefixedPlaceholder_ReportsUnknownSource(string objectName)
	{
		var diagnostics = await AnalyzeAsync($$"""
			[Route("items")]
			public class ItemsController : ControllerBase
			{
				[HttpGet("{id}")]
				[RbacObject("{{objectName}}")]
				public IActionResult Get(string id) => this.Ok();
			}
			""");
		
		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal(RbacPlaceholderAnalyzer.UnknownSourceDiagnosticId, diagnostic.Id);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
	}
	
	[Fact]
	public async Task InvalidPrefixedPlaceholder_IsReportedWithoutRouteAttributes()
	{
		// The source prefix does not depend on the route, so it is validated on the actions without attribute routing too
		var diagnostics = await AnalyzeAsync("""
			public class ItemsController : ControllerBase
			{
				[RbacResource("{qurey::id}")]
				public IActionResult Get() => this.Ok();
			}
			""");
		
		Assert.Equal(RbacPlaceholderAnalyzer.UnknownSourceDiagnosticId, Assert.Single(diagnostics).Id);
	}
	
	[Theory]
	[InlineData("{query::user}")]
	[InlineData("{header::X-User}")]
	[InlineData("{env::PREFIX}-{header::X-User}")]
	public async Task SubjectFromClientRequest_ReportsClientSourcedSubject(string subject)
	{
		var diagnostics = await AnalyzeAsync($$"""
			[Route("items")]
			public class ItemsController : ControllerBase
			{
				[HttpGet("{id}")]
				[RbacSubject("{{subject}}")]
				public IActionResult Get() => this.Ok();
			}
			""");
		
		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal(RbacPlaceholderAnalyzer.ClientSourcedSubjectDiagnosticId, diagnostic.Id);
		Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
	}
	
	[Theory]
	[InlineData("{env::SUBJECT}")]
	[InlineData("{route::id}")]
	[InlineData("{id}")]
	public async Task SubjectFromServerOrRoute_NoDiagnostic(string subject)
	{
		var diagnostics = await AnalyzeAsync($$"""
			[Route("items")]
			public class ItemsController : ControllerBase
			{
				[HttpGet("{id}")]
				[RbacSubject("{{subject}}")]
				public IActionResult Get() => this.Ok();
			}
			""");
		
		Assert.Empty(diagnostics);
	}
	
	[Theory]
	[InlineData("RbacResource")]
	[InlineData("RbacAction")]
	[InlineData("RbacObject")]
	public async Task ClientSourcesOnOtherAttributes_NoDiagnostic(string attributeName)
	{
		var diagnostics = await AnalyzeAsync($$"""
			[Route("items")]
			public class ItemsController : ControllerBase
			{
				[HttpGet("{id}")]
				[{{attributeName}}("{query::x}-{header::X-Name}")]
				public IActionResult Get() => this.Ok();
			}
			""");
		
		Assert.Empty(diagnostics);
	}
	
	#endregion
}
