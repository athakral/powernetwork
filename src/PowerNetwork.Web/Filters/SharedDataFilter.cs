using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PowerNetwork.Core.Helpers;
using Microsoft.Extensions.Logging;

namespace PowerNetwork.Web.Filters {
    public class SharedDataFilter : ResultFilterAttribute {

        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly ILogger _logger;

        public SharedDataFilter(IHostingEnvironment hostingEnvironment, ILogger<SharedDataFilter> logger) {
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
        }

        public override void OnResultExecuting(ResultExecutingContext context) {
            var currentLang = context.HttpContext.Request.Query["l"];
            if (!string.IsNullOrEmpty(currentLang)) {
                context.HttpContext.Session.SetString("current_lang", currentLang);
            }

            currentLang = context.HttpContext.Session.GetString("current_lang");

            var controller = context.Controller as Controller;
            controller.ViewBag.Texts = ResourceService.Instance(_hostingEnvironment).GetMap(currentLang);
        }
    }
}
