// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.ThreadDump;

internal class ConfigureThreadDumpEndpointOptions : ConfigureEndpointOptions<ThreadDumpEndpointOptions>
{
    private const string ManagementInfoPrefix = "management:endpoints:dump";

    public ConfigureThreadDumpEndpointOptions(IConfiguration configuration)
        : base(configuration, ManagementInfoPrefix, "threaddump")
    {
    }
}
