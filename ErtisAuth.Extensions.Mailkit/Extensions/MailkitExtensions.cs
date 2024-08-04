using Ertis.Net.Rest;
using ErtisAuth.Extensions.Mailkit.Services;
using ErtisAuth.Extensions.Mailkit.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ErtisAuth.Extensions.Mailkit.Extensions
{
    public static class MailkitExtensions
    {
        #region Statics

        public static ISystemRestHandler RestHandler { get; private set; }

        #endregion
        
        #region Methods

        public static void AddMailkit(this IServiceCollection services)
        {
            services.AddSingleton<IMailService, MailService>();
        }
        
        public static void UseMailkit(this IApplicationBuilder app)
        {
            RestHandler = app.ApplicationServices.GetRequiredService<ISystemRestHandler>();
        }

        #endregion
    }
}