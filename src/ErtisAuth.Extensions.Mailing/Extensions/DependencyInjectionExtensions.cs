using ErtisAuth.Extensions.Mailing.Services;
using ErtisAuth.Extensions.Mailing.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ErtisAuth.Extensions.Mailing.Extensions;

public static class DependencyInjectionExtensions
{
	#region Methods
	
	public static void AddMailProviders(this IServiceCollection services)
	{
		services.AddSingleton<IMailService, MailChimpService>();
		services.AddSingleton<IMailService, SendGridService>();
		services.AddSingleton<IMailService, SmtpServerService>();
	}
	
	#endregion
}