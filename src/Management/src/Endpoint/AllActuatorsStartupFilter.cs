// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.ManagementPort;

namespace Steeltoe.Management.Endpoint;

public class AllActuatorsStartupFilter : IStartupFilter
{
    private readonly ActuatorConventionBuilder _conventionBuilder;

    public AllActuatorsStartupFilter(ActuatorConventionBuilder conventionBuilder)
    {
        _conventionBuilder = conventionBuilder;
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            if (app.ApplicationServices.GetService<ICorsService>() != null)
            {
                app.UseCors("SteeltoeManagement");
            }

            if (Platform.IsCloudFoundry)
            {
                app.UseCloudFoundrySecurity();
            }

            app.UseMiddleware<ManagementPortMiddleware>();
            next(app);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapAllActuators(_conventionBuilder);
            });

            app.ApplicationServices.InitializeAvailability();
        };
    }
}
