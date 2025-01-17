// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Http;
using Steeltoe.Common.Net;
using Steeltoe.Common.Options;
using Steeltoe.Common.Reflection;
using Steeltoe.Connector.Services;
using Steeltoe.Discovery.Client;
using Steeltoe.Discovery.Eureka.Transport;
using static Steeltoe.Discovery.Client.DiscoveryServiceCollectionExtensions;

namespace Steeltoe.Discovery.Eureka;

public class EurekaDiscoveryClientExtension : IDiscoveryClientExtension
{
    private const string SpringDiscoveryEnabled = "spring:cloud:discovery:enabled";
    public const string EurekaPrefix = "eureka";

    public string ServiceInfoName { get; private set; }

    public EurekaDiscoveryClientExtension()
        : this(null)
    {
    }

    public EurekaDiscoveryClientExtension(string serviceInfoName)
    {
        ServiceInfoName = serviceInfoName;
    }

    /// <inheritdoc />
    public void ApplyServices(IServiceCollection services)
    {
        ConfigureEurekaServices(services);
        AddEurekaServices(services);
    }

    public bool IsConfigured(IConfiguration configuration, IServiceInfo serviceInfo = null)
    {
        return configuration.GetSection(EurekaPrefix).GetChildren().Any() || serviceInfo is EurekaServiceInfo;
    }

    internal void ConfigureEurekaServices(IServiceCollection services)
    {
        services.AddOptions<EurekaClientOptions>().Configure<IConfiguration>((options, configuration) =>
        {
            configuration.GetSection(EurekaClientOptions.EurekaClientConfigurationPrefix).Bind(options);

            // Eureka is enabled by default. If eureka:client:enabled was not set then check spring:cloud:discovery:enabled
            if (options.Enabled && configuration.GetValue<bool?>($"{EurekaClientOptions.EurekaClientConfigurationPrefix}:enabled") is null &&
                configuration.GetValue<bool?>(SpringDiscoveryEnabled) == false)
            {
                options.Enabled = false;
            }
        }).PostConfigure<IConfiguration>((options, configuration) =>
        {
            EurekaServiceInfo info = GetServiceInfo(configuration);
            EurekaPostConfigurer.UpdateConfiguration(info, options);
        });

        services.AddOptions<EurekaInstanceOptions>()
            .Configure<IConfiguration>((options, configuration) =>
                configuration.GetSection(EurekaInstanceOptions.EurekaInstanceConfigurationPrefix).Bind(options)).PostConfigure<IServiceProvider>(
                (options, serviceProvider) =>
                {
                    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                    var appInfo = serviceProvider.GetRequiredService<IApplicationInstanceInfo>();
                    var inetOptions = configuration.GetSection(InetOptions.Prefix).Get<InetOptions>();
                    options.NetUtils = new InetUtils(inetOptions);
                    options.ApplyNetUtils();
                    const string endpointAssembly = "Steeltoe.Management.Endpoint";

                    if (ReflectionHelpers.IsAssemblyLoaded(endpointAssembly))
                    {
                        Type mgmtOptionsType = ReflectionHelpers.FindType(new[]
                        {
                            endpointAssembly
                        }, new[]
                        {
                            "Steeltoe.Management.Endpoint.Options.ManagementEndpointOptions"
                        });

                        Type endpointOptionsBaseType = ReflectionHelpers.FindType(new[]
                        {
                            "Steeltoe.Management.Abstractions"
                        }, new[]
                        {
                            "Steeltoe.Management.IEndpointOptions"
                        });

                        Type contextNameType = ReflectionHelpers.FindType(new[]
                        {
                            endpointAssembly
                        }, new[]
                        {
                            "Steeltoe.Management.Endpoint.Options.IContextName"
                        });

                        IEnumerable<object> contexts = serviceProvider.GetServices(contextNameType);

                        if (contexts.Any())
                        {
                            object actuatorOptions = GetOptionsMonitor(serviceProvider, mgmtOptionsType, "Actuator");
                            string basePath = $"{(string)actuatorOptions.GetType().GetProperty("Path")?.GetValue(actuatorOptions)}/";

                            if (string.IsNullOrEmpty(
                                configuration.GetValue<string>($"{EurekaInstanceOptions.EurekaInstanceConfigurationPrefix}:HealthCheckUrlPath")))
                            {
                                Type healthOptionsType = ReflectionHelpers.FindType(new[]
                                {
                                    endpointAssembly
                                }, new[]
                                {
                                    "Steeltoe.Management.Endpoint.Health.HealthEndpointOptions"
                                });

                                object healthOptions = GetOptionsMonitor(serviceProvider, healthOptionsType);

                                if (healthOptions != null)
                                {
                                    options.HealthCheckUrlPath =
                                        basePath + ((string)endpointOptionsBaseType.GetProperty("Path")?.GetValue(healthOptions))?.TrimStart('/');
                                }
                            }

                            if (string.IsNullOrEmpty(
                                configuration.GetValue<string>($"{EurekaInstanceOptions.EurekaInstanceConfigurationPrefix}:StatusPageUrlPath")))
                            {
                                Type infoOptionsType = ReflectionHelpers.FindType(new[]
                                {
                                    endpointAssembly
                                }, new[]
                                {
                                    "Steeltoe.Management.Endpoint.Info.InfoEndpointOptions"
                                });

                                object infoOptions = GetOptionsMonitor(serviceProvider, infoOptionsType);

                                if (infoOptions != null)
                                {
                                    options.StatusPageUrlPath =
                                        basePath + ((string)endpointOptionsBaseType.GetProperty("Path")?.GetValue(infoOptions))?.TrimStart('/');
                                }
                            }
                        }
                    }

                    EurekaServiceInfo info = GetServiceInfo(configuration);
                    EurekaPostConfigurer.UpdateConfiguration(configuration, info, options, info?.ApplicationInfo ?? appInfo);
                });

        services.TryAddSingleton(serviceProvider =>
        {
            var clientOptions = serviceProvider.GetRequiredService<IOptions<EurekaClientOptions>>();

            return new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(clientOptions.Value.CacheTtl)
            };
        });
    }

    private static object GetOptionsMonitor(IServiceProvider serviceProvider, Type tOptions, string name = "")
    {
        Type optionsMonitor = typeof(IOptionsMonitor<>);
        Type genericOptions = optionsMonitor.MakeGenericType(tOptions);
        object optionsMonitorT = serviceProvider.GetService(genericOptions);

        if (!string.IsNullOrEmpty(name))
        {
            return genericOptions.GetMethod("Get").Invoke(optionsMonitorT, new[]
            {
                name
            });
        }

        return genericOptions.GetProperty("CurrentValue")?.GetValue(optionsMonitorT);
    }

    private void AddEurekaServices(IServiceCollection services)
    {
        services.AddSingleton<EurekaApplicationInfoManager>();
        services.AddSingleton<EurekaDiscoveryManager>();
        services.AddSingleton<EurekaDiscoveryClient>();

        services.AddSingleton<IDiscoveryClient>(p =>
        {
            var eurekaService = p.GetService<EurekaDiscoveryClient>();

            // Wire in health checker if present
            if (eurekaService != null)
            {
                eurekaService.HealthCheckHandler = p.GetService<IHealthCheckHandler>();
            }

            return eurekaService;
        });

        services.AddSingleton<IHealthContributor, EurekaServerHealthContributor>();

        ServiceDescriptor existingHandler = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IHttpClientHandlerProvider));

        if (existingHandler is IHttpClientHandlerProvider handlerProvider)
        {
            AddEurekaHttpClient(services).ConfigurePrimaryHttpMessageHandler(() => handlerProvider.GetHttpClientHandler());
        }
        else
        {
            AddEurekaHttpClient(services).ConfigurePrimaryHttpMessageHandler(serviceProvider =>
            {
                var certOptions = serviceProvider.GetService<IOptionsMonitor<CertificateOptions>>();
                var eurekaOptions = serviceProvider.GetService<IOptionsMonitor<EurekaClientOptions>>();

                return EurekaHttpClient.ConfigureEurekaHttpClientHandler(eurekaOptions.CurrentValue,
                    certOptions is null ? null : new ClientCertificateHttpHandler(certOptions));
            });
        }
    }

    private IHttpClientBuilder AddEurekaHttpClient(IServiceCollection services)
    {
        return services.AddHttpClient<EurekaDiscoveryClient>("Eureka", (services, client) =>
        {
            var clientOptions = services.GetRequiredService<IOptions<EurekaClientOptions>>();

            if (clientOptions.Value.EurekaServerConnectTimeoutSeconds > 0)
            {
                client.Timeout = TimeSpan.FromSeconds(clientOptions.Value.EurekaServerConnectTimeoutSeconds);
            }
        });
    }

    private EurekaServiceInfo GetServiceInfo(IConfiguration configuration)
    {
        ServiceInfoName ??= configuration.GetValue<string>("eureka:serviceInfoName");

        IServiceInfo info = string.IsNullOrEmpty(ServiceInfoName)
            ? GetSingletonDiscoveryServiceInfo(configuration)
            : GetNamedDiscoveryServiceInfo(configuration, ServiceInfoName);

        return info as EurekaServiceInfo;
    }
}
