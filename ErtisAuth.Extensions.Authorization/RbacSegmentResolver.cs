using System.Text;
using Ertis.TemplateEngine;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Roles;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace ErtisAuth.Extensions.Authorization;

/// <summary>
/// Resolves the placeholders of the rbac attribute values from the request and the environment.
/// A placeholder names its source with a prefix: {id} and {route::id} are read from the route values,
/// {query::id} from the query string, {header::X-Name} from the request headers and {env::NAME} from the environment variables.
/// </summary>
public static class RbacSegmentResolver
{
	#region Constants
	
	private const string SourceSeparator = "::";
	private const string RouteSource = "route";
	private const string QuerySource = "query";
	private const string HeaderSource = "header";
	private const string EnvironmentVariableSource = "env";
	private const string EncodedSeparator = "%2E";
	
	#endregion
	
	#region Fields
	
	private static readonly Formatter Formatter = new(new ParserOptions { OpenBrackets = "{", CloseBrackets = "}" });
	
	#endregion
	
	#region Methods
	
	/// <summary>
	/// Replaces the placeholders of the segment value with their values.
	/// </summary>
	/// <param name="segmentValue"></param>
	/// <param name="httpContext"></param>
	/// <param name="isSubject">The subject can not be read from the sources which are sent by the client (query and header)</param>
	/// <exception cref="ErtisAuthException">Access denied when a placeholder other than a route placeholder can not be resolved, or a resolved value is '*', contains '%2E', whitespace or control characters</exception>
	public static string Resolve(string segmentValue, HttpContext httpContext, bool isSubject = false)
	{
		var stringBuilder = new StringBuilder();
		foreach (var segment in Formatter.LookUp(segmentValue))
		{
			if (segment is PlaceHolder placeHolder)
			{
				stringBuilder.Append(ResolvePlaceHolder(placeHolder, httpContext, isSubject));
			}
			else
			{
				stringBuilder.Append(segment);
			}
		}
		
		return stringBuilder.ToString();
	}
	
	private static string ResolvePlaceHolder(PlaceHolder placeHolder, HttpContext httpContext, bool isSubject)
	{
		var inner = placeHolder.Value ?? string.Empty;
		var separatorIndex = inner.IndexOf(SourceSeparator, StringComparison.Ordinal);
		if (separatorIndex < 0)
		{
			return ResolveRouteValue(placeHolder.Outer, httpContext, placeHolder);
		}

		var source = inner[..separatorIndex];
		var name = inner[(separatorIndex + SourceSeparator.Length)..];
		if (string.IsNullOrEmpty(name))
		{
			throw ErtisAuthException.AccessDenied($"The rbac placeholder '{placeHolder.Outer}' has no name");
		}
		
		if (isSubject && source is QuerySource or HeaderSource)
		{
			throw ErtisAuthException.AccessDenied($"The rbac subject can not be read from the client request ('{placeHolder.Outer}')");
		}
		
		if (source == RouteSource)
		{
			return ResolveRouteValue($"{{{name}}}", httpContext, placeHolder);
		}

		var value = source switch
		{
			QuerySource => GetSingleValue(httpContext.Request.Query.TryGetValue(name, out var queryValues) ? queryValues : StringValues.Empty, placeHolder),
			HeaderSource => GetSingleValue(httpContext.Request.Headers.TryGetValue(name, out var headerValues) ? headerValues : StringValues.Empty, placeHolder),
			EnvironmentVariableSource => Environment.GetEnvironmentVariable(name),
			_ => throw ErtisAuthException.AccessDenied($"The rbac placeholder '{placeHolder.Outer}' has an unknown source '{source}'")
		};

		if (string.IsNullOrEmpty(value))
		{
			throw ErtisAuthException.AccessDenied($"The rbac placeholder '{placeHolder.Outer}' could not be resolved");
		}

		return ValidateValue(value, placeHolder);
	}

	private static string ResolveRouteValue(string template, HttpContext httpContext, PlaceHolder placeHolder)
	{
		var value = Formatter.Format(template, httpContext.Request.RouteValues);

		// Unchanged behavior of the route placeholders: an undefined route value is written as it is
		if (value == template)
		{
			return value;
		}

		if (string.IsNullOrEmpty(value))
		{
			throw ErtisAuthException.AccessDenied($"The rbac placeholder '{placeHolder.Outer}' could not be resolved");
		}

		return ValidateValue(value, placeHolder);
	}

	/// <summary>
	/// Rejects the values which could make the authorization and the action see different objects.
	/// </summary>
	private static string ValidateValue(string value, PlaceHolder placeHolder)
	{
		// '*' would widen the segment to all values
		if (value == RbacSegment.All.Value || value == RbacSegment.All.Slug)
		{
			throw ErtisAuthException.AccessDenied($"The rbac placeholder '{placeHolder.Outer}' resolved to a reserved value");
		}

		// The dots of the values are encoded as %2E and the segment comparison treats %2E as a dot,
		// so 'a%2Eb' would be authorized as 'a.b' while the action uses 'a%2Eb'
		if (value.Contains(EncodedSeparator, StringComparison.OrdinalIgnoreCase))
		{
			throw ErtisAuthException.AccessDenied($"The rbac placeholder '{placeHolder.Outer}' resolved to a value which contains '{EncodedSeparator}'");
		}

		// ' secret' would not match a forbidden 'secret' although a layer behind the action may trim it
		if (value.Any(x => char.IsWhiteSpace(x) || char.IsControl(x)))
		{
			throw ErtisAuthException.AccessDenied($"The rbac placeholder '{placeHolder.Outer}' resolved to a value which contains whitespace or control characters");
		}

		return value;
	}
	
	private static string? GetSingleValue(StringValues values, PlaceHolder placeHolder)
	{
		// The authorization and the action must see the same value, a repeated query parameter or header is ambiguous
		if (values.Count > 1)
		{
			throw ErtisAuthException.AccessDenied($"The rbac placeholder '{placeHolder.Outer}' has multiple values");
		}
		
		return values.Count == 1 ? values[0] : null;
	}
	
	#endregion
}
