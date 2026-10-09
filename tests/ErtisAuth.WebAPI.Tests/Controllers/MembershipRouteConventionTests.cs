using System.Reflection;
using System.Text.RegularExpressions;
using ErtisAuth.Extensions.AspNetCore.Attributes;
using ErtisAuth.WebAPI.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace ErtisAuth.WebAPI.Tests.Controllers;

/// <summary>
/// Membership bounded endpoints must be routed with MembershipRouteAttribute, because the authentication handler
/// relies on it to ensure that the token belongs to the membership in the route.
/// </summary>
public class MembershipRouteConventionTests
{
	#region Constants
	
	// memberships/{anything} optionally followed by more segments
	private static readonly Regex MembershipScopedTemplate = new(@"^memberships/\{[^}]+\}(/|$)", RegexOptions.IgnoreCase);
	
	// MembershipsController manages memberships themselves and is intentionally not membership bounded
	private static readonly Type[] ExcludedControllers = [typeof(MembershipsController)];
	
	#endregion
	
	#region Helpers
	
	private static IEnumerable<Type> GetControllerTypes()
	{
		return typeof(MembershipsController).Assembly
			.GetTypes()
			.Where(x => x is { IsClass: true, IsAbstract: false } && typeof(ControllerBase).IsAssignableFrom(x));
	}
	
	private static IEnumerable<string> GetRouteTemplates(Type controllerType)
	{
		var controllerTemplates = controllerType
			.GetCustomAttributes(inherit: true)
			.OfType<IRouteTemplateProvider>()
			.Select(x => x.Template)
			.ToArray();
			
		if (controllerTemplates.Length == 0)
		{
			controllerTemplates = [null];
		}
		
		var actionTemplates = controllerType
			.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
			.SelectMany(x => x.GetCustomAttributes(inherit: true).OfType<IRouteTemplateProvider>())
			.Select(x => x.Template)
			.DefaultIfEmpty(null)
			.ToArray();
			
		foreach (var controllerTemplate in controllerTemplates)
		{
			foreach (var actionTemplate in actionTemplates)
			{
				yield return Combine(controllerTemplate, actionTemplate);
			}
		}
	}
	
	private static string Combine(string? controllerTemplate, string? actionTemplate)
	{
		if (!string.IsNullOrEmpty(actionTemplate) && (actionTemplate.StartsWith('/') || actionTemplate.StartsWith("~/")))
		{
			return actionTemplate.TrimStart('~', '/');
		}
		
		var segments = new[] { controllerTemplate, actionTemplate }.Where(x => !string.IsNullOrEmpty(x)).Select(x => x!.Trim('/'));
		return string.Join('/', segments);
	}
	
	#endregion
	
	#region Tests
	
	[Fact]
	public void MembershipScopedRoutes_AreDeclaredWithMembershipRouteAttribute()
	{
		var violations = GetControllerTypes()
			.Where(x => !ExcludedControllers.Contains(x))
			.Where(x => x.GetCustomAttribute<MembershipRouteAttribute>() == null)
			.SelectMany(x => GetRouteTemplates(x).Where(template => MembershipScopedTemplate.IsMatch(template)).Select(template => $"{x.Name}: {template}"))
			.ToArray();
			
		Assert.Empty(violations);
	}
	
	[Fact]
	public void MembershipRouteControllers_HaveNoOtherControllerLevelRoutes()
	{
		var violations = GetControllerTypes()
			.Where(x => x.GetCustomAttribute<MembershipRouteAttribute>() != null)
			.Where(x => x.GetCustomAttributes(inherit: true).OfType<IRouteTemplateProvider>().Count() > 1)
			.Select(x => x.Name)
			.ToArray();
			
		Assert.Empty(violations);
	}
	
	[Fact]
	public void MembershipRouteControllers_HaveNoAbsoluteActionRoutes()
	{
		// An absolute action route ("/..." or "~/...") would bypass the membership prefix of the controller.
		var violations = GetControllerTypes()
			.Where(x => x.GetCustomAttribute<MembershipRouteAttribute>() != null)
			.SelectMany(x => x
				.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
				.SelectMany(method => method.GetCustomAttributes(inherit: true).OfType<IRouteTemplateProvider>())
				.Where(provider => provider.Template != null && (provider.Template.StartsWith('/') || provider.Template.StartsWith("~/")))
				.Select(provider => $"{x.Name}: {provider.Template}"))
			.ToArray();
			
		Assert.Empty(violations);
	}
	
	[Fact]
	public void MembershipRouteAttribute_ProducesMembershipScopedTemplate()
	{
		var attribute = new MembershipRouteAttribute("users");
		
		Assert.Equal("memberships/{membershipId}/users", attribute.Template);
		Assert.Equal("membershipId", MembershipRouteAttribute.ParameterName);
	}
	
	[Fact]
	public void MembershipBoundedControllers_UseMembershipRouteAttribute()
	{
		// Guards against the convention test passing vacuously.
		var controllers = GetControllerTypes().Where(x => x.GetCustomAttribute<MembershipRouteAttribute>() != null).Select(x => x.Name).ToArray();
		
		Assert.Equal(12, controllers.Length);
		Assert.Contains(nameof(UsersController), controllers);
		Assert.Contains(nameof(ApplicationsController), controllers);
		Assert.DoesNotContain(nameof(MembershipsController), controllers);
	}
	
	#endregion
}
