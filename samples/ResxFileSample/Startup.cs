using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.Routing;
using ResxFileSample.Controllers;

namespace ResxFileSample
{
    public class Startup
    {
       

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddLocalization(options => { options.ResourcesPath = "Resources"; options.EnabledFileResources = true; });
            services.AddMvcCore()
                    .AddJsonFormatters()
                    .AddViewLocalization()
                    .AddDataAnnotationsLocalization();

            // services.AddSingleton<IStringLocalizerFactory, ResourceManagerStringLocalizerFactory>();
            // services.AddTransient(typeof(IUrlHelper), typeof(UrlLocalHelper));

            services.AddSingleton<IUrlHelperFactory, UrlLocalHelperFactory>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(LogLevel.Debug);
            loggerFactory.AddDebug();
            var vSupCulture = new List<CultureInfo>()
            {
                new CultureInfo("zh-CN"),
                new CultureInfo("fr-FR"),
                new CultureInfo("en-US")
            };
            RequestLocalizationOptions RLOptions = new RequestLocalizationOptions()
            {
                DefaultRequestCulture = new RequestCulture("zh-CN"),
                SupportedCultures = vSupCulture,
                SupportedUICultures = vSupCulture,
            };
            RLOptions.RequestCultureProviders.Insert(0, new URLRequestCultureProvider() { Options = RLOptions });// Options.SupportedCultures));

          
            app.UseRequestLocalization(RLOptions);

            app.UseStaticFiles();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

           
        }
    }
}
