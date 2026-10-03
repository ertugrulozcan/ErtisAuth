using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ErtisAuth.Analyzers;

/// <summary>
/// Validates the placeholders of the rbac attributes (RbacSubject, RbacResource, RbacAction, RbacObject).
/// A placeholder names its source with a prefix ({query::id}, {header::X-Name}, {env::NAME}), the placeholders without a prefix
/// and the {route::id} placeholders are resolved from the route values at runtime.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RbacPlaceholderAnalyzer : DiagnosticAnalyzer
{
	#region Constants
	
	public const string RouteParameterDiagnosticId = "ERTISAUTH601";
	public const string UnknownSourceDiagnosticId = "ERTISAUTH602";
	public const string ClientSourcedSubjectDiagnosticId = "ERTISAUTH603";
	
	private const string AttributesNamespace = "ErtisAuth.Extensions.Authorization.Attributes";
	private const string RbacSubjectAttributeName = AttributesNamespace + ".RbacSubjectAttribute";
	private const string MembershipRouteAttributeName = "ErtisAuth.Extensions.AspNetCore.Attributes.MembershipRouteAttribute";
	private const string RouteTemplateProviderName = "Microsoft.AspNetCore.Mvc.Routing.IRouteTemplateProvider";
	private const string MvcNamespace = "Microsoft.AspNetCore.Mvc";
	
	// Keep in sync with ErtisAuth.Extensions.Authorization.RbacSegmentResolver
	private const string SourceSeparator = "::";
	private const string RouteSource = "route";
	private const string QuerySource = "query";
	private const string HeaderSource = "header";
	private const string EnvironmentVariableSource = "env";
	
	private static readonly string[] RbacAttributeNames =
	[
		RbacSubjectAttributeName,
		AttributesNamespace + ".RbacResourceAttribute",
		AttributesNamespace + ".RbacActionAttribute",
		AttributesNamespace + ".RbacObjectAttribute"
	];
	
	private static readonly Regex PlaceholderRegex = new(@"\{([^{}]+)\}", RegexOptions.Compiled);
	
	private static readonly DiagnosticDescriptor RouteParameterRule = new(
		RouteParameterDiagnosticId,
		"Rbac placeholder is not a route parameter",
		"Rbac placeholder '{0}' is not a route parameter of action '{1}'",
		"Usage",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "Rbac placeholders without a source prefix are resolved from the route values, so each of them must match a route parameter of the action.");
	
	private static readonly DiagnosticDescriptor UnknownSourceRule = new(
		UnknownSourceDiagnosticId,
		"Rbac placeholder has an unknown source",
		"Rbac placeholder '{0}' is not valid, the known sources are route, query, header and env",
		"Usage",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "A prefixed rbac placeholder must name a known source and a value, like {query::id}. An invalid placeholder denies the access at runtime.");
	
	private static readonly DiagnosticDescriptor ClientSourcedSubjectRule = new(
		ClientSourcedSubjectDiagnosticId,
		"Rbac subject can not be read from the client request",
		"RbacSubject placeholder '{0}' reads the subject from the client request",
		"Security",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "The subject is the identity which is authorized, a subject read from the query string or the headers would be chosen by the client. Such a placeholder denies the access at runtime.");
	
	#endregion
	
	#region Properties
	
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RouteParameterRule, UnknownSourceRule, ClientSourcedSubjectRule);
	
	#endregion
	
	#region Methods
	
	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(compilationContext =>
		{
			var rbacAttributeTypes = RbacAttributeNames
				.Select(x => compilationContext.Compilation.GetTypeByMetadataName(x))
				.Where(x => x != null)
				.Cast<INamedTypeSymbol>()
				.ToImmutableArray();
			
			if (rbacAttributeTypes.Length == 0)
			{
				return;
			}
			
			var routeTemplateProviderType = compilationContext.Compilation.GetTypeByMetadataName(RouteTemplateProviderName);
			compilationContext.RegisterSymbolAction(symbolContext => AnalyzeMethod(symbolContext, rbacAttributeTypes, routeTemplateProviderType), SymbolKind.Method);
		});
	}
	
	private static void AnalyzeMethod(SymbolAnalysisContext context, ImmutableArray<INamedTypeSymbol> rbacAttributeTypes, INamedTypeSymbol? routeTemplateProviderType)
	{
		var method = (IMethodSymbol)context.Symbol;
		var routePlaceholdersByAttribute = new List<(AttributeData Attribute, List<string> Placeholders)>();
		foreach (var attribute in method.GetAttributes())
		{
			var attributeClass = attribute.AttributeClass;
			if (attributeClass == null || !rbacAttributeTypes.Contains(attributeClass, SymbolEqualityComparer.Default))
			{
				continue;
			}

			// RbacAction(CrudActions) has no placeholders
			if (attribute.ConstructorArguments.Length == 0 || attribute.ConstructorArguments[0].Value is not string segmentValue)
			{
				continue;
			}

			var isSubject = attributeClass.ToDisplayString() == RbacSubjectAttributeName;
			var location = GetLocation(context, attribute, method);
			var routePlaceholders = new List<string>();
			foreach (var placeholder in GetPlaceholders(segmentValue))
			{
				var separatorIndex = placeholder.IndexOf(SourceSeparator, StringComparison.Ordinal);
				if (separatorIndex < 0)
				{
					routePlaceholders.Add(placeholder);
					continue;
				}
				
				var source = placeholder.Substring(0, separatorIndex);
				var name = placeholder.Substring(separatorIndex + SourceSeparator.Length);
				if (name.Length == 0 || source is not (RouteSource or QuerySource or HeaderSource or EnvironmentVariableSource))
				{
					context.ReportDiagnostic(Diagnostic.Create(UnknownSourceRule, location, placeholder));
				}
				else if (isSubject && source is QuerySource or HeaderSource)
				{
					context.ReportDiagnostic(Diagnostic.Create(ClientSourcedSubjectRule, location, placeholder));
				}
				else if (source == RouteSource)
				{
					routePlaceholders.Add(name);
				}
			}
			
			var distinctRoutePlaceholders = routePlaceholders.Distinct(StringComparer.Ordinal).ToList();
			if (distinctRoutePlaceholders.Count > 0)
			{
				routePlaceholdersByAttribute.Add((attribute, distinctRoutePlaceholders));
			}
		}
		
		if (routePlaceholdersByAttribute.Count == 0 || routeTemplateProviderType == null)
		{
			return;
		}
		
		var routeParameterSets = GetRouteParameterSets(method, routeTemplateProviderType);
		if (routeParameterSets == null)
		{
			return;
		}
		
		foreach (var (attribute, placeholders) in routePlaceholdersByAttribute)
		{
			// Every route of the action produces a separate endpoint, so the placeholder must exist in all of them
			var missingPlaceholders = placeholders.Where(placeholder => routeParameterSets.Any(x => !x.Contains(placeholder))).ToList();
			var location = GetLocation(context, attribute, method);
			foreach (var placeholder in missingPlaceholders)
			{
				context.ReportDiagnostic(Diagnostic.Create(RouteParameterRule, location, placeholder, method.Name));
			}
		}
	}
	
	private static Location? GetLocation(SymbolAnalysisContext context, AttributeData attribute, IMethodSymbol method)
	{
		return attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ?? method.Locations.FirstOrDefault();
	}
	
	/// <summary>
	/// The route parameters of each route of the action. Returns null when the routes can not be known statically.
	/// </summary>
	private static List<HashSet<string>>? GetRouteParameterSets(IMethodSymbol method, INamedTypeSymbol routeTemplateProviderType)
	{
		// No route attribute on the action means it is not attribute routed, the route can not be known
		if (!TryGetRouteTemplates(method.GetAttributes(), routeTemplateProviderType, out var actionTemplates) || actionTemplates.Count == 0)
		{
			return null;
		}
		
		var controllerAttributes = new List<AttributeData>();
		for (var type = method.ContainingType; type != null; type = type.BaseType)
		{
			controllerAttributes.AddRange(type.GetAttributes());
		}
		
		if (!TryGetRouteTemplates(controllerAttributes, routeTemplateProviderType, out var controllerTemplates))
		{
			return null;
		}
		
		// Explicit action templates override the bare http method attributes like [HttpGet]
		var explicitActionTemplates = actionTemplates.Where(x => x != null).ToList();
		if (explicitActionTemplates.Count > 0)
		{
			actionTemplates = explicitActionTemplates;
		}
		
		if (controllerTemplates.Count == 0)
		{
			controllerTemplates.Add(null);
		}
		
		var routeParameterSets = new List<HashSet<string>>();
		foreach (var actionTemplate in actionTemplates)
		{
			var isAbsolute = actionTemplate != null && (actionTemplate.StartsWith("/", StringComparison.Ordinal) || actionTemplate.StartsWith("~/", StringComparison.Ordinal));
			foreach (var controllerTemplate in isAbsolute ? [null] : controllerTemplates)
			{
				routeParameterSets.Add(new HashSet<string>(GetRouteParameters(controllerTemplate).Concat(GetRouteParameters(actionTemplate)), StringComparer.Ordinal));
			}
		}
		
		return routeParameterSets;
	}
	
	private static List<string> GetPlaceholders(string segment)
	{
		return PlaceholderRegex.Matches(segment)
			.Cast<Match>()
			.Select(x => x.Groups[1].Value.Trim())
			.Where(x => x.Length > 0)
			.Distinct(StringComparer.Ordinal)
			.ToList();
	}
	
	private static IEnumerable<string> GetRouteParameters(string? template)
	{
		if (string.IsNullOrEmpty(template))
		{
			yield break;
		}
		
		// Escaped braces ({{ and }}) are literals in route templates
		var unescapedTemplate = template!.Replace("{{", string.Empty).Replace("}}", string.Empty);
		foreach (Match match in PlaceholderRegex.Matches(unescapedTemplate))
		{
			// {*slug}, {id:int}, {id?}, {id=default}
			var parameter = match.Groups[1].Value.TrimStart('*');
			var end = parameter.IndexOfAny([':', '=', '?']);
			if (end >= 0)
			{
				parameter = parameter.Substring(0, end);
			}
			
			parameter = parameter.Trim();
			if (parameter.Length > 0)
			{
				yield return parameter;
			}
		}
	}
	
	/// <summary>
	/// Collects the templates of the route attributes. Returns false when the template of a route attribute can not be resolved statically.
	/// </summary>
	private static bool TryGetRouteTemplates(IEnumerable<AttributeData> attributes, INamedTypeSymbol routeTemplateProviderType, out List<string?> templates)
	{
		templates = [];
		foreach (var attribute in attributes)
		{
			var attributeClass = attribute.AttributeClass;
			if (attributeClass == null || !attributeClass.AllInterfaces.Contains(routeTemplateProviderType, SymbolEqualityComparer.Default))
			{
				continue;
			}
			
			var templateArgument = attribute.ConstructorArguments.Length > 0 ? attribute.ConstructorArguments[0].Value as string : null;
			if (attributeClass.ContainingNamespace?.ToDisplayString() == MvcNamespace)
			{
				templates.Add(templateArgument);
			}
			else if (attributeClass.ToDisplayString() == MembershipRouteAttributeName && TryGetMembershipRouteTemplate(attributeClass, templateArgument, out var membershipRouteTemplate))
			{
				templates.Add(membershipRouteTemplate);
			}
			else
			{
				// A custom route attribute may build its template in any way, avoid false warnings
				return false;
			}
		}
		
		return true;
	}
	
	private static bool TryGetMembershipRouteTemplate(INamedTypeSymbol attributeClass, string? templateArgument, out string? template)
	{
		var parameterName = attributeClass.GetMembers("ParameterName").OfType<IFieldSymbol>().FirstOrDefault(x => x.HasConstantValue)?.ConstantValue as string;
		if (string.IsNullOrEmpty(parameterName))
		{
			template = null;
			return false;
		}
		
		template = $"memberships/{{{parameterName}}}/{templateArgument}";
		return true;
	}
	
	#endregion
}
