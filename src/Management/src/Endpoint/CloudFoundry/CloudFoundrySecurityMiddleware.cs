// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.CloudFoundry;

public class CloudFoundrySecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CloudFoundrySecurityMiddleware> _logger;
    private readonly IOptionsMonitor<CloudFoundryEndpointOptions> _options;

    private readonly ManagementEndpointOptions _managementOptions;

    private readonly SecurityBase _base;

    public CloudFoundrySecurityMiddleware(RequestDelegate next, IOptionsMonitor<CloudFoundryEndpointOptions> options,
        IOptionsMonitor<ManagementEndpointOptions> managementOptions, ILogger<CloudFoundrySecurityMiddleware> logger = null)
    {
        _next = next;
        _logger = logger;
        _options = options;
        _managementOptions = managementOptions.Get(CFContext.Name);

        _base = new SecurityBase(options.CurrentValue, managementOptions.Get(CFContext.Name), logger);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        CloudFoundryEndpointOptions cfOptions = _options.CurrentValue;
        CloudFoundryEndpointOptions endpointOptions = _options.CurrentValue;
        _logger?.LogDebug("InvokeAsync({requestPath}), contextPath: {contextPath}", context.Request.Path.Value, _managementOptions.Path);

        if (Platform.IsCloudFoundry && endpointOptions.IsEnabled(_managementOptions) && _base.IsCloudFoundryRequest(context.Request.Path))
        {
            if (string.IsNullOrEmpty(cfOptions.ApplicationId))
            {
                _logger?.LogCritical(
                    "The Application Id could not be found. Make sure the Cloud Foundry Configuration Provider has been added to the application configuration.");

                await ReturnErrorAsync(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, SecurityBase.ApplicationIdMissingMessage));

                return;
            }

            if (string.IsNullOrEmpty(cfOptions.CloudFoundryApi))
            {
                await ReturnErrorAsync(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, SecurityBase.CloudfoundryApiMissingMessage));

                return;
            }

            IEndpointOptions target = FindTargetEndpoint(context.Request.Path);

            if (target == null)
            {
                await ReturnErrorAsync(context, new SecurityResult(HttpStatusCode.ServiceUnavailable, SecurityBase.EndpointNotConfiguredMessage));

                return;
            }

            SecurityResult sr = await GetPermissionsAsync(context);

            if (sr.Code != HttpStatusCode.OK)
            {
                await ReturnErrorAsync(context, sr);
                return;
            }

            Permissions permissions = sr.Permissions;

            if (!target.IsAccessAllowed(permissions))
            {
                await ReturnErrorAsync(context, new SecurityResult(HttpStatusCode.Forbidden, SecurityBase.AccessDeniedMessage));
                return;
            }
        }

        await _next(context);
    }

    internal string GetAccessToken(HttpRequest request)
    {
        if (request.Headers.TryGetValue(SecurityBase.AuthorizationHeader, out StringValues headerVal))
        {
            string header = headerVal.ToString();

            if (header.StartsWith(SecurityBase.Bearer, StringComparison.OrdinalIgnoreCase))
            {
                return header.Substring(SecurityBase.Bearer.Length + 1);
            }
        }

        return null;
    }

    internal Task<SecurityResult> GetPermissionsAsync(HttpContext context)
    {
        string token = GetAccessToken(context.Request);
        return _base.GetPermissionsAsync(token);
    }

    private IEndpointOptions FindTargetEndpoint(PathString path)
    {
        List<IEndpointOptions> configEndpoints;

        configEndpoints = _managementOptions.EndpointOptions;

        foreach (IEndpointOptions ep in configEndpoints)
        {
            string contextPath = _managementOptions.Path;

            if (!contextPath.EndsWith('/') && !string.IsNullOrEmpty(ep.Path))
            {
                contextPath += '/';
            }

            string fullPath = contextPath + ep.Path;

            if (ep is CloudFoundryEndpointOptions)
            {
                if (path.Value.Equals(contextPath, StringComparison.OrdinalIgnoreCase))
                {
                    return ep;
                }
            }
            else if (path.StartsWithSegments(new PathString(fullPath)))
            {
                return ep;
            }
        }

        return null;
    }

    private Task ReturnErrorAsync(HttpContext context, SecurityResult error)
    {
        LogError(context, error);
        context.Response.Headers.Add("Content-Type", "application/json;charset=UTF-8");

        // allowing override of 400-level errors is more likely to cause confusion than to be useful
        if (_managementOptions.UseStatusCodeFromResponse || (int)error.Code < 500)
        {
            context.Response.StatusCode = (int)error.Code;
        }

        return context.Response.WriteAsync(_base.Serialize(error));
    }

    private void LogError(HttpContext context, SecurityResult error)
    {
        _logger?.LogError("Actuator Security Error: {code} - {message}", error.Code, error.Message);

        if (_logger != null && _logger.IsEnabled(LogLevel.Trace))
        {
            foreach (KeyValuePair<string, StringValues> header in context.Request.Headers)
            {
                _logger.LogTrace("Header: {key} - {value}", header.Key, header.Value);
            }
        }
    }
}
