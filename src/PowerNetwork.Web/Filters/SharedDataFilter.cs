using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PowerNetwork.Core.Helpers;

namespace PowerNetwork.Web.Filters
{
    public class SharedDataFilter : ResultFilterAttribute
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        public SharedDataFilter(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            var currentLang = context.HttpContext.Request.Query["l"];
            if(!string.IsNullOrEmpty(currentLang))
                    context.HttpContext.Session.SetString("current_lang",currentLang);

            currentLang = context.HttpContext.Session.GetString("current_lang");

            var controller = context.Controller as Controller;
            controller.ViewBag.Texts = ResourceService.Instance(this._hostingEnvironment).GetMap(currentLang);
            controller.ViewBag.SubDomain = context.HttpContext.Request.GetSubDomain();
        }
    }
}
