// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Env;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Env;

public class EndpointServiceCollectionTest : BaseTest
{
    [Fact]
    public void AddEnvActuator_ThrowsOnNulls()
    {
        const IServiceCollection services = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddEnvActuator());
        Assert.Contains(nameof(services), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddEnvActuator_AddsCorrectServices()
    {
        var services = new ServiceCollection();
        IHostEnvironment host = HostingHelpers.GetHostingEnvironment();
        services.AddSingleton(host);

        var appSettings = new Dictionary<string, string>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();
        services.AddSingleton<IConfiguration>(configurationRoot);

        services.AddEnvActuator();

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptionsMonitor<EnvEndpointOptions>>();
        Assert.NotNull(options);
        var ep = serviceProvider.GetService<EnvEndpoint>();
        Assert.NotNull(ep);
    }
}
