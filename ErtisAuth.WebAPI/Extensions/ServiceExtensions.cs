using Ertis.Net.Rest;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Extensions.AspNetCore.Middleware;
using ErtisAuth.Extensions.AspNetCore.Services;
using ErtisAuth.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;

namespace ErtisAuth.WebAPI.Extensions;

public static class ServiceExtensions
{
	#region Methods
	
	public static void AddServices(this IServiceCollection services)
	{
		services.AddSingleton<ISystemRestHandler, SystemRestHandler>();
		services.AddSingleton<IJwtService, JwtService>();
		services.AddSingleton<ITokenService, TokenService>();
		services.AddSingleton<IActiveTokenService, ActiveTokenService>();
		services.AddSingleton<IRevokedTokenService, RevokedTokenService>();
		services.AddSingleton<IAccessControlService, AccessControlService>();
		services.AddSingleton<IMembershipService, MembershipService>();
		services.AddSingleton<IEventService, EventService>();
		services.AddSingleton<IUserTypeService, UserTypeService>();
		services.AddSingleton<IUserService, UserService>();
		services.AddSingleton<IApplicationService, ApplicationService>();
		services.AddSingleton<IRoleService, RoleService>();
		services.AddSingleton<ITokenCodeService, TokenCodeService>();
		services.AddSingleton<ITokenCodePolicyService, TokenCodePolicyService>();
		services.AddSingleton<IOneTimePasswordService, OneTimePasswordService>();
		services.AddSingleton<IProviderService, ProviderService>();
		services.AddSingleton<IWebhookService, WebhookService>();
		services.AddSingleton<IMailHookService, MailHookService>();
		services.AddSingleton<IMailServiceBackgroundWorker, MailServiceBackgroundWorker>();
		services.AddSingleton<IMigrationService, MigrationService>();
		services.AddSingleton<IUtilizerService, UtilizerService>();
		services.AddSingleton<IAuthorizationHandler, ErtisAuthAuthorizationHandler>();
	}
	
	public static void UseServices(this IApplicationBuilder app)
	{
		var serviceProvider = app.ApplicationServices;
		
		serviceProvider.GetRequiredService<IUserTypeService>();
		serviceProvider.GetRequiredService<IUserService>();
		serviceProvider.GetRequiredService<IApplicationService>();
		serviceProvider.GetRequiredService<IRoleService>();
		serviceProvider.GetRequiredService<IProviderService>();
		serviceProvider.GetRequiredService<IWebhookService>();
		serviceProvider.GetRequiredService<IMailHookService>();
	}
	
	#endregion
}