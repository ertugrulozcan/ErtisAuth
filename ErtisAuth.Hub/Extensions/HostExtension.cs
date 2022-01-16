using System.Linq;
using ErtisAuth.Hub.Services.Interfaces;
using ErtisAuth.Core.Models.Identity;
using Microsoft.AspNetCore.Builder;

namespace ErtisAuth.Hub.Extensions
{
    public static class HostExtension
    {
        #region Methods

        public static void UseTokenAccessor(this IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                if (context.RequestServices.GetService(typeof(IAuthenticationTokenAccessor)) is IAuthenticationTokenAccessor authenticationTokenAccessor)
                {
                    var tokenClaim = context.User.Claims.FirstOrDefault(x => x.Type == "access_token");
                    if (tokenClaim != null && !string.IsNullOrEmpty(tokenClaim.Value))
                    {
                        var emailClaim = context.User.Claims.FirstOrDefault(x => x.Type == "email");
                        if (string.IsNullOrEmpty(emailClaim?.Value))
                        {
                            authenticationTokenAccessor.Token = new BasicToken(tokenClaim.Value);
                        }
                        else
                        {
                            authenticationTokenAccessor.Token = BearerToken.CreateTemp(tokenClaim.Value);	
                        }	
                    }
                }
				
                await next.Invoke();
            });
        }

        #endregion
    }
}