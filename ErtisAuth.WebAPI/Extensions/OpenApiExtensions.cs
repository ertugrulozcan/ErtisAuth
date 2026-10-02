using System.Reflection;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace ErtisAuth.WebAPI.Extensions;

public static class OpenApiExtensions
{
	#region Constants
	
	private const string BasePath = "docs";
	private const string Title = "ErtisAuth";
	private const string Summary = "Open Source Identity and Access Management API";
	private const string RepositoryUrl = "https://github.com/ertugrulozcan/ErtisAuth";
	
	private const string Description =
		"""
		ErtisAuth is a free and open-source OpenID-Connect framework and identity and access management (IAM) API, providing a common way to authenticate the requests of web, native and mobile applications and Web API endpoints. Authorization is based on the RBAC (role based access control) and UBAC (user based access control) models.
		
		**Memberships** are isolated tenants with their own users, user types, roles, applications and token settings; most endpoints are scoped by the membership id in the route.
		
		**Authorization header:** `Bearer <access token>` for users (see generate token), `Basic <application id>:<application secret>` for applications (machine to machine).
		
		See the [wiki](https://github.com/ertugrulozcan/ErtisAuth/wiki) for the developer guide.
		""";
	
	#endregion
	
	#region Methods
	
	public static void AddOpenApi(this WebApplicationBuilder builder)
	{
		var services = builder.Services;
		services.AddOpenApi(options =>
		{
			options.AddDocumentTransformer((document, _, _) =>
			{
				var version = GetVersion();
				
				document.Info.Title = $"{Title} - Mark IV";
				document.Info.Summary = Summary;
				document.Info.Description = Description;
				document.Info.Version = version;
				document.Info.Contact = new OpenApiContact
				{
					Name = "Ertuğrul Özcan",
					Url = new Uri(RepositoryUrl)
				};
				document.Info.License = new OpenApiLicense
				{
					Name = "MIT",
					Url = new Uri($"{RepositoryUrl}/blob/master/LICENSE")
				};
				
				return Task.CompletedTask;
			});
		});
	}
	
	public static void UseOpenApi(this WebApplication app)
	{
		if (app.Environment.IsDevelopment())
		{
			app.MapOpenApi();
			app.MapScalarApiReference(BasePath, options =>
			{
				options
					.WithTitle(Title)
					.ShowOperationId()
					.ExpandAllTags()
					.SortOperationsByMethod()
					.PreserveSchemaPropertyOrder();
			});
		}
	}
	
	/// <summary>
	/// The informational version of the WebAPI assembly, without the source revision (e.g. "10.0.0+a1b2c3d" -> "10.0.0").
	/// </summary>
	private static string GetVersion()
	{
		var assembly = typeof(OpenApiExtensions).Assembly;
		var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? assembly.GetName().Version?.ToString() ?? "1.0.0";
		var revisionIndex = version.IndexOf('+');
		
		return revisionIndex > 0 ? version[..revisionIndex] : version;
	}
	
	#endregion
}
