﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Security;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Web;

namespace Steeltoe.Management.Endpoint.Handler
{
    public class MetricsHandler : ActuatorHandler<MetricsEndpoint, IMetricsResponse, MetricsRequest>
    {
        public MetricsHandler(MetricsEndpoint endpoint, IEnumerable<ISecurityService> securityServices, IEnumerable<IManagementOptions> mgmtOptions, ILogger<MetricsHandler> logger = null)
           : base(endpoint, securityServices, mgmtOptions, null, false, logger)
        {
        }

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public MetricsHandler(MetricsEndpoint endpoint, IEnumerable<ISecurityService> securityServices, ILogger<MetricsHandler> logger = null)
            : base(endpoint, securityServices, null, false, logger)
        {
        }

        public override void HandleRequest(HttpContextBase context)
        {
            var request = context.Request;
            var response = context.Response;

            _logger?.LogDebug("Incoming path: {0}", request.Path);

            var metricName = GetMetricName(request);
            if (!string.IsNullOrEmpty(metricName))
            {
                // GET /metrics/{metricName}?tag=key:value&tag=key:value
                var tags = ParseTags(request.QueryString);
                var metricRequest = new MetricsRequest(metricName, tags);
                var serialInfo = HandleRequest(metricRequest);

                if (serialInfo != null)
                {
                    response.StatusCode = (int)HttpStatusCode.OK;

                    context.Response.Write(HttpUtility.HtmlEncode(serialInfo));
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                }
            }
            else
            {
                // GET /metrics
                var serialInfo = HandleRequest(null);
                _logger?.LogDebug("Returning: {0}", serialInfo);
                response.Headers.Set("Content-Type", "application/vnd.spring-boot.actuator.v2+json");
                response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.Write(serialInfo);
            }
        }

        protected internal string GetMetricName(HttpRequestBase request)
        {
            List<string> epPaths;
            if (_mgmtOptions == null)
            {
                epPaths = new List<string>() { _endpoint.Path };
            }
            else
            {
                epPaths = _mgmtOptions.Select(opt => $"{opt.Path}/{_endpoint.Id}").ToList();
            }

            foreach (var epPath in epPaths)
            {
                var psPath = request.Path;
                if (psPath.StartsWithSegments(epPath, _mgmtOptions.Select(p => p.Path), out var remaining) && !string.IsNullOrEmpty(remaining))
                {
                    return remaining.TrimStart('/');
                }
            }

            return null;
        }

        /// <summary>
        /// Turn a querystring into a dictionary
        /// </summary>
        /// <param name="query">Request querystring</param>
        /// <returns>List of key-value pairs</returns>
        protected internal List<KeyValuePair<string, string>> ParseTags(NameValueCollection query)
        {
            var results = new List<KeyValuePair<string, string>>();
            if (query == null)
            {
                return results;
            }

            foreach (var q in query.AllKeys)
            {
                if (q.Equals("tag", StringComparison.InvariantCultureIgnoreCase))
                {
                    foreach (var kvp in query.GetValues(q))
                    {
                        var pair = ParseTag(kvp);
                        if (pair != null && !results.Contains(pair.Value))
                        {
                            results.Add(pair.Value);
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Split a key-value pair out from a single string
        /// </summary>
        /// <param name="kvp">Colon-delimited key-value pair</param>
        /// <returns>A pair of strings</returns>
        protected internal KeyValuePair<string, string>? ParseTag(string kvp)
        {
            var str = kvp.Split(new char[] { ':' }, 2);
            return str != null && str.Length == 2
                ? (KeyValuePair<string, string>?)new KeyValuePair<string, string>(str[0], str[1])
                : null;
        }
    }
}