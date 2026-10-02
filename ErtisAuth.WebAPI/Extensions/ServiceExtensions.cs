using Ertis.Net.Rest;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Core.Models.Webhooks;
using ErtisAuth.Extensions.AspNetCore.Middleware;
using ErtisAuth.Extensions.AspNetCore.Services;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.WebAPI.BackgroundServices;
using Microsoft.AspNetCore.Authorization;

namespace ErtisAuth.WebAPI.Extensions;

public static class ServiceExtensions
{
	#region Methods
	
	public static void AddServices(this IServiceCollection services)
	{
		services.AddSingleton<IRestHandler, RestHandler>();
		services.AddSingleton<IJwtService, JwtService>();
		services.AddSingleton<LegacyApplicationSecretVerifier>(); // LEGACY-APP-SECRET
		services.AddSingleton<ITokenService, TokenService>();
		services.AddSingleton<IActiveTokenService, ActiveTokenService>();
		services.AddSingleton<IRevokedTokenService, RevokedTokenService>();
		services.AddSingleton<IAccessControlService, AccessControlService>();
		services.AddSingleton<IMembershipService, MembershipService>();
		services.AddSingleton<IEventService, EventService>();
		services.AddSingleton<IUserUniqueIndexSynchronizer, UserUniqueIndexSynchronizer>();
		services.AddSingleton<IUserTypeService, UserTypeService>();
		services.AddSingleton<IUserService, UserService>();
		services.AddSingleton<IPasswordResetService, PasswordResetService>();
		services.AddSingleton<IApplicationService, ApplicationService>();
		services.AddSingleton<IRoleService, RoleService>();
		services.AddSingleton<ITokenCodeService, TokenCodeService>();
		services.AddSingleton<ITokenCodePolicyService, TokenCodePolicyService>();
		services.AddSingleton<IOneTimePasswordService, OneTimePasswordService>();
		services.AddSingleton<IProviderService, ProviderService>();
		services.AddSingleton<IBackgroundQueue<WebhookCall>, BackgroundQueue<WebhookCall>>();
		services.AddSingleton<IWebhookService, WebhookService>();
		services.AddHostedService<WebhookBackgroundService>();
		services.AddSingleton<IBackgroundQueue<HookMail>, BackgroundQueue<HookMail>>();
		services.AddSingleton<IMailHookService, MailHookService>();
		services.AddHostedService<MailHookBackgroundService>();
		services.AddSingleton<ISetupService, SetupService>();
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