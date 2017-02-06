using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;

namespace PowerNetwork.Core.Loggers
{
    public class LogRequestMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LogRequestMiddleware> _logger;
        private readonly Func<string, Exception, string> _defaultFormatter = (state, exception) => state;

        public LogRequestMiddleware(RequestDelegate next, ILogger<LogRequestMiddleware> logger)
        {
            this._next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var requestBodyStream = new MemoryStream();
            var originalRequestBody = context.Request.Body;

            await context.Request.Body.CopyToAsync(requestBodyStream);
            requestBodyStream.Seek(0, SeekOrigin.Begin);

            var url = context.Request.GetDisplayUrl();
            var requestBodyText = new StreamReader(requestBodyStream).ReadToEnd();
            _logger.Log(LogLevel.Information, 1, $"REQUEST METHOD: {context.Request.Method}, REQUEST BODY: {requestBodyText}, REQUEST URL: {url}", null, _defaultFormatter);

            requestBodyStream.Seek(0, SeekOrigin.Begin);
            context.Request.Body = requestBodyStream;

            await _next(context);
            context.Request.Body = originalRequestBody;
        }
    }
}
