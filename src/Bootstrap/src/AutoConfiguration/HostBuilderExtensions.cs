// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#pragma warning disable 0436 // Type conflicts with imported type

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Configuration.ConfigServer;
using Steeltoe.Configuration.Kubernetes;
using Steeltoe.Configuration.Placeholder;
using Steeltoe.Configuration.RandomValue;
using Steeltoe.Connector;
using Steeltoe.Connector.MongoDb;
using Steeltoe.Connector.MySql;
using Steeltoe.Connector.Oracle;
using Steeltoe.Connector.PostgreSql;
using Steeltoe.Connector.RabbitMQ;
using Steeltoe.Connector.Redis;
using Steeltoe.Connector.SqlServer;
using Steeltoe.Discovery.Client;
using Steeltoe.Logging.DynamicSerilog;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Kubernetes;
using Steeltoe.Management.Prometheus;
using Steeltoe.Management.Tracing;
using Steeltoe.Management.Wavefront;
using Steeltoe.Security.Authentication.CloudFoundry;

namespace Steeltoe.Bootstrap.AutoConfiguration;

public static class HostBuilderExtensions
{
    private const string LoggerName = "Steeltoe.AutoConfiguration";
    private static ILoggerFactory _loggerFactory;

    static HostBuilderExtensions()
    {
        AppDomain currentDomain = AppDomain.CurrentDomain;
        currentDomain.AssemblyResolve += AssemblyExtensions.LoadAnyVersion;
    }

    /// <summary>
    /// Automatically configure Steeltoe packages that have been added as NuGet references.
    /// <para />
    /// PLEASE NOTE: No extensions to IApplicationBuilder will be configured.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your <see cref="IHostBuilder" />.
    /// </param>
    /// <param name="exclusions">
    /// A list of assemblies to exclude from auto-configuration. For ease of use, select from <see cref="SteeltoeAssemblies" />.
    /// </param>
    /// <param name="loggerFactory">
    /// For logging within auto-configuration.
    /// </param>
    public static IHostBuilder AddSteeltoe(this IHostBuilder hostBuilder, IEnumerable<string> exclusions = null, ILoggerFactory loggerFactory = null)
    {
        AssemblyExtensions.ExcludedAssemblies = exclusions ?? new List<string>();
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        ILogger logger = _loggerFactory.CreateLogger(LoggerName);
        hostBuilder.Properties[LoggerName] = logger;

        if (!hostBuilder.WireIfLoaded(WireConfigServer, SteeltoeAssemblies.SteeltoeExtensionsConfigurationConfigServer))
        {
            hostBuilder.WireIfLoaded(WireCloudFoundryConfiguration, SteeltoeAssemblies.SteeltoeExtensionsConfigurationCloudFoundry);
        }

        if (Platform.IsKubernetes && AssemblyExtensions.IsAssemblyLoaded(SteeltoeAssemblies.SteeltoeExtensionsConfigurationKubernetes))
        {
            WireKubernetesConfiguration(hostBuilder);
        }

        hostBuilder.WireIfLoaded(WireRandomValueProvider, SteeltoeAssemblies.SteeltoeExtensionsConfigurationRandomValue);

        hostBuilder.WireIfLoaded(WirePlaceholderResolver, SteeltoeAssemblies.SteeltoeExtensionsConfigurationPlaceholder);

        if (hostBuilder.WireIfLoaded(WireConnectorConfiguration, SteeltoeAssemblies.SteeltoeConnector))
        {
            hostBuilder.WireIfAnyLoaded(WireMySqlConnection, MySqlTypeLocator.Assemblies);
            hostBuilder.WireIfAnyLoaded(WireMongoClient, MongoDbTypeLocator.Assemblies);
            hostBuilder.WireIfAnyLoaded(WireOracleConnection, OracleTypeLocator.Assemblies);
            hostBuilder.WireIfAnyLoaded(WirePostgreSqlConnection, PostgreSqlTypeLocator.Assemblies);
            hostBuilder.WireIfAnyLoaded(WireRabbitMqConnection, RabbitMQTypeLocator.Assemblies);
            hostBuilder.WireIfAnyLoaded(WireRedisConnectionMultiplexer, RedisTypeLocator.StackExchangeAssemblies);

            hostBuilder.WireIfAnyLoaded(WireDistributedRedisCache, RedisTypeLocator.MicrosoftAssemblies.Except(new[]
            {
                "Microsoft.Extensions.Caching.Abstractions"
            }).ToArray());

            hostBuilder.WireIfAnyLoaded(WireSqlServerConnection, SqlServerTypeLocator.Assemblies);
        }

        hostBuilder.WireIfLoaded(WireDynamicSerilog, SteeltoeAssemblies.SteeltoeExtensionsLoggingDynamicSerilog);
        hostBuilder.WireIfLoaded(WireDiscoveryClient, SteeltoeAssemblies.SteeltoeDiscoveryClient);

        if (AssemblyExtensions.IsAssemblyLoaded(SteeltoeAssemblies.SteeltoeManagementKubernetes))
        {
            hostBuilder.WireIfLoaded(WireKubernetesActuators, SteeltoeAssemblies.SteeltoeManagementKubernetes);
        }
        else
        {
            hostBuilder.WireIfLoaded(WireAllActuators, SteeltoeAssemblies.SteeltoeManagementEndpoint);
        }

        hostBuilder.WireIfLoaded(WireSteeltoePrometheus, SteeltoeAssemblies.SteeltoePrometheus);

        hostBuilder.WireIfLoaded(WireWavefrontMetrics, SteeltoeAssemblies.SteeltoeWavefront);

        hostBuilder.WireIfLoaded(WireDistributedTracing, SteeltoeAssemblies.SteeltoeManagementTracing);

        hostBuilder.WireIfLoaded(WireCloudFoundryContainerIdentity, SteeltoeAssemblies.SteeltoeSecurityAuthenticationCloudFoundry);
        return hostBuilder;
    }

    private static bool WireIfLoaded(this IHostBuilder hostBuilder, Action<IHostBuilder> action, params string[] assembly)
    {
        if (assembly.All(AssemblyExtensions.IsAssemblyLoaded))
        {
            action(hostBuilder);
            return true;
        }

        return false;
    }

    private static void WireIfAnyLoaded(this IHostBuilder hostBuilder, Action<IHostBuilder> action, params string[] assembly)
    {
        if (assembly.Any(AssemblyExtensions.IsAssemblyLoaded))
        {
            action(hostBuilder);
        }
    }

    private static void Log(this IHostBuilder host, string message)
    {
        var logger = (ILogger)host.Properties[LoggerName];
        logger.LogInformation(message);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireConfigServer(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration((context, cfg) => cfg.AddConfigServer(context.HostingEnvironment, _loggerFactory))
            .ConfigureServices((_, services) => services.AddConfigServerServices()).Log(LogMessages.WireConfigServer);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireCloudFoundryConfiguration(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(cfg => cfg.AddCloudFoundry()).Log(LogMessages.WireCloudFoundryConfiguration);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireKubernetesConfiguration(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(cfg => cfg.AddKubernetes(_loggerFactory))
            .ConfigureServices(serviceCollection => serviceCollection.AddKubernetesConfigurationServices()).Log(LogMessages.WireKubernetesConfiguration);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireRandomValueProvider(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(cfg => cfg.AddRandomValueSource(_loggerFactory)).Log(LogMessages.WireRandomValueProvider);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WirePlaceholderResolver(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(cfg => cfg.AddPlaceholderResolver(_loggerFactory)).Log(LogMessages.WirePlaceholderResolver);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireConnectorConfiguration(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration((_, svc) => svc.AddConnectionStrings()).Log(LogMessages.WireConnectorsConfiguration);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireMySqlConnection(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices((host, svc) => svc.AddMySqlConnection(host.Configuration)).Log(LogMessages.WireMySqlConnection);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireMongoClient(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices((host, svc) => svc.AddMongoClient(host.Configuration)).Log(LogMessages.WireMongoClient);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireOracleConnection(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices((host, svc) => svc.AddOracleConnection(host.Configuration)).Log(LogMessages.WireOracleConnection);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WirePostgreSqlConnection(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices((host, svc) => svc.AddPostgreSqlConnection(host.Configuration)).Log(LogMessages.WirePostgreSqlConnection);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireRabbitMqConnection(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices((host, svc) => svc.AddRabbitMQConnection(host.Configuration)).Log(LogMessages.WireRabbitMqConnection);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireRedisConnectionMultiplexer(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices((host, svc) => svc.AddRedisConnectionMultiplexer(host.Configuration)).Log(LogMessages.WireRedisConnectionMultiplexer);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireDistributedRedisCache(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices((host, svc) => svc.AddDistributedRedisCache(host.Configuration)).Log(LogMessages.WireDistributedRedisCache);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireSqlServerConnection(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices((host, svc) => svc.AddSqlServerConnection(host.Configuration)).Log(LogMessages.WireSqlServerConnection);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireDiscoveryClient(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices((host, svc) => svc.AddDiscoveryClient(host.Configuration)).Log(LogMessages.WireDiscoveryClient);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireDistributedTracing(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices((_, svc) => svc.AddDistributedTracingAspNetCore()).Log(LogMessages.WireDistributedTracing);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireKubernetesActuators(this IHostBuilder hostBuilder)
    {
        hostBuilder.AddKubernetesActuators().Log(LogMessages.WireKubernetesActuators);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireAllActuators(this IHostBuilder hostBuilder)
    {
        hostBuilder.AddAllActuators().Log(LogMessages.WireAllActuators);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireWavefrontMetrics(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices((context, collection) =>
        {
            if (context.Configuration.HasWavefront())
            {
                collection.AddWavefrontMetrics();
            }
        });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireSteeltoePrometheus(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices((context, collection) => collection.AddPrometheusActuator());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireDynamicSerilog(this IHostBuilder hostBuilder)
    {
        hostBuilder.AddDynamicSerilog().Log(LogMessages.WireDynamicSerilog);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireCloudFoundryContainerIdentity(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(cfg => cfg.AddCloudFoundryContainerIdentity()).ConfigureServices((_, svc) => svc.AddCloudFoundryCertificateAuth())
            .Log(LogMessages.WireCloudFoundryContainerIdentity);
    }
}
