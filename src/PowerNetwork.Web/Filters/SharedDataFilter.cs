using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PowerNetwork.Core.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PowerNetwork.Core.DataModels;

namespace PowerNetwork.Web.Filters {
    public class SharedDataFilter : ResultFilterAttribute {

        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly ILogger _logger;
        private readonly AppConfig _appConf;

        public SharedDataFilter(IHostingEnvironment hostingEnvironment, ILogger<SharedDataFilter> logger, IOptions<AppConfig> appConfig) {
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
            _appConf = appConfig.Value;
        }

        public override void OnResultExecuting(ResultExecutingContext context) {
            var currentLang = context.HttpContext.Request.Query["l"];

            if (!string.IsNullOrEmpty(currentLang)) {
                context.HttpContext.Session.SetString("current_lang", currentLang);

            } else {
                currentLang = context.HttpContext.Session.GetString("current_lang");

                if (string.IsNullOrEmpty(currentLang)) {
                    currentLang = _appConf.DefaultLanguage;
                    context.HttpContext.Session.SetString("current_lang", currentLang);
                }
            }
            
            var controller = context.Controller as Controller;
            controller.ViewBag.CurrentLanguage = currentLang;
            controller.ViewBag.Texts = ResourceService.Instance(_hostingEnvironment).GetMap(currentLang);
        }
    }
}
