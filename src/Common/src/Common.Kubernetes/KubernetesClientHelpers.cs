// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using k8s.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Common.Kubernetes;

public static class KubernetesClientHelpers
{
    public static IKubernetes GetKubernetesClient(IConfiguration configuration, Action<KubernetesClientConfiguration> kubernetesClientConfiguration = null,
        ILogger logger = null)
    {
        return GetKubernetesClient(new KubernetesApplicationOptions(configuration), kubernetesClientConfiguration, logger);
    }

    public static IKubernetes GetKubernetesClient(KubernetesApplicationOptions appInfo,
        Action<KubernetesClientConfiguration> kubernetesClientConfiguration = null, ILogger logger = null)
    {
        KubernetesClientConfiguration configuration = null;

        try
        {
            if (appInfo.Config.Paths.Any())
            {
                char delimiter = Platform.IsWindows ? ';' : ':';
                string joinedPaths = appInfo.Config.Paths.Aggregate((i, j) => i + delimiter + j);
                Environment.SetEnvironmentVariable("KUBECONFIG", joinedPaths);
            }

            configuration = KubernetesClientConfiguration.BuildDefaultConfig();
        }
        catch (KubeConfigException e)
        {
            // couldn't locate .kube\config or user-identified files. use an empty configuration object and fall back on user-defined Action to set the configuration
            logger?.LogWarning(e, "Failed to build KubernetesClientConfiguration, creating an empty configuration...");
        }

        // BuildDefaultConfig() doesn't set a host if KubeConfigException is thrown
        configuration ??= new KubernetesClientConfiguration
        {
            Host = "http://localhost:8080"
        };

        kubernetesClientConfiguration?.Invoke(configuration);

        return new k8s.Kubernetes(configuration);
    }
}
