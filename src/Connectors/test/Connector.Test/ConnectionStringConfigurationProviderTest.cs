// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common.Utils.IO;
using Steeltoe.Connector.Test.Redis;
using Xunit;

namespace Steeltoe.Connector.Test;

public class ConnectionStringConfigurationProviderTest
{
    [Fact]
    public void ConstructorThrowsOnNullProviders()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new ConnectionStringConfigurationProvider(null));
        Assert.Equal("providers", ex.ParamName);
    }

    [Fact]
    public void TryGetReadsBasicConnectorConnectionString()
    {
        var appSettings = new Dictionary<string, string>
        {
            { "redis:client:host", "testHost" }
        };

        var provider = new ConnectionStringConfigurationProvider(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build().Providers);
        Assert.True(provider.TryGet("connectionstrings:redis", out string connectionString));
        Assert.Equal("testHost:6379,allowAdmin=false,abortConnect=true,resolveDns=false,ssl=false", connectionString);
    }

    [Fact]
    public void TryGetReadsServiceBindingConnectionString()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", string.Empty);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", string.Empty);

        var provider = new ConnectionStringConfigurationProvider(new ConfigurationBuilder()
            .AddInMemoryCollection(RedisCacheTestHelpers.SingleServerAsDictionary).Build().Providers);

        Assert.True(provider.TryGet("connectionstrings:myRedisService", out string connectionString), "TryGet ConnectionStrings:myRedisService");

        Assert.Equal("192.168.0.103:60287,password=133de7c8-9f3a-4df1-8a10-676ba7ddaa10,allowAdmin=false,abortConnect=true,resolveDns=false,ssl=false",
            connectionString);
    }

    [Fact]
    public void TryGetReturnsFalseWhenNotFound()
    {
        var provider = new ConnectionStringConfigurationProvider(new ConfigurationBuilder().Build().Providers);
        Assert.False(provider.TryGet("connectionstrings:myRedisService", out _));
    }

    [Fact]
    public void TryGetReturnsHandlesUnexpectedUsagePatterns()
    {
        var provider = new ConnectionStringConfigurationProvider(new ConfigurationBuilder().Build().Providers);
        Assert.False(provider.TryGet("connectionstringsmyRedisService", out _));
        Assert.False(provider.TryGet("connectionstrings:myRedisService:banana", out _));
    }

    [Fact]
    public void ProviderSeesConfigUpdates()
    {
        const string appSettings1 = @"{ ""redis"": { ""client"": { ""host"": ""testHost"" } } }";
        const string appSettings2 = @"{ ""redis"": { ""client"": { ""host"": ""updatedTestHost"" } } }";
        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile("appsettings.json", appSettings1);
        string directory = Path.GetDirectoryName(path);
        string fileName = Path.GetFileName(path);

        IEnumerable<IConfigurationProvider> baseProviders =
            new ConfigurationBuilder().SetBasePath(directory).AddJsonFile(fileName, false, true).Build().Providers;

        var provider = new ConnectionStringConfigurationProvider(baseProviders);
        IChangeToken token = provider.GetReloadToken();

        Assert.True(provider.TryGet("connectionstrings:redis", out string connectionString));
        Assert.Equal("testHost:6379,allowAdmin=false,abortConnect=true,resolveDns=false,ssl=false", connectionString);

        File.WriteAllText(path, appSettings2);
        Thread.Sleep(2000);

        Assert.True(token.HasChanged);
        Assert.True(provider.TryGet("connectionstrings:redis", out connectionString));
        Assert.Equal("updatedTestHost:6379,allowAdmin=false,abortConnect=true,resolveDns=false,ssl=false", connectionString);
    }
}
