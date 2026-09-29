using ErtisAuth.Core.Attributes;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Extensions.Authorization.Attributes;
using ErtisAuth.Sdk.AspNetCore.Extensions;
using ErtisAuth.Sdk.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.IntegrationTests.Infrastructure;

/// <summary>
/// A client application protected by ErtisAuth.Sdk.AspNetCore, as the services using ErtisAuth run it, calling the
/// ErtisAuth instance under test over HTTP. Endpoints:
/// <c>GET /orders</c> (orders.read), <c>POST /orders</c> (orders.create), <c>GET /profile</c> (self authorized),
/// <c>GET /documents/{type}</c> (a self authorized action of an authorized controller, which checks the permission
/// 'documents-{type}.read' itself, as the CMS checks content permissions by content type).
/// </summary>
public sealed class SdkClientApplication : IAsyncDisposable
{
	#region Fields
	
	private readonly WebApplication _application;
	
	#endregion
	
	#region Properties
	
	public Uri BaseAddress { get; }
	
	#endregion
	
	#region Constructors
	
	private SdkClientApplication(WebApplication application, Uri baseAddress)
	{
		this._application = application;
		this.BaseAddress = baseAddress;
	}
	
	#endregion
	
	#region Methods
	
	public static async Task<SdkClientApplication> StartAsync(ErtisAuthInstance instance)
	{
		var ertisAuthAddress = instance.CreateClient().BaseAddress!.ToString().TrimEnd('/');
		
		var builder = WebApplication.CreateSlimBuilder();
		builder.WebHost.UseKestrel(options => options.Listen(System.Net.IPAddress.Loopback, 0));
		builder.Logging.ClearProviders();
		builder.Services.AddErtisAuth(options =>
		{
			options.BaseUrl = ertisAuthAddress;
			options.MembershipId = instance.MembershipId;
		});
		
		var application = builder.Build();
		application.UseAuthentication();
		application.UseAuthorization();
		
		application.MapGet("/orders", () => Results.Ok("orders"))
			.WithMetadata(new AuthorizedAttribute(), new RbacResourceAttribute("orders"), new RbacActionAttribute(Rbac.CrudActions.Read));
		application.MapPost("/orders", () => Results.Ok("created"))
			.WithMetadata(new AuthorizedAttribute(), new RbacResourceAttribute("orders"), new RbacActionAttribute(Rbac.CrudActions.Create));
		application.MapGet("/profile", () => Results.Ok("profile"))
			.WithMetadata(new SelfAuthorizedAttribute());
		
		// Metadata order as for a controller action: the controller's attributes first, then the action's
		application.MapGet("/documents/{type}", async (string type, HttpRequest request, IRoleService roleService) =>
			{
				var token = request.GetTokenFromHeader(out var tokenType);
				TokenBase tokenBase = tokenType == "Basic" ? new BasicToken(token!) : BearerToken.CreateTemp(token!);
				return await roleService.CheckPermissionAsync($"documents-{type}.read", tokenBase) ? Results.Ok(type) : Results.StatusCode(StatusCodes.Status403Forbidden);
			})
			.WithMetadata(new AuthorizedAttribute(), new RbacResourceAttribute("documents"), new RbacActionAttribute(Rbac.CrudActions.Read), new SelfAuthorizedAttribute());
		
		await application.StartAsync();
		var address = application.Urls.First();
		return new SdkClientApplication(application, new Uri(address));
	}
	
	public HttpClient CreateClient(string authorization)
	{
		var client = new HttpClient { BaseAddress = this.BaseAddress };
		client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authorization);
		return client;
	}
	
	public async ValueTask DisposeAsync()
	{
		await this._application.StopAsync();
		await this._application.DisposeAsync();
	}
	
	#endregion
}