// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;
using Steeltoe.Management.Diagnostics;
using Steeltoe.Management.Endpoint.Diagnostics;

namespace Steeltoe.Management.Endpoint.Trace;

public static class EndpointServiceCollectionExtensions
{
    /// <summary>
    /// Adds components of the Trace actuator to the D/I container.
    /// </summary>
    /// <param name="services">
    /// Service collection to add trace to.
    /// </param>
    /// <param name="configuration">
    /// Application configuration. Retrieved from the <see cref="IServiceCollection" /> if not provided (this actuator looks for a settings starting with
    /// management:endpoints:trace).
    /// </param>
    public static void AddTraceActuator(this IServiceCollection services, IConfiguration configuration = null)
    {
        services.AddTraceActuator(MediaTypeVersion.V2);
    }

    /// <summary>
    /// Adds components of the Trace actuator to the D/I container.
    /// </summary>
    /// <param name="services">
    /// Service collection to add trace to.
    /// </param>
    /// <param name="version">
    /// <see cref="MediaTypeVersion" /> to use in responses.
    /// </param>
    public static void AddTraceActuator(this IServiceCollection services, MediaTypeVersion version)
    {
        ArgumentGuard.NotNull(services);

        services.TryAddSingleton<IDiagnosticsManager, DiagnosticsManager>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, DiagnosticServices>());
        services.AddCommonActuatorServices();
        services.AddTraceActuatorServices(version);

        switch (version)
        {
            case MediaTypeVersion.V1:

                services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, TraceDiagnosticObserver>());
                services.TryAddSingleton<ITraceRepository, TraceDiagnosticObserver>();
                break;
            default:
                services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, HttpTraceDiagnosticObserver>());
                services.TryAddSingleton<IHttpTraceRepository, HttpTraceDiagnosticObserver>();

                break;
        }
    }
}
