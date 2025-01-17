// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Steeltoe.Common.Hosting.Test;

public class HostBuilderExtensionsTest
{
    [Fact]
    public void UseCloudHosting_Web_ThrowsIfHostBuilderNull()
    {
        const IWebHostBuilder webHostBuilder = null;

        var ex = Assert.Throws<ArgumentNullException>(() => webHostBuilder.UseCloudHosting());
        Assert.Contains(nameof(webHostBuilder), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UseCloudHosting_Default8080()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().UseStartup<TestServerStartup>().UseKestrel();

        hostBuilder.UseCloudHosting();
        IWebHost server = hostBuilder.Build();

        var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
        Assert.Contains("http://*:8080", addresses.Addresses);
    }

    [Fact]
    public void UseCloudHosting_MakeSureThePortIsSet()
    {
        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);
            Environment.SetEnvironmentVariable("PORT", "42");
            IWebHostBuilder hostBuilder = new WebHostBuilder().UseStartup<TestServerStartup>().UseKestrel();

            hostBuilder.UseCloudHosting();
            IWebHost server = hostBuilder.Build();

            var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
            Assert.Contains("http://*:42", addresses.Addresses);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PORT", null);
        }
    }

    [Fact]
    public void UseCloudHosting_ReadsTyePorts()
    {
        try
        {
            Environment.SetEnvironmentVariable("PORT", "80;443");
            IWebHostBuilder hostBuilder = new WebHostBuilder().UseStartup<TestServerStartup>().UseKestrel();

            hostBuilder.UseCloudHosting();
            IWebHost server = hostBuilder.Build();

            var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
            Assert.Contains("http://*:80", addresses.Addresses);
            Assert.Contains("https://*:443", addresses.Addresses);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PORT", null);
        }
    }

    [Fact]
    public void UseCloudHosting_SeesTyePortsAndUsesAspNetCoreURL()
    {
        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://*:80;https://*:443");
            Environment.SetEnvironmentVariable("PORT", "88;4443");
            IWebHostBuilder hostBuilder = new WebHostBuilder().UseStartup<TestServerStartup>().UseKestrel();

            hostBuilder.UseCloudHosting();
            IWebHost server = hostBuilder.Build();

            var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
            Assert.Contains("http://*:80", addresses.Addresses);
            Assert.Contains("https://*:443", addresses.Addresses);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);
            Environment.SetEnvironmentVariable("PORT", null);
        }
    }

    [Fact]
    public void UseCloudHosting_UsesServerPort()
    {
        try
        {
            Environment.SetEnvironmentVariable("SERVER_PORT", "42");
            Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);
            IWebHostBuilder hostBuilder = new WebHostBuilder().UseStartup<TestServerStartup>().UseKestrel();

            hostBuilder.UseCloudHosting();
            IWebHost server = hostBuilder.Build();

            var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
            Assert.Contains("http://*:42", addresses.Addresses);
        }
        finally
        {
            Environment.SetEnvironmentVariable("SERVER_PORT", null);
        }
    }

    [Fact]
    public void UseCloudHosting_UsesCommandLine_ServerUrls()
    {
        IConfigurationRoot config = new ConfigurationBuilder().AddCommandLine(new[]
        {
            "--server.urls",
            "http://*:8081"
        }).Build();

        IWebHostBuilder hostBuilder = new WebHostBuilder().UseConfiguration(config).UseStartup<TestServerStartup>().UseKestrel();

        hostBuilder.UseCloudHosting();
        IWebHost server = hostBuilder.Build();

        var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();

        Assert.Single(addresses.Addresses);
        Assert.Contains("http://*:8081", addresses.Addresses);
    }

    [Fact]
    public void UseCloudHosting_MultipleVariantsWorkTogether()
    {
        try
        {
            Environment.SetEnvironmentVariable("SERVER_PORT", "8080");

            IConfigurationRoot config = new ConfigurationBuilder().AddCommandLine(new[]
            {
                "--urls",
                "http://0.0.0.0:8080"
            }).Build();

            IWebHostBuilder hostBuilder = new WebHostBuilder().UseConfiguration(config).UseStartup<TestServerStartup>().UseKestrel();

            hostBuilder.UseCloudHosting();
            IWebHost server = hostBuilder.Build();

            var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();

            Assert.Single(addresses.Addresses);
            Assert.Contains("http://*:8080", addresses.Addresses);
        }
        finally
        {
            Environment.SetEnvironmentVariable("SERVER_PORT", null);
        }
    }

    [Fact]
    public void UseCloudHosting_UsesCommandLine_Urls()
    {
        IConfigurationRoot config = new ConfigurationBuilder().AddCommandLine(new[]
        {
            "--urls",
            "http://*:8081"
        }).Build();

        IWebHostBuilder hostBuilder = new WebHostBuilder().UseConfiguration(config).UseStartup<TestServerStartup>().UseKestrel();

        hostBuilder.UseCloudHosting();
        IWebHost server = hostBuilder.Build();

        var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();

        Assert.Single(addresses.Addresses);
        Assert.Contains("http://*:8081", addresses.Addresses);
    }

    [Fact]
    public void UseCloudHosting_WebApplication_Default8080()
    {
        WebApplicationBuilder hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.UseCloudHosting();
        using WebApplication host = hostBuilder.Build();
        host.Start();

        var addressFeature = ((IApplicationBuilder)host).ServerFeatures.Get<IServerAddressesFeature>();
        Assert.Single(addressFeature.Addresses);
        Assert.Equal("http://[::]:8080", addressFeature.Addresses.First());
    }

    [Fact]
    public void UseCloudHosting_WebApplication_MakeSureThePortIsSet()
    {
        Environment.SetEnvironmentVariable("PORT", "5042");
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);

        try
        {
            WebApplicationBuilder hostBuilder = WebApplication.CreateBuilder();

            hostBuilder.UseCloudHosting();
            using WebApplication host = hostBuilder.Build();
            host.Start();

            var addressFeature = ((IApplicationBuilder)host).ServerFeatures.Get<IServerAddressesFeature>();
            Assert.Single(addressFeature.Addresses);
            Assert.Equal("http://[::]:5042", addressFeature.Addresses.First());
        }
        finally
        {
            Environment.SetEnvironmentVariable("PORT", null);
        }
    }

    [Fact]
    public void UseCloudHosting_WebApplication_IsStateful()
    {
        Environment.SetEnvironmentVariable("PORT", "5044");

        try
        {
            WebApplicationBuilder hostBuilder = WebApplication.CreateBuilder();

            hostBuilder.UseCloudHosting();
            Environment.SetEnvironmentVariable("PORT", null);
            Environment.SetEnvironmentVariable("SERVER_PORT", "5055");

            hostBuilder.UseCloudHosting();
            using WebApplication host = hostBuilder.Build();
            host.Start();

            var addressFeature = ((IApplicationBuilder)host).ServerFeatures.Get<IServerAddressesFeature>();
            Assert.Contains("http://[::]:5044", addressFeature.Addresses);
            Assert.Contains("http://[::]:5055", addressFeature.Addresses);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PORT", null);
            Environment.SetEnvironmentVariable("SERVER_PORT", null);
            Environment.SetEnvironmentVariable("SERVER_PORT", null);
        }
    }
}
