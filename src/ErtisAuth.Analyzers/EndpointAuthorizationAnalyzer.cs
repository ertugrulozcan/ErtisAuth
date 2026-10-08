using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ErtisAuth.Analyzers;

/// <summary>
/// Checks how the actions of the controllers are authorized, as ErtisAuth.Extensions.Authorization.Extensions.EndpointAuthorizationExtensions
/// decides at runtime: the most specific of [Authorized], [SelfAuthorized] and [Unauthorized] applies, an action's attribute
/// overrides its controller's. Only the actions declared in a controller are analyzed; the controller level attributes are
/// collected from the controller and its base classes. Minimal API endpoints get their attributes at runtime and are not analyzed.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EndpointAuthorizationAnalyzer : DiagnosticAnalyzer
{
	#region Constants
	
	public const string UnprotectedEndpointDiagnosticId = "ERTISAUTH610";
	public const string ConflictingAuthorizationDiagnosticId = "ERTISAUTH611";
	public const string IgnoredRbacDiagnosticId = "ERTISAUTH612";
	
	private const string AttributesNamespace = "ErtisAuth.Extensions.Authorization.Attributes";
	private const string AuthorizedAttributeName = AttributesNamespace + ".AuthorizedAttribute";
	private const string SelfAuthorizedAttributeName = AttributesNamespace + ".SelfAuthorizedAttribute";
	private const string UnauthorizedAttributeName = AttributesNamespace + ".UnauthorizedAttribute";
	private const string RbacResourceAttributeName = AttributesNamespace + ".RbacResourceAttribute";
	private const string MvcNamespace = "Microsoft.AspNetCore.Mvc";
	private const string ControllerAttributeName = MvcNamespace + ".ControllerAttribute";
	private const string NonControllerAttributeName = MvcNamespace + ".NonControllerAttribute";
	private const string NonActionAttributeName = MvcNamespace + ".NonActionAttribute";
	
	private static readonly string[] RbacAttributeNames =
	[
		AttributesNamespace + ".RbacSubjectAttribute",
		RbacResourceAttributeName,
		AttributesNamespace + ".RbacActionAttribute",
		AttributesNamespace + ".RbacObjectAttribute"
	];
	
	private static readonly DiagnosticDescriptor UnprotectedEndpointRule = new(
		UnprotectedEndpointDiagnosticId,
		"Rbac attributes on an endpoint which is not protected",
		"Action '{0}' has rbac attributes but neither the action nor its controller has [Authorized] or [SelfAuthorized], the endpoint is public",
		"Security",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "The rbac attributes are only checked on the endpoints of an [Authorized] controller. Without [Authorized] or [SelfAuthorized] the endpoint is not authenticated at all.");
	
	private static readonly DiagnosticDescriptor ConflictingAuthorizationRule = new(
		ConflictingAuthorizationDiagnosticId,
		"Conflicting authorization attributes",
		"{0} '{1}' has conflicting authorization attributes ({2}), the one declared last applies at runtime",
		"Usage",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "[Authorized], [SelfAuthorized] and [Unauthorized] exclude each other on the same level (the controller with its base classes, or the action). Which of them applies depends on the order of the attributes.");
	
	private static readonly DiagnosticDescriptor IgnoredRbacRule = new(
		IgnoredRbacDiagnosticId,
		"Rbac attributes are not checked",
		"Rbac attributes of action '{0}' are not checked, the endpoint is {1}",
		"Usage",
		DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		description: "A [SelfAuthorized] endpoint checks the permission itself, an [Unauthorized] endpoint is public: the rbac attributes of their actions have no effect.");
	
	#endregion
	
	#region Enums
	
	private enum Authorization
	{
		None,
		Authorized,
		SelfAuthorized,
		Unauthorized,
		Conflicting
	}
	
	#endregion
	
	#region Properties
	
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(UnprotectedEndpointRule, ConflictingAuthorizationRule, IgnoredRbacRule);
	
	#endregion
	
	#region Methods
	
	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(compilationContext =>
		{
			var compilation = compilationContext.Compilation;
			var authorizationTypes = new Dictionary<Authorization, INamedTypeSymbol?>
			{
				[Authorization.Authorized] = compilation.GetTypeByMetadataName(AuthorizedAttributeName),
				[Authorization.SelfAuthorized] = compilation.GetTypeByMetadataName(SelfAuthorizedAttributeName),
				[Authorization.Unauthorized] = compilation.GetTypeByMetadataName(UnauthorizedAttributeName)
			};
			
			if (authorizationTypes.Values.Any(x => x == null))
			{
				return;
			}
			
			var symbols = new Symbols(
				authorizationTypes.ToImmutableDictionary(x => x.Key, x => x.Value!),
				RbacAttributeNames.Select(x => compilation.GetTypeByMetadataName(x)).Where(x => x != null).Cast<INamedTypeSymbol>().ToImmutableArray(),
				compilation.GetTypeByMetadataName(RbacResourceAttributeName),
				compilation.GetTypeByMetadataName(ControllerAttributeName),
				compilation.GetTypeByMetadataName(NonControllerAttributeName),
				compilation.GetTypeByMetadataName(NonActionAttributeName));
			
			compilationContext.RegisterSymbolAction(symbolContext => AnalyzeController(symbolContext, symbols), SymbolKind.NamedType);
		});
	}
	
	private static void AnalyzeController(SymbolAnalysisContext context, Symbols symbols)
	{
		var type = (INamedTypeSymbol)context.Symbol;
		if (!IsController(type, symbols))
		{
			return;
		}
		
		// The controller level: the controller and its base classes
		var controllerAttributes = new List<AttributeData>();
		for (var current = type; current != null && !IsFrameworkType(current); current = current.BaseType)
		{
			controllerAttributes.AddRange(current.GetAttributes());
		}
		
		var controllerAuthorization = GetAuthorization(controllerAttributes, symbols, out var controllerKinds);
		if (controllerAuthorization == Authorization.Conflicting)
		{
			context.ReportDiagnostic(Diagnostic.Create(ConflictingAuthorizationRule, type.Locations.FirstOrDefault(), "Controller", type.Name, FormatKinds(controllerKinds)));
		}
		
		var unprotectedActionsWithoutRbac = new List<string>();
		foreach (var method in type.GetMembers().OfType<IMethodSymbol>().Where(x => IsAction(x, symbols)))
		{
			var methodAttributes = method.GetAttributes();
			var actionAuthorization = GetAuthorization(methodAttributes, symbols, out var actionKinds);
			if (actionAuthorization == Authorization.Conflicting)
			{
				context.ReportDiagnostic(Diagnostic.Create(ConflictingAuthorizationRule, method.Locations.FirstOrDefault(), "Action", method.Name, FormatKinds(actionKinds)));
				continue;
			}
			
			var authorization = actionAuthorization != Authorization.None ? actionAuthorization : controllerAuthorization;
			if (authorization == Authorization.Conflicting)
			{
				continue;
			}
			
			var rbacAttribute = methodAttributes.FirstOrDefault(x => IsRbacAttribute(x, symbols));
			if (authorization == Authorization.None)
			{
				if (rbacAttribute != null)
				{
					context.ReportDiagnostic(Diagnostic.Create(UnprotectedEndpointRule, GetLocation(context, rbacAttribute, method), method.Name));
				}
				else
				{
					unprotectedActionsWithoutRbac.Add(method.Name);
				}
			}
			else if (rbacAttribute != null && authorization is Authorization.SelfAuthorized or Authorization.Unauthorized)
			{
				var reason = authorization == Authorization.SelfAuthorized ? "self authorized ([SelfAuthorized])" : "public ([Unauthorized])";
				context.ReportDiagnostic(Diagnostic.Create(IgnoredRbacRule, GetLocation(context, rbacAttribute, method), method.Name, reason));
			}
		}
		
		// An [RbacResource] of the controller itself which is not checked for some of its actions
		var controllerRbacResource = symbols.RbacResourceAttributeType == null ? null : type.GetAttributes().FirstOrDefault(x => InheritsFrom(x.AttributeClass, symbols.RbacResourceAttributeType));
		if (controllerRbacResource != null && unprotectedActionsWithoutRbac.Count > 0)
		{
			context.ReportDiagnostic(Diagnostic.Create(UnprotectedEndpointRule, GetLocation(context, controllerRbacResource, type), string.Join("', '", unprotectedActionsWithoutRbac)));
		}
	}
	
	/// <summary>
	/// The authorization the attributes of one level declare. The runtime compares the exact attribute types.
	/// </summary>
	private static Authorization GetAuthorization(IEnumerable<AttributeData> attributes, Symbols symbols, out List<Authorization> kinds)
	{
		kinds = attributes
			.Select(x => symbols.AuthorizationTypes.FirstOrDefault(y => SymbolEqualityComparer.Default.Equals(x.AttributeClass, y.Value)))
			.Where(x => x.Value != null)
			.Select(x => x.Key)
			.Distinct()
			.ToList();
		
		return kinds.Count switch
		{
			0 => Authorization.None,
			1 => kinds[0],
			_ => Authorization.Conflicting
		};
	}
	
	private static string FormatKinds(IEnumerable<Authorization> kinds)
	{
		return string.Join(", ", kinds.Select(x => $"[{x}]"));
	}
	
	private static bool IsRbacAttribute(AttributeData attribute, Symbols symbols)
	{
		return symbols.RbacAttributeTypes.Any(x => InheritsFrom(attribute.AttributeClass, x));
	}
	
	/// <summary>
	/// The controller discovery of ASP.NET Core: a public, non-abstract, non-generic class named *Controller or with [Controller] (e.g. [ApiController]), without [NonController].
	/// </summary>
	private static bool IsController(INamedTypeSymbol type, Symbols symbols)
	{
		if (type.TypeKind != TypeKind.Class || type.IsAbstract || type.IsStatic || type.IsGenericType || type.DeclaredAccessibility != Accessibility.Public)
		{
			return false;
		}
		
		var hasControllerAttribute = false;
		for (var current = type; current != null; current = current.BaseType)
		{
			foreach (var attribute in current.GetAttributes())
			{
				if (symbols.NonControllerAttributeType != null && SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, symbols.NonControllerAttributeType))
				{
					return false;
				}
				
				if (symbols.ControllerAttributeType != null && InheritsFrom(attribute.AttributeClass, symbols.ControllerAttributeType))
				{
					hasControllerAttribute = true;
				}
			}
		}
		
		return hasControllerAttribute || type.Name.EndsWith("Controller", System.StringComparison.Ordinal);
	}
	
	private static bool IsAction(IMethodSymbol method, Symbols symbols)
	{
		if (method.MethodKind != MethodKind.Ordinary || method.IsStatic || method.IsAbstract || method.IsGenericMethod || method.DeclaredAccessibility != Accessibility.Public)
		{
			return false;
		}
		
		return symbols.NonActionAttributeType == null || !method.GetAttributes().Any(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, symbols.NonActionAttributeType));
	}
	
	/// <summary>
	/// ControllerBase, Controller and object declare no ErtisAuth attributes
	/// </summary>
	private static bool IsFrameworkType(INamedTypeSymbol type)
	{
		return type.SpecialType == SpecialType.System_Object || type.ContainingNamespace?.ToDisplayString() == MvcNamespace;
	}
	
	private static bool InheritsFrom(INamedTypeSymbol? type, INamedTypeSymbol baseType)
	{
		for (var current = type; current != null; current = current.BaseType)
		{
			if (SymbolEqualityComparer.Default.Equals(current, baseType))
			{
				return true;
			}
		}
		
		return false;
	}
	
	private static Location? GetLocation(SymbolAnalysisContext context, AttributeData attribute, ISymbol symbol)
	{
		return attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ?? symbol.Locations.FirstOrDefault();
	}
	
	#endregion
	
	#region Helper Classes
	
	private sealed class Symbols(
		ImmutableDictionary<Authorization, INamedTypeSymbol> authorizationTypes,
		ImmutableArray<INamedTypeSymbol> rbacAttributeTypes,
		INamedTypeSymbol? rbacResourceAttributeType,
		INamedTypeSymbol? controllerAttributeType,
		INamedTypeSymbol? nonControllerAttributeType,
		INamedTypeSymbol? nonActionAttributeType)
	{
		public ImmutableDictionary<Authorization, INamedTypeSymbol> AuthorizationTypes { get; } = authorizationTypes;
		
		public ImmutableArray<INamedTypeSymbol> RbacAttributeTypes { get; } = rbacAttributeTypes;
		
		public INamedTypeSymbol? RbacResourceAttributeType { get; } = rbacResourceAttributeType;
		
		public INamedTypeSymbol? ControllerAttributeType { get; } = controllerAttributeType;
		
		public INamedTypeSymbol? NonControllerAttributeType { get; } = nonControllerAttributeType;
		
		public INamedTypeSymbol? NonActionAttributeType { get; } = nonActionAttributeType;
	}
	
	#endregion
}
