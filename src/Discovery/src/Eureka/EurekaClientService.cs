// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka;

public static class EurekaClientService
{
    /// <summary>
    /// Using the Eureka configuration values provided in <paramref name="configuration" /> contact the Eureka server and return all the service instances
    /// for the provided <paramref name="serviceId" />. The Eureka client is shutdown after contacting the server.
    /// </summary>
    /// <param name="configuration">
    /// configuration values used for configuring the Eureka client.
    /// </param>
    /// <param name="serviceId">
    /// the Eureka service id to look up all instances of.
    /// </param>
    /// <param name="logFactory">
    /// optional log factory to use for logging.
    /// </param>
    /// <returns>
    /// service instances.
    /// </returns>
    public static IList<IServiceInstance> GetInstances(IConfiguration configuration, string serviceId, ILoggerFactory logFactory = null)
    {
        EurekaClientOptions options = ConfigureClientOptions(configuration);
        LookupClient client = GetLookupClient(options, logFactory);
        IList<IServiceInstance> result = client.GetInstancesInternal(serviceId);
        client.ShutdownAsync().GetAwaiter().GetResult();
        return result;
    }

    /// <summary>
    /// Using the Eureka configuration values provided in <paramref name="configuration" /> contact the Eureka server and return all the registered services.
    /// The Eureka client is shutdown after contacting the server.
    /// </summary>
    /// <param name="configuration">
    /// configuration values used for configuring the Eureka client.
    /// </param>
    /// <param name="logFactory">
    /// optional log factory to use for logging.
    /// </param>
    /// <returns>
    /// all registered services.
    /// </returns>
    public static IList<string> GetServices(IConfiguration configuration, ILoggerFactory logFactory = null)
    {
        EurekaClientOptions options = ConfigureClientOptions(configuration);
        LookupClient client = GetLookupClient(options, logFactory);
        IList<string> result = client.GetServicesInternal();
        client.ShutdownAsync().GetAwaiter().GetResult();
        return result;
    }

    internal static LookupClient GetLookupClient(EurekaClientOptions options, ILoggerFactory logFactory)
    {
        return new LookupClient(options, null, logFactory);
    }

    internal static EurekaClientOptions ConfigureClientOptions(IConfiguration configuration)
    {
        IConfigurationSection clientConfigSection = configuration.GetSection(EurekaClientOptions.EurekaClientConfigurationPrefix);

        var clientOptions = new EurekaClientOptions();
        clientConfigSection.Bind(clientOptions);
        clientOptions.ShouldFetchRegistry = true;
        clientOptions.ShouldRegisterWithEureka = false;
        return clientOptions;
    }

    internal sealed class LookupClient : DiscoveryClient
    {
        public LookupClient(IEurekaClientConfiguration clientConfiguration, IEurekaHttpClient httpClient = null, ILoggerFactory logFactory = null)
            : base(clientConfiguration, httpClient, logFactory)
        {
            if (cacheRefreshTimer != null)
            {
                cacheRefreshTimer.Dispose();
                cacheRefreshTimer = null;
            }
        }

        public IList<IServiceInstance> GetInstancesInternal(string serviceId)
        {
            IList<InstanceInfo> infos = GetInstancesByVipAddress(serviceId, false);
            var instances = new List<IServiceInstance>();

            foreach (InstanceInfo info in infos)
            {
                logger?.LogDebug($"GetInstances returning: {info}");
                instances.Add(new EurekaServiceInstance(info));
            }

            return instances;
        }

        public IList<string> GetServicesInternal()
        {
            Applications applications = Applications;

            if (applications == null)
            {
                return new List<string>();
            }

            IList<Application> registered = applications.GetRegisteredApplications();
            var names = new List<string>();

            foreach (Application app in registered)
            {
                if (app.Instances.Count == 0)
                {
                    continue;
                }

#pragma warning disable S4040 // Strings should be normalized to uppercase
                names.Add(app.Name.ToLowerInvariant());
#pragma warning restore S4040 // Strings should be normalized to uppercase
            }

            return names;
        }
    }
}
