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
using Newtonsoft.Json;
using PowerNetwork.Core.DataModels;
using PowerNetwork.Core.Loggers;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using PowerNetwork.Web.Filters;

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
            services.AddDistributedMemoryCache(); // Adds a default in-memory implementation of IDistributedCache
            services.AddSession();
            // Add framework services.
            services.AddMvc().AddJsonOptions(jsonOptions =>
            {
                jsonOptions.SerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
                jsonOptions.SerializerSettings.ContractResolver =
                    new Newtonsoft.Json.Serialization.DefaultContractResolver();
            }).AddMvcOptions(options =>
            {
                options.Filters.Add(new TypeFilterAttribute(typeof(SharedDataFilter)));
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
                //app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStatusCodePagesWithReExecute("/StatusCode/{0}");

            app.UseStaticFiles(new StaticFileOptions()
            {
                ServeUnknownFileTypes = true,
                DefaultContentType = "applications/octet-stream"
            });

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

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
            // IMPORTANT: This session call MUST go before UseMvc()
            app.UseSession();

            app.Use((context, next) => {
                if (!context.Response.Headers.ContainsKey("X-Content-Type-Options"))
                    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");

                if (!context.Response.Headers.ContainsKey("X-Frame-Options"))
                    context.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");

                if (!context.Response.Headers.ContainsKey("X-Content-Type-Options"))
                    context.Response.Headers.Add("X-Content-Type-Options", "1; mode=block");

                return next.Invoke();
            });

            app.UseMvc(routes => {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
