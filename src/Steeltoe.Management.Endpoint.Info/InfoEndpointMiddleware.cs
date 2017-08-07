﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Steeltoe.Management.Endpoint.Middleware;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Info
{

    public class InfoEndpointMiddleware : EndpointMiddleware<Dictionary<string,object>>
    {
        private RequestDelegate _next;

        public InfoEndpointMiddleware(RequestDelegate next, InfoEndpoint endpoint, ILogger<InfoEndpointMiddleware> logger)
            : base(endpoint, logger)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (IsInfoRequest(context))
            {
                await HandleInfoRequestAsync(context);
            }
            else
            {
                await _next(context);
            }
        }

        private async Task HandleInfoRequestAsync(HttpContext context)
        {
            var serialInfo = base.HandleRequest();
            logger.LogDebug("Returning: {0}", serialInfo);
            context.Response.Headers.Add("Content-Type", "application/vnd.spring-boot.actuator.v1+json");
            await context.Response.WriteAsync(serialInfo);
        }

        private bool IsInfoRequest(HttpContext context)
        {
            if (!context.Request.Method.Equals("GET")) { return false; }
            PathString path = new PathString(endpoint.Path);
            return context.Request.Path.Equals(path);
        }

    }
}