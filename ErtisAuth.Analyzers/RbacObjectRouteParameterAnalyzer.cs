using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ErtisAuth.Analyzers;

/// <summary>
/// Reports RbacObject placeholders which are not a route parameter of the action.
/// The placeholders are resolved from the route values at runtime, so a placeholder missing in the route template
/// is never replaced and the permission check runs against a wrong object.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RbacObjectRouteParameterAnalyzer : DiagnosticAnalyzer
{
	#region Constants
	
	public const string DiagnosticId = "ERTISAUTH001";
	
	private const string RbacObjectAttributeName = "ErtisAuth.Extensions.Authorization.Attributes.RbacObjectAttribute";
	private const string MembershipRouteAttributeName = "ErtisAuth.Extensions.AspNetCore.Attributes.MembershipRouteAttribute";
	private const string RouteTemplateProviderName = "Microsoft.AspNetCore.Mvc.Routing.IRouteTemplateProvider";
	private const string MvcNamespace = "Microsoft.AspNetCore.Mvc";
	
	// Placeholders like {env::NAME} are resolved from other sources than the route values
	private const string SourcePrefixSeparator = "::";
	
	private static readonly Regex PlaceholderRegex = new(@"\{([^{}]+)\}", RegexOptions.Compiled);
	
	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticId,
		"RbacObject placeholder is not a route parameter",
		"RbacObject placeholder '{0}' is not a route parameter of action '{1}'",
		"Usage",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "RbacObject placeholders are resolved from the route values, so each placeholder must match a route parameter of the action.");
	
	#endregion
	
	#region Properties
	
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);
	
	#endregion
	
	#region Methods
	
	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(compilationContext =>
		{
			var rbacObjectAttributeType = compilationContext.Compilation.GetTypeByMetadataName(RbacObjectAttributeName);
			var routeTemplateProviderType = compilationContext.Compilation.GetTypeByMetadataName(RouteTemplateProviderName);
			if (rbacObjectAttributeType == null || routeTemplateProviderType == null)
			{
				return;
			}
			
			compilationContext.RegisterSymbolAction(symbolContext => AnalyzeMethod(symbolContext, rbacObjectAttributeType, routeTemplateProviderType), SymbolKind.Method);
		});
	}
	
	private static void AnalyzeMethod(SymbolAnalysisContext context, INamedTypeSymbol rbacObjectAttributeType, INamedTypeSymbol routeTemplateProviderType)
	{
		var method = (IMethodSymbol)context.Symbol;
		var rbacObjectAttribute = method.GetAttributes().FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, rbacObjectAttributeType));
		if (rbacObjectAttribute == null || rbacObjectAttribute.ConstructorArguments.Length == 0 || rbacObjectAttribute.ConstructorArguments[0].Value is not string objectName)
		{
			return;
		}
		
		var placeholders = GetPlaceholders(objectName);
		if (placeholders.Count == 0)
		{
			return;
		}
		
		// No route attribute on the action means it is not attribute routed, the route can not be known
		if (!TryGetRouteTemplates(method.GetAttributes(), routeTemplateProviderType, out var actionTemplates) || actionTemplates.Count == 0)
		{
			return;
		}
		
		var controllerAttributes = new List<AttributeData>();
		for (var type = method.ContainingType; type != null; type = type.BaseType)
		{
			controllerAttributes.AddRange(type.GetAttributes());
		}
		
		if (!TryGetRouteTemplates(controllerAttributes, routeTemplateProviderType, out var controllerTemplates))
		{
			return;
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
		
		// Every route of the action produces a separate endpoint, so the placeholder must exist in all of them
		var missingPlaceholders = new List<string>();
		foreach (var actionTemplate in actionTemplates)
		{
			var isAbsolute = actionTemplate != null && (actionTemplate.StartsWith("/", StringComparison.Ordinal) || actionTemplate.StartsWith("~/", StringComparison.Ordinal));
			foreach (var controllerTemplate in isAbsolute ? [null] : controllerTemplates)
			{
				var routeParameters = new HashSet<string>(GetRouteParameters(controllerTemplate).Concat(GetRouteParameters(actionTemplate)), StringComparer.OrdinalIgnoreCase);
				foreach (var placeholder in placeholders)
				{
					if (!routeParameters.Contains(placeholder) && !missingPlaceholders.Contains(placeholder, StringComparer.OrdinalIgnoreCase))
					{
						missingPlaceholders.Add(placeholder);
					}
				}
			}
		}
		
		if (missingPlaceholders.Count == 0)
		{
			return;
		}
		
		var location = rbacObjectAttribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ?? method.Locations.FirstOrDefault();
		foreach (var placeholder in missingPlaceholders)
		{
			context.ReportDiagnostic(Diagnostic.Create(Rule, location, placeholder, method.Name));
		}
	}
	
	private static List<string> GetPlaceholders(string segment)
	{
		return PlaceholderRegex.Matches(segment)
			.Cast<Match>()
			.Select(x => x.Groups[1].Value.Trim())
			.Where(x => x.Length > 0 && x.IndexOf(SourcePrefixSeparator, StringComparison.Ordinal) < 0)
			.Distinct(StringComparer.OrdinalIgnoreCase)
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
