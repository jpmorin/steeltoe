// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;

namespace Steeltoe.Management.Task;

public static class TaskWebHostExtensions
{
    /// <summary>
    /// Runs a web application, blocking the calling thread until the host shuts down.
    /// <para />
    /// To execute your task, invoke the application with argument "runtask=taskname", where "taskname" is your task's name.
    /// <para />
    /// Command line arguments should be registered as a configuration source for this functionality to work.
    /// </summary>
    /// <param name="webHost">
    /// The <see cref="T:Microsoft.AspNetCore.Hosting.IWebHost" /> to run.
    /// </param>
    public static void RunWithTasks(this IWebHost webHost)
    {
        ArgumentGuard.NotNull(webHost);

        if (FindAndRunTask(webHost.Services))
        {
            webHost.Dispose();
        }
        else
        {
            webHost.Run();
        }
    }

    /// <summary>
    /// Runs a web application, blocking the calling thread until the host shuts down.
    /// <para />
    /// To execute your task, invoke the application with argument "runtask=taskname", where "taskname" is your task's name.
    /// <para />
    /// Command line arguments should be registered as a configuration source for this functionality to work.
    /// </summary>
    /// <param name="host">
    /// The <see cref="T:Microsoft.AspNetCore.Hosting.IWebHost" /> to run.
    /// </param>
    public static void RunWithTasks(this IHost host)
    {
        ArgumentGuard.NotNull(host);

        if (FindAndRunTask(host.Services))
        {
            host.Dispose();
        }
        else
        {
            host.Run();
        }
    }

    private static bool FindAndRunTask(IServiceProvider services)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        string taskName = configuration.GetValue<string>("runtask");
        IServiceProvider scope = services.CreateScope().ServiceProvider;

        if (taskName != null)
        {
            IApplicationTask task = scope.GetServices<IApplicationTask>()
                .FirstOrDefault(x => string.Equals(x.Name, taskName, StringComparison.OrdinalIgnoreCase));

            if (task != null)
            {
                task.Run();
            }
            else
            {
                ILogger logger = scope.GetService<ILoggerFactory>().CreateLogger("CloudFoundryTasks");
                logger.LogError($"No task with name {taskName} is found registered in service container");
            }

            return true;
        }

        return false;
    }
}
