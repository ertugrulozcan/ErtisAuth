using System.Collections.Immutable;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Extensions.AspNetCore.Attributes;
using ErtisAuth.Extensions.Authorization.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ErtisAuth.Analyzers.Tests;

public class EndpointAuthorizationAnalyzerTests
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
			.WithAnalyzers([new EndpointAuthorizationAnalyzer()])
			.GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);
	}
	
	private static string GetSourceText(Diagnostic diagnostic)
	{
		// ReSharper disable once MethodHasAsyncOverload
		return diagnostic.Location.SourceTree!.GetText(TestContext.Current.CancellationToken).ToString(diagnostic.Location.SourceSpan);
	}
	
	#endregion
	
	#region Protected Endpoints
	
	[Fact]
	public async Task AuthorizedController_NoDiagnostic()
	{
		var diagnostics = await AnalyzeAsync("""
			[Authorized]
			[RbacResource("items")]
			[Route("items")]
			public class ItemsController : ControllerBase
			{
				[HttpGet("{id}")]
				[RbacAction("read")]
				[RbacObject("{id}")]
				public IActionResult Get(string id) => this.Ok();
				
				[HttpGet("statuses")]
				[Unauthorized]
				public IActionResult Statuses() => this.Ok();
				
				[HttpGet("mine")]
				[SelfAuthorized]
				public IActionResult Mine() => this.Ok();
			}
			""");
		
		Assert.Empty(diagnostics);
	}
	
	[Fact]
	public async Task AuthorizedBaseController_NoDiagnostic()
	{
		var diagnostics = await AnalyzeAsync("""
			[Authorized]
			public abstract class SecuredControllerBase : ControllerBase
			{
			}
			
			[Route("items")]
			public class ItemsController : SecuredControllerBase
			{
				[HttpGet]
				[RbacResource("items")]
				[RbacAction("read")]
				public IActionResult List() => this.Ok();
			}
			""");
		
		Assert.Empty(diagnostics);
	}
	
	[Fact]
	public async Task ControllerWithoutRbacAttributes_NoDiagnostic()
	{
		var diagnostics = await AnalyzeAsync("""
			[Route("health")]
			public class HealthController : ControllerBase
			{
				[HttpGet]
				public IActionResult Get() => this.Ok();
			}
			""");
		
		Assert.Empty(diagnostics);
	}
	
	[Fact]
	public async Task NonActionAndNonController_NoDiagnostic()
	{
		var diagnostics = await AnalyzeAsync("""
			public class ItemsController : ControllerBase
			{
				[NonAction]
				[RbacAction("read")]
				public void Helper() { }
			}
			
			[NonController]
			public class HelperController : ControllerBase
			{
				[RbacAction("read")]
				public IActionResult Get() => this.Ok();
			}
			""");
		
		Assert.Empty(diagnostics);
	}
	
	#endregion
	
	#region Unprotected Endpoints (ERTISAUTH610)
	
	[Fact]
	public async Task RbacAttributesWithoutAuthorized_ReportsUnprotectedEndpoint()
	{
		var diagnostics = await AnalyzeAsync("""
			[Route("items")]
			public class ItemsController : ControllerBase
			{
				[HttpGet]
				[RbacAction("read")]
				public IActionResult List() => this.Ok();
			}
			""");
		
		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal(EndpointAuthorizationAnalyzer.UnprotectedEndpointDiagnosticId, diagnostic.Id);
		Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
		Assert.Contains("'List'", diagnostic.GetMessage());
		Assert.Equal("RbacAction(\"read\")", GetSourceText(diagnostic));
	}
	
	[Fact]
	public async Task ControllerRbacResourceWithoutAuthorized_ReportsTheUnprotectedActions()
	{
		var diagnostics = await AnalyzeAsync("""
			[RbacResource("items")]
			[ApiController]
			[Route("items")]
			public class Items : ControllerBase
			{
				[HttpGet]
				public IActionResult List() => this.Ok();
				
				[HttpPost]
				public IActionResult Create() => this.Ok();
				
				[HttpGet("mine")]
				[SelfAuthorized]
				public IActionResult Mine() => this.Ok();
			}
			""");
		
		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal(EndpointAuthorizationAnalyzer.UnprotectedEndpointDiagnosticId, diagnostic.Id);
		Assert.Contains("'List', 'Create'", diagnostic.GetMessage());
		Assert.Equal("RbacResource(\"items\")", GetSourceText(diagnostic));
	}
	
	[Fact]
	public async Task SelfAuthorizedActionOfUnauthorizedController_NoUnprotectedEndpoint()
	{
		// The action's attribute overrides the controller's: the endpoint requires a token
		var diagnostics = await AnalyzeAsync("""
			[Unauthorized]
			[Route("items")]
			public class ItemsController : ControllerBase
			{
				[HttpGet("mine")]
				[SelfAuthorized]
				public IActionResult Mine() => this.Ok();
			}
			""");
		
		Assert.Empty(diagnostics);
	}
	
	#endregion
	
	#region Conflicting Attributes (ERTISAUTH611)
	
	[Fact]
	public async Task ConflictingActionAttributes_ReportsConflict()
	{
		var diagnostics = await AnalyzeAsync("""
			[Authorized]
			[Route("items")]
			public class ItemsController : ControllerBase
			{
				[HttpGet]
				[SelfAuthorized]
				[Unauthorized]
				[RbacAction("read")]
				public IActionResult List() => this.Ok();
			}
			""");
		
		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal(EndpointAuthorizationAnalyzer.ConflictingAuthorizationDiagnosticId, diagnostic.Id);
		Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
		Assert.Equal("Action 'List' has conflicting authorization attributes ([SelfAuthorized], [Unauthorized]), the one declared last applies at runtime", diagnostic.GetMessage());
	}
	
	[Fact]
	public async Task ConflictingControllerAttributes_ReportsConflict()
	{
		var diagnostics = await AnalyzeAsync("""
			[Authorized]
			public abstract class SecuredControllerBase : ControllerBase
			{
			}
			
			[Unauthorized]
			[Route("items")]
			public class ItemsController : SecuredControllerBase
			{
				[HttpGet]
				public IActionResult List() => this.Ok();
			}
			""");
		
		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal(EndpointAuthorizationAnalyzer.ConflictingAuthorizationDiagnosticId, diagnostic.Id);
		Assert.StartsWith("Controller 'ItemsController'", diagnostic.GetMessage());
	}
	
	[Fact]
	public async Task SameAttributeTwice_NoConflict()
	{
		var diagnostics = await AnalyzeAsync("""
			[Authorized]
			public abstract class SecuredControllerBase : ControllerBase
			{
			}
			
			[Authorized]
			[Route("items")]
			public class ItemsController : SecuredControllerBase
			{
				[HttpGet]
				[RbacAction("read")]
				public IActionResult List() => this.Ok();
			}
			""");
		
		Assert.Empty(diagnostics);
	}
	
	#endregion
	
	#region Ignored Rbac Attributes (ERTISAUTH612)
	
	[Theory]
	[InlineData("SelfAuthorized", "self authorized ([SelfAuthorized])")]
	[InlineData("Unauthorized", "public ([Unauthorized])")]
	public async Task RbacAttributesOfSelfAuthorizedOrUnauthorizedAction_ReportsIgnoredRbac(string attribute, string reason)
	{
		var diagnostics = await AnalyzeAsync($$"""
			[Authorized]
			[RbacResource("items")]
			[Route("items")]
			public class ItemsController : ControllerBase
			{
				[HttpGet("{id}")]
				[{{attribute}}]
				[RbacObject("{id}")]
				public IActionResult Get(string id) => this.Ok();
			}
			""");
		
		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal(EndpointAuthorizationAnalyzer.IgnoredRbacDiagnosticId, diagnostic.Id);
		Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
		Assert.Equal($"Rbac attributes of action 'Get' are not checked, the endpoint is {reason}", diagnostic.GetMessage());
	}
	
	[Fact]
	public async Task RbacAttributesOfActionOfUnauthorizedController_ReportsIgnoredRbac()
	{
		var diagnostics = await AnalyzeAsync("""
			[Unauthorized]
			[Route("items")]
			public class ItemsController : ControllerBase
			{
				[HttpGet]
				[RbacAction("read")]
				public IActionResult List() => this.Ok();
			}
			""");
		
		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal(EndpointAuthorizationAnalyzer.IgnoredRbacDiagnosticId, diagnostic.Id);
	}
	
	#endregion
}
