using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft​.AspNetCore​.Http;
using Newtonsoft.Json;
using PowerNetwork.Core.DataModels;
using PowerNetwork.Core.Loggers;

namespace PowerNetwork
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            // Add framework services.
            services.AddMvc().AddJsonOptions(jsonOptions =>
            {
                jsonOptions.SerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
            });
            services.AddAuthorization(options =>   {
                    options.AddPolicy("ReadPolicy", policyBuilder => 
                    {
                        policyBuilder.RequireAuthenticatedUser()
                            .RequireAssertion(context => context.User.HasClaim("Read", "true"))
                            .Build();
                     });
                 });

            services.AddOptions();
            services.Configure<AppConfig>(options => Configuration.GetSection("AppConfig").Bind(options));

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseCookieAuthentication(new CookieAuthenticationOptions()
            {
                AuthenticationScheme = "Cookies",
                LoginPath = new PathString("/Home/Index/"),
                AccessDeniedPath = new PathString("/Home/Index/"),
                AutomaticAuthenticate = true,
                AutomaticChallenge = true
            });

            // add logging middleware
            // app.UseMiddleware<LogResponseMiddleware>();
            // app.UseMiddleware<LogRequestMiddleware>();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
