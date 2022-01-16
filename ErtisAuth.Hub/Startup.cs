using ErtisAuth.Hub.Configuration;
using ErtisAuth.Hub.Extensions;
using ErtisAuth.Hub.Services;
using ErtisAuth.Hub.Services.Interfaces;
using ErtisAuth.Extensions.AspNetCore;
using ErtisAuth.Extensions.AspNetCore.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace ErtisAuth.Hub
{
    public class Startup
    {
        #region Constants

        private const string CORS_POLICY_KEY = "cors-policy";

        #endregion
        
        #region Properties

        public IConfiguration Configuration { get; }

        #endregion
        
        #region Constructors

        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        #endregion
        
        #region Methods

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<IAuthenticationTokenAccessor, AuthenticationTokenAccessor>();
            services.AddScoped<ISessionService, SessionService>();

            // Middleware Services
            services.AddScoped<IMiddlewareRoleService, MiddlewareRoleService>();
            
            // Google Maps
            services.Configure<GoogleMapsApiConfiguration>(this.Configuration.GetSection("GoogleMaps"));
            services.AddSingleton<IGoogleMapsApiConfiguration>(serviceProvider => serviceProvider.GetRequiredService<IOptions<GoogleMapsApiConfiguration>>().Value);
            
            // Cookies
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = _ => false;
                options.MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
            });

            // Cors options
            services.AddCors(options =>
            {
                options.AddPolicy(CORS_POLICY_KEY,
                    builder =>
                    {
                        builder
                            .AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                    });
            });
            
            // ErtisAuth
            services.AddErtisAuth(x =>
            {
                x.SetDefaultAuthenticationHandler = false;
                x.SetDefaultAuthorizationHandler = false;
            });
            
            // SuperUserOptions
            services.Configure<SuperUserOptions>(this.Configuration.GetSection("ErtisAuth"));
            services.AddSingleton<ISuperUserOptions>(serviceProvider => serviceProvider.GetRequiredService<IOptions<SuperUserOptions>>().Value);

            services.AddSingleton<IAuthorizationHandler, MiddlewareAuthorizationHandler>();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme,
                    options =>
                    {
                        options.LoginPath = new PathString("/login");
                        options.LogoutPath = new PathString("/logout");
                        options.AccessDeniedPath = new PathString("/unauthorized");
                        options.ReturnUrlParameter = Constants.SchemaConstants.ReturnUrlParameter;
                    });

            // Authorization
            services.AddAuthorization(options =>
            {
                options.AddPolicy(
                    ErtisAuth.Extensions.AspNetCore.Constants.Schemes.ErtisAuthAuthorizationSchemeName,
                    policy => policy.AddRequirements(new ErtisAuthAuthorizationRequirement()));
            });
            
            services
                .AddControllersWithViews()
                .AddNewtonsoftJson()
                .AddRazorRuntimeCompilation();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // #{Octopus.Release.Number}
            Program.SetEnvironmentParameter("Version", app.ApplicationServices.GetRequiredService<IConfiguration>()["Version"]);
            Program.SetEnvironmentParameter("Environment", this.Configuration["Environment"]);
            Program.SetEnvironmentParameter("SelfUrl", this.Configuration["SelfUrl"]);

            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    const int durationInSeconds = 60 * 60 * 24 * 3;
                    ctx.Context.Response.Headers[HeaderNames.CacheControl] = "public,max-age=" + durationInSeconds;
                }
            });
			
            // No-index
            app.UseXRobotsTag(options => options.NoIndex().NoFollow());
            
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseTokenAccessor();
            app.UseCookiePolicy();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        #endregion
    }
}