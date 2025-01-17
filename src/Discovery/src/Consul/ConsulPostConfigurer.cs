// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Common.Net;
using Steeltoe.Discovery.Client;
using Steeltoe.Discovery.Consul.Discovery;

namespace Steeltoe.Discovery.Consul;

public static class ConsulPostConfigurer
{
    /// <summary>
    /// At PostConfigure, confirm that settings are valid for the current environment.
    /// </summary>
    /// <param name="options">
    /// ConsulOptions to evaluate.
    /// </param>
    public static void ValidateConsulOptions(ConsulOptions options)
    {
        if ((Platform.IsContainerized || Platform.IsCloudHosted) && options.Host == "localhost")
        {
            throw new InvalidOperationException(
                $"Consul URL {options.Scheme}://{options.Host}:{options.Port} is not valid in containerized or cloud environments. Please configure Consul:Host with a non-localhost server.");
        }
    }

    /// <summary>
    /// Perform post-configuration on ConsulDiscoveryOptions.
    /// </summary>
    /// <param name="configuration">
    /// Application Configuration.
    /// </param>
    /// <param name="options">
    /// ConsulDiscoveryOptions to configure.
    /// </param>
    /// <param name="netOptions">
    /// Optional InetOptions.
    /// </param>
    public static void UpdateDiscoveryOptions(IConfiguration configuration, ConsulDiscoveryOptions options, InetOptions netOptions)
    {
        options.NetUtils = new InetUtils(netOptions);
        options.ApplyNetUtils();
        options.ApplyConfigUrls(configuration.GetAspNetCoreUrls(), ConfigurationUrlHelpers.WildcardHost);
    }
}
