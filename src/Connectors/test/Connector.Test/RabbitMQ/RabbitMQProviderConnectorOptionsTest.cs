// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.RabbitMQ;
using Xunit;

namespace Steeltoe.Connector.Test.RabbitMQ;

public class RabbitMQProviderConnectorOptionsTest
{
    [Fact]
    public void Constructor_ThrowsIfConfigNull()
    {
        const IConfiguration configuration = null;

        var ex = Assert.Throws<ArgumentNullException>(() => new RabbitMQProviderConnectorOptions(configuration));
        Assert.Contains(nameof(configuration), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_Binds_Rabbit_Values()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["rabbit:client:server"] = "localhost",
            ["rabbit:client:port"] = "1234",
            ["rabbit:client:password"] = "password",
            ["rabbit:client:username"] = "username",
            ["rabbit:client:sslEnabled"] = "true"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var options = new RabbitMQProviderConnectorOptions(configurationRoot);
        Assert.Equal("localhost", options.Server);
        Assert.Equal(1234, options.Port);
        Assert.Equal("password", options.Password);
        Assert.Equal("username", options.Username);
        Assert.Null(options.Uri);
        Assert.True(options.SslEnabled);
        Assert.Equal(RabbitMQProviderConnectorOptions.DefaultSslPort, options.SslPort);
    }

    [Fact]
    public void Constructor_Binds_RabbitMQ_Values()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["rabbitmq:client:server"] = "localhost",
            ["rabbitmq:client:port"] = "1234",
            ["rabbitmq:client:password"] = "password",
            ["rabbitmq:client:username"] = "username",
            ["rabbitmq:client:sslEnabled"] = "true"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var options = new RabbitMQProviderConnectorOptions(configurationRoot);
        Assert.Equal("localhost", options.Server);
        Assert.Equal(1234, options.Port);
        Assert.Equal("password", options.Password);
        Assert.Equal("username", options.Username);
        Assert.Null(options.Uri);
        Assert.True(options.SslEnabled);
        Assert.Equal(RabbitMQProviderConnectorOptions.DefaultSslPort, options.SslPort);
    }

    [Fact]
    public void ToString_ReturnsValid()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["rabbit:client:server"] = "localhost",
            ["rabbit:client:port"] = "1234",
            ["rabbit:client:password"] = "password",
            ["rabbit:client:username"] = "username",
            ["rabbit:client:virtualHost"] = "foobar",
            ["rabbit:client:sslEnabled"] = "true"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var options = new RabbitMQProviderConnectorOptions(configurationRoot);
        string result = options.ToString();
        Assert.Equal("amqps://username:password@localhost:5671/foobar", result);
    }
}
