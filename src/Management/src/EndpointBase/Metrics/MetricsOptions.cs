﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;

namespace Steeltoe.Management.Endpoint.Metrics
{
    [Obsolete("Use MetricsEndpointOptions instead")]
    public class MetricsOptions : AbstractOptions, IMetricsOptions
    {
        internal const string MANAGEMENT_INFO_PREFIX = "management:endpoints:metrics";
        internal const string DEFAULT_INGRESS_IGNORE_PATTERN = "/cloudfoundryapplication|/cloudfoundryapplication/.*|.*\\.png|.*\\.css|.*\\.js|.*\\.html|/favicon.ico|/hystrix.stream|.*\\.gif";
        internal const string DEFAULT_EGRESS_IGNORE_PATTERN = "/api/v2/spans|/v2/apps/.*/permissions";

        public MetricsOptions()
            : base()
        {
            Id = "metrics";
            IngressIgnorePattern = DEFAULT_INGRESS_IGNORE_PATTERN;
            EgressIgnorePattern = DEFAULT_EGRESS_IGNORE_PATTERN;
        }

        public MetricsOptions(IConfiguration config)
            : base(MANAGEMENT_INFO_PREFIX, config)
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = "metrics";
            }

            if (string.IsNullOrEmpty(IngressIgnorePattern))
            {
                IngressIgnorePattern = DEFAULT_INGRESS_IGNORE_PATTERN;
            }

            if (string.IsNullOrEmpty(EgressIgnorePattern))
            {
                EgressIgnorePattern = DEFAULT_EGRESS_IGNORE_PATTERN;
            }
        }

        public string IngressIgnorePattern { get; set; }

        public string EgressIgnorePattern { get; set; }
    }
}