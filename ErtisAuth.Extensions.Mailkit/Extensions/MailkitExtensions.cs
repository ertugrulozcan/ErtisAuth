using ErtisAuth.Extensions.Mailkit.Services;
using ErtisAuth.Extensions.Mailkit.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ErtisAuth.Extensions.Mailkit.Extensions
{
    public static class MailkitExtensions
    {
        #region Methods

        public static void AddMailkit(this IServiceCollection services)
        {
            services.AddSingleton<IMailService, MailService>();
        }

        #endregion
    }
}