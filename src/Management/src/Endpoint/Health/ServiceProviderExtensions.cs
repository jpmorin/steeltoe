// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Availability;

namespace Steeltoe.Management.Endpoint.Health;

public static class ServiceProviderExtensions
{
    /// <summary>
    /// Register events to trigger initial and shutting down values for Readiness and Liveness states of <see cref="ApplicationAvailability" />.
    /// </summary>
    /// <param name="serviceProvider">
    /// <see cref="IServiceProvider" /> for your application.
    /// </param>
    public static IServiceProvider InitializeAvailability(this IServiceProvider serviceProvider)
    {
        var availability = serviceProvider.GetService<ApplicationAvailability>();

        if (availability != null)
        {
            var lifetime = serviceProvider.GetService<IHostApplicationLifetime>();

            lifetime.ApplicationStarted.Register(() =>
            {
                availability.SetAvailabilityState(ApplicationAvailability.LivenessKey, LivenessState.Correct, "ApplicationStarted");
                availability.SetAvailabilityState(ApplicationAvailability.ReadinessKey, ReadinessState.AcceptingTraffic, "ApplicationStarted");
            });

            lifetime.ApplicationStopping.Register(() =>
                availability.SetAvailabilityState(ApplicationAvailability.ReadinessKey, ReadinessState.RefusingTraffic, "ApplicationStopping"));
        }

        return serviceProvider;
    }
}
