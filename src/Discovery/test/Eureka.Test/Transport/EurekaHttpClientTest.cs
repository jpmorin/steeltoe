// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Transport;
using Steeltoe.Discovery.Eureka.Util;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test.Transport;

public class EurekaHttpClientTest : AbstractBaseTest
{
    [Fact]
    public void Constructor_Throws_IfConfigNull()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new EurekaHttpClient((IEurekaClientConfiguration)null));
        Assert.Contains("config", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_Throws_IfHeadersNull()
    {
        const IDictionary<string, string> headers = null;
        var ex = Assert.Throws<ArgumentNullException>(() => new EurekaHttpClient(new EurekaClientConfiguration(), headers));
        Assert.Contains("headers", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_Throws_IfServiceUrlBad()
    {
        var configuration = new EurekaClientConfiguration
        {
            EurekaServerServiceUrls = "foobar\\foobar"
        };

        var ex = Assert.Throws<UriFormatException>(() => new EurekaHttpClient(configuration));
        Assert.Contains("URI", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Register_Throws_IfInstanceInfoNull()
    {
        var configuration = new EurekaClientConfiguration();
        var client = new EurekaHttpClient(configuration);
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => client.RegisterAsync(null));
        Assert.Contains("info", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RegisterAsync_ThrowsHttpRequestException_ServerTimeout()
    {
        var configuration = new EurekaClientConfiguration
        {
            EurekaServerServiceUrls = "http://localhost:9999/",
            EurekaServerRetryCount = 0
        };

        var client = new EurekaHttpClient(configuration);
        await Assert.ThrowsAsync<EurekaTransportException>(() => client.RegisterAsync(new InstanceInfo()));
    }

    [Fact]
    public async Task RegisterAsync_InvokesServer_ReturnsStatusCodeAndHeaders()
    {
        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Response = string.Empty;
        TestConfigServerStartup.ReturnStatus = 204;
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(environment.EnvironmentName);
        var server = new TestServer(builder);

        const string uri = "http://localhost:8888/";
        server.BaseAddress = new Uri(uri);
        var configuration = new EurekaInstanceConfiguration();
        var info = InstanceInfo.FromInstanceConfiguration(configuration);

        var clientConfig = new EurekaClientConfiguration
        {
            EurekaServerServiceUrls = uri
        };

        var client = new EurekaHttpClient(clientConfig, server.CreateClient());

        EurekaHttpResponse resp = await client.RegisterAsync(info);
        Assert.NotNull(resp);
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
        Assert.NotNull(resp.Headers);
    }

    [Fact]
    public async Task RegisterAsync_SendsValidPOSTData()
    {
        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Response = string.Empty;
        TestConfigServerStartup.ReturnStatus = 204;
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(environment.EnvironmentName);
        var server = new TestServer(builder);

        const string uri = "http://localhost:8888/";
        server.BaseAddress = new Uri(uri);

        var configuration = new EurekaInstanceConfiguration
        {
            AppName = "foobar"
        };

        var info = InstanceInfo.FromInstanceConfiguration(configuration);

        var clientConfig = new EurekaClientConfiguration
        {
            EurekaServerServiceUrls = uri
        };

        var client = new EurekaHttpClient(clientConfig, server.CreateClient());
        await client.RegisterAsync(info);

        Assert.NotNull(TestConfigServerStartup.LastRequest);
        Assert.Equal("POST", TestConfigServerStartup.LastRequest.Method);
        Assert.Equal("localhost:8888", TestConfigServerStartup.LastRequest.Host.Value);
        Assert.Equal("/apps/FOOBAR", TestConfigServerStartup.LastRequest.Path.Value);

        // Check JSON payload
        var receivedJson = JsonSerializer.Deserialize<JsonInstanceInfoRoot>(new StreamReader(TestConfigServerStartup.LastRequest.Body).ReadToEnd());
        Assert.NotNull(receivedJson);
        Assert.NotNull(receivedJson.Instance);

        // Compare a few random values
        JsonInstanceInfo sentJsonObj = info.ToJsonInstance();
        Assert.Equal(sentJsonObj.ActionType, receivedJson.Instance.ActionType);
        Assert.Equal(sentJsonObj.AppName, receivedJson.Instance.AppName);
        Assert.Equal(sentJsonObj.HostName, receivedJson.Instance.HostName);
    }

    [Fact]
    public async Task SendHeartbeat_Throws_IfAppNameNull()
    {
        var configuration = new EurekaClientConfiguration();
        var client = new EurekaHttpClient(configuration);
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => client.SendHeartBeatAsync(null, "bar", new InstanceInfo(), InstanceStatus.Down));
        Assert.Contains("appName", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendHeartbeat_Throws_IfIdNull()
    {
        var configuration = new EurekaClientConfiguration();
        var client = new EurekaHttpClient(configuration);
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => client.SendHeartBeatAsync("foo", null, new InstanceInfo(), InstanceStatus.Down));
        Assert.Contains("id", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendHeartbeat_Throws_IfInstanceInfoNull()
    {
        var configuration = new EurekaClientConfiguration();
        var client = new EurekaHttpClient(configuration);
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => client.SendHeartBeatAsync("foo", "bar", null, InstanceStatus.Down));
        Assert.Contains("info", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendHeartBeatAsync_InvokesServer_ReturnsStatusCodeAndHeaders()
    {
        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Response = string.Empty;
        TestConfigServerStartup.ReturnStatus = 200;
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(environment.EnvironmentName);
        var server = new TestServer(builder);

        const string uri = "http://localhost:8888/";
        server.BaseAddress = new Uri(uri);

        var configuration = new EurekaInstanceConfiguration
        {
            AppName = "foo",
            InstanceId = "id1"
        };

        var info = InstanceInfo.FromInstanceConfiguration(configuration);

        var clientConfig = new EurekaClientConfiguration
        {
            EurekaServerServiceUrls = uri
        };

        var client = new EurekaHttpClient(clientConfig, server.CreateClient());
        EurekaHttpResponse<InstanceInfo> resp = await client.SendHeartBeatAsync("foo", "id1", info, InstanceStatus.Unknown);
        Assert.NotNull(resp);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.NotNull(resp.Headers);

        Assert.Equal("PUT", TestConfigServerStartup.LastRequest.Method);
        Assert.Equal("localhost:8888", TestConfigServerStartup.LastRequest.Host.Value);
        Assert.Equal("/apps/FOO/id1", TestConfigServerStartup.LastRequest.Path.Value);
        long time = DateTimeConversions.ToJavaMillis(new DateTime(info.LastDirtyTimestamp, DateTimeKind.Utc));
        Assert.Equal($"?status=STARTING&lastDirtyTimestamp={time}", TestConfigServerStartup.LastRequest.QueryString.Value);
    }

    [Fact]
    public async Task GetApplicationsAsync_InvokesServer_ReturnsExpectedApplications()
    {
        const string json = @"
                { 
                    ""applications"": { 
                        ""versions__delta"":""1"",
                        ""apps__hashcode"":""UP_1_"",
                        ""application"":[{
                            ""name"":""FOO"",
                            ""instance"":[{ 
                                ""instanceId"":""localhost:foo"",
                                ""hostName"":""localhost"",
                                ""app"":""FOO"",
                                ""ipAddr"":""192.168.56.1"",
                                ""status"":""UP"",
                                ""overriddenstatus"":""UNKNOWN"",
                                ""port"":{""$"":8080,""@enabled"":""true""},
                                ""securePort"":{""$"":443,""@enabled"":""false""},
                                ""countryId"":1,
                                ""dataCenterInfo"":{""@class"":""com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo"",""name"":""MyOwn""},
                                ""leaseInfo"":{""renewalIntervalInSecs"":30,""durationInSecs"":90,""registrationTimestamp"":1457714988223,""lastRenewalTimestamp"":1457716158319,""evictionTimestamp"":0,""serviceUpTimestamp"":1457714988223},
                                ""metadata"":{""@class"":""java.util.Collections$EmptyMap""},
                                ""homePageUrl"":""http://localhost:8080/"",
                                ""statusPageUrl"":""http://localhost:8080/info"",
                                ""healthCheckUrl"":""http://localhost:8080/health"",
                                ""vipAddress"":""foo"",
                                ""isCoordinatingDiscoveryServer"":""false"",
                                ""lastUpdatedTimestamp"":""1457714988223"",
                                ""lastDirtyTimestamp"":""1457714988172"",
                                ""actionType"":""ADDED""
                            }]
                        }]
                    }
                }";

        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Response = json;
        TestConfigServerStartup.ReturnStatus = 200;
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(environment.EnvironmentName);
        var server = new TestServer(builder);

        const string uri = "http://localhost:8888/";
        server.BaseAddress = new Uri(uri);

        var clientConfig = new EurekaClientConfiguration
        {
            EurekaServerServiceUrls = uri
        };

        var client = new EurekaHttpClient(clientConfig, server.CreateClient());
        EurekaHttpResponse<Applications> resp = await client.GetApplicationsAsync();
        Assert.NotNull(resp);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("GET", TestConfigServerStartup.LastRequest.Method);
        Assert.Equal("localhost:8888", TestConfigServerStartup.LastRequest.Host.Value);
        Assert.Equal("/apps/", TestConfigServerStartup.LastRequest.Path.Value);
        Assert.NotNull(resp.Headers);
        Assert.NotNull(resp.Response);
        Assert.NotNull(resp.Response.ApplicationMap);
        Assert.Single(resp.Response.ApplicationMap);
        Application app = resp.Response.GetRegisteredApplication("foo");

        Assert.NotNull(app);
        Assert.Equal("FOO", app.Name);

        IList<InstanceInfo> instances = app.Instances;
        Assert.NotNull(instances);
        Assert.Equal(1, instances.Count);

        foreach (InstanceInfo instance in instances)
        {
            Assert.Equal("localhost:foo", instance.InstanceId);
            Assert.Equal("foo", instance.VipAddress);
            Assert.Equal("localhost", instance.HostName);
            Assert.Equal("192.168.56.1", instance.IPAddress);
            Assert.Equal(InstanceStatus.Up, instance.Status);
        }
    }

    [Fact]
    public async Task GetVipAsync_Throws_IfVipAddressNull()
    {
        var configuration = new EurekaClientConfiguration();
        var client = new EurekaHttpClient(configuration);
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetVipAsync(null));
        Assert.Contains("vipAddress", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetSecureVipAsync_Throws_IfVipAddressNull()
    {
        var configuration = new EurekaClientConfiguration();
        var client = new EurekaHttpClient(configuration);
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetSecureVipAsync(null));
        Assert.Contains("secureVipAddress", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetApplicationAsync_Throws_IfAppNameNull()
    {
        var configuration = new EurekaClientConfiguration();
        var client = new EurekaHttpClient(configuration);
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetApplicationAsync(null));
        Assert.Contains("appName", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetApplicationAsync_InvokesServer_ReturnsExpectedApplications()
    {
        const string json = @"
                {
                    ""application"": {
                        ""name"":""FOO"",
                        ""instance"":[ {
                            ""instanceId"":""localhost:foo"",
                            ""hostName"":""localhost"",
                            ""app"":""FOO"",
                            ""ipAddr"":""192.168.56.1"",
                            ""status"":""UP"",
                            ""overriddenstatus"":""UNKNOWN"",
                            ""port"":{""$"":8080,""@enabled"":""true""},
                            ""securePort"":{""$"":443,""@enabled"":""false""},
                            ""countryId"":1,
                            ""dataCenterInfo"":{""@class"":""com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo"",""name"":""MyOwn""},
                            ""leaseInfo"":{""renewalIntervalInSecs"":30,""durationInSecs"":90,""registrationTimestamp"":1458152330783,""lastRenewalTimestamp"":1458243422342,""evictionTimestamp"":0,""serviceUpTimestamp"":1458152330783},
                            ""metadata"":{""@class"":""java.util.Collections$EmptyMap""},
                            ""homePageUrl"":""http://localhost:8080/"",
                            ""statusPageUrl"":""http://localhost:8080/info"",
                            ""healthCheckUrl"":""http://localhost:8080/health"",
                            ""vipAddress"":""foo"",
                            ""isCoordinatingDiscoveryServer"":""false"",
                            ""lastUpdatedTimestamp"":""1458152330783"",
                            ""lastDirtyTimestamp"":""1458152330696"",
                            ""actionType"":""ADDED""
                        }]
                    }
                }";

        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Response = json;
        TestConfigServerStartup.ReturnStatus = 200;
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(environment.EnvironmentName);
        var server = new TestServer(builder);

        const string uri = "http://localhost:8888/";
        server.BaseAddress = new Uri(uri);

        var clientConfig = new EurekaClientConfiguration
        {
            EurekaServerServiceUrls = uri
        };

        var client = new EurekaHttpClient(clientConfig, server.CreateClient());
        EurekaHttpResponse<Application> resp = await client.GetApplicationAsync("foo");
        Assert.NotNull(resp);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("GET", TestConfigServerStartup.LastRequest.Method);
        Assert.Equal("localhost:8888", TestConfigServerStartup.LastRequest.Host.Value);
        Assert.Equal("/apps/foo", TestConfigServerStartup.LastRequest.Path.Value);
        Assert.NotNull(resp.Headers);
        Assert.NotNull(resp.Response);
        Assert.Equal("FOO", resp.Response.Name);

        IList<InstanceInfo> instances = resp.Response.Instances;
        Assert.NotNull(instances);
        Assert.Equal(1, instances.Count);

        foreach (InstanceInfo instance in instances)
        {
            Assert.Equal("localhost:foo", instance.InstanceId);
            Assert.Equal("foo", instance.VipAddress);
            Assert.Equal("localhost", instance.HostName);
            Assert.Equal("192.168.56.1", instance.IPAddress);
            Assert.Equal(InstanceStatus.Up, instance.Status);
        }

        Assert.Equal("http://localhost:8888/", client.ServiceUrl);
    }

    [Fact]
    public async Task GetApplicationAsync__FirstServerFails_InvokesSecondServer_ReturnsExpectedApplications()
    {
        const string json = @"
                {
                    ""application"": {
                        ""name"":""FOO"",
                        ""instance"":[{
                            ""instanceId"":""localhost:foo"",
                            ""hostName"":""localhost"",
                            ""app"":""FOO"",
                            ""ipAddr"":""192.168.56.1"",
                            ""status"":""UP"",
                            ""overriddenstatus"":""UNKNOWN"",
                            ""port"":{""$"":8080,""@enabled"":""true""},
                            ""securePort"":{""$"":443,""@enabled"":""false""},
                            ""countryId"":1,
                            ""dataCenterInfo"":{""@class"":""com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo"",""name"":""MyOwn""},
                            ""leaseInfo"":{""renewalIntervalInSecs"":30,""durationInSecs"":90,""registrationTimestamp"":1458152330783,""lastRenewalTimestamp"":1458243422342,""evictionTimestamp"":0,""serviceUpTimestamp"":1458152330783},
                            ""metadata"":{""@class"":""java.util.Collections$EmptyMap""},
                            ""homePageUrl"":""http://localhost:8080/"",
                            ""statusPageUrl"":""http://localhost:8080/info"",
                            ""healthCheckUrl"":""http://localhost:8080/health"",
                            ""vipAddress"":""foo"",
                            ""isCoordinatingDiscoveryServer"":""false"",
                            ""lastUpdatedTimestamp"":""1458152330783"",
                            ""lastDirtyTimestamp"":""1458152330696"",
                            ""actionType"":""ADDED""
                        }]
                    }
                }";

        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Response = json;
        TestConfigServerStartup.ReturnStatus = 200;
        TestConfigServerStartup.Host = "localhost:8888";
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(environment.EnvironmentName);
        var server = new TestServer(builder);

        const string uri = "http://localhost:8888/";
        server.BaseAddress = new Uri(uri);

        var clientConfig = new EurekaClientConfiguration
        {
            EurekaServerServiceUrls = $"https://bad.host:9999/,{uri}"
        };

        var client = new EurekaHttpClient(clientConfig, server.CreateClient());
        EurekaHttpResponse<Application> resp = await client.GetApplicationAsync("foo");
        Assert.NotNull(resp);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("GET", TestConfigServerStartup.LastRequest.Method);
        Assert.Equal("localhost:8888", TestConfigServerStartup.LastRequest.Host.Value);
        Assert.Equal("/apps/foo", TestConfigServerStartup.LastRequest.Path.Value);
        Assert.NotNull(resp.Headers);
        Assert.NotNull(resp.Response);
        Assert.Equal("FOO", resp.Response.Name);

        IList<InstanceInfo> instances = resp.Response.Instances;
        Assert.NotNull(instances);
        Assert.Equal(1, instances.Count);

        foreach (InstanceInfo instance in instances)
        {
            Assert.Equal("localhost:foo", instance.InstanceId);
            Assert.Equal("foo", instance.VipAddress);
            Assert.Equal("localhost", instance.HostName);
            Assert.Equal("192.168.56.1", instance.IPAddress);
            Assert.Equal(InstanceStatus.Up, instance.Status);
        }

        Assert.Equal("http://localhost:8888/", client.ServiceUrl);
    }

    [Fact]
    public async Task GetInstanceAsync_Throws_IfAppNameNull()
    {
        var configuration = new EurekaClientConfiguration();
        var client = new EurekaHttpClient(configuration);
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetInstanceAsync(null, "id"));
        Assert.Contains("appName", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetInstanceAsync_Throws_IfAppNameNotNullAndIDNull()
    {
        var configuration = new EurekaClientConfiguration();
        var client = new EurekaHttpClient(configuration);
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetInstanceAsync("appName", null));
        Assert.Contains("id", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetInstanceAsync_Throws_IfIDNull()
    {
        var configuration = new EurekaClientConfiguration();
        var client = new EurekaHttpClient(configuration);
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetInstanceAsync(null));
        Assert.Contains("id", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetInstanceAsync_InvokesServer_ReturnsExpectedInstances()
    {
        const string json = @"
                { 
                    ""instance"": {
                        ""instanceId"":""DESKTOP-GNQ5SUT"",
                        ""app"":""FOOBAR"",
                        ""appGroupName"":null,
                        ""ipAddr"":""192.168.0.147"",
                        ""sid"":""na"",
                        ""port"":{""@enabled"":true,""$"":80},
                        ""securePort"":{""@enabled"":false,""$"":443},
                        ""homePageUrl"":""http://DESKTOP-GNQ5SUT:80/"",
                        ""statusPageUrl"":""http://DESKTOP-GNQ5SUT:80/Status"",
                        ""healthCheckUrl"":""http://DESKTOP-GNQ5SUT:80/healthcheck"",
                        ""secureHealthCheckUrl"":null,
                        ""vipAddress"":""DESKTOP-GNQ5SUT:80"",
                        ""secureVipAddress"":""DESKTOP-GNQ5SUT:443"",
                        ""countryId"":1,
                        ""dataCenterInfo"":{""@class"":""com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo"",""name"":""MyOwn""},
                        ""hostName"":""DESKTOP-GNQ5SUT"",
                        ""status"":""UP"",
                        ""overriddenstatus"":""UNKNOWN"",
                        ""leaseInfo"":{""renewalIntervalInSecs"":30,""durationInSecs"":90,""registrationTimestamp"":0,""lastRenewalTimestamp"":0,""renewalTimestamp"":0,""evictionTimestamp"":0,""serviceUpTimestamp"":0},
                        ""isCoordinatingDiscoveryServer"":false,
                        ""metadata"":{""@class"":""java.util.Collections$EmptyMap"",""metadata"":null},
                        ""lastUpdatedTimestamp"":1458116137663,
                        ""lastDirtyTimestamp"":1458116137663,
                        ""actionType"":""ADDED"",
                        ""asgName"":null
                    }
                }";

        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Response = json;
        TestConfigServerStartup.ReturnStatus = 200;
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(environment.EnvironmentName);
        var server = new TestServer(builder);

        const string uri = "http://localhost:8888/";
        server.BaseAddress = new Uri(uri);

        var clientConfig = new EurekaClientConfiguration
        {
            EurekaServerServiceUrls = uri
        };

        var client = new EurekaHttpClient(clientConfig, server.CreateClient());
        EurekaHttpResponse<InstanceInfo> resp = await client.GetInstanceAsync("DESKTOP-GNQ5SUT");
        Assert.NotNull(resp);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("GET", TestConfigServerStartup.LastRequest.Method);
        Assert.Equal("localhost:8888", TestConfigServerStartup.LastRequest.Host.Value);
        Assert.Equal("/instances/DESKTOP-GNQ5SUT", TestConfigServerStartup.LastRequest.Path.Value);
        Assert.NotNull(resp.Headers);
        Assert.NotNull(resp.Response);
        Assert.Equal("DESKTOP-GNQ5SUT", resp.Response.InstanceId);
        Assert.Equal("DESKTOP-GNQ5SUT:80", resp.Response.VipAddress);
        Assert.Equal("DESKTOP-GNQ5SUT", resp.Response.HostName);
        Assert.Equal("192.168.0.147", resp.Response.IPAddress);
        Assert.Equal(InstanceStatus.Up, resp.Response.Status);

        Assert.Equal("http://localhost:8888/", client.ServiceUrl);
    }

    [Fact]
    public async Task GetInstanceAsync_FirstServerFails_InvokesSecondServer_ReturnsExpectedInstances()
    {
        const string json = @"
                { 
                    ""instance"":{
                        ""instanceId"":""DESKTOP-GNQ5SUT"",
                        ""app"":""FOOBAR"",
                        ""appGroupName"":null,
                        ""ipAddr"":""192.168.0.147"",
                        ""sid"":""na"",
                        ""port"":{""@enabled"":true,""$"":80},
                        ""securePort"":{""@enabled"":false,""$"":443},
                        ""homePageUrl"":""http://DESKTOP-GNQ5SUT:80/"",
                        ""statusPageUrl"":""http://DESKTOP-GNQ5SUT:80/Status"",
                        ""healthCheckUrl"":""http://DESKTOP-GNQ5SUT:80/healthcheck"",
                        ""secureHealthCheckUrl"":null,
                        ""vipAddress"":""DESKTOP-GNQ5SUT:80"",
                        ""secureVipAddress"":""DESKTOP-GNQ5SUT:443"",
                        ""countryId"":1,
                        ""dataCenterInfo"":{""@class"":""com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo"",""name"":""MyOwn""},
                        ""hostName"":""DESKTOP-GNQ5SUT"",
                        ""status"":""UP"",
                        ""overriddenstatus"":""UNKNOWN"",
                        ""leaseInfo"":{""renewalIntervalInSecs"":30,""durationInSecs"":90,""registrationTimestamp"":0,""lastRenewalTimestamp"":0,""renewalTimestamp"":0,""evictionTimestamp"":0,""serviceUpTimestamp"":0},
                        ""isCoordinatingDiscoveryServer"":false,
                        ""metadata"":{""@class"":""java.util.Collections$EmptyMap"",""metadata"":null},
                        ""lastUpdatedTimestamp"":1458116137663,
                        ""lastDirtyTimestamp"":1458116137663,
                        ""actionType"":""ADDED"",
                        ""asgName"":null
                    }
                }";

        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Response = json;
        TestConfigServerStartup.ReturnStatus = 200;
        TestConfigServerStartup.Host = "localhost:8888";
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(environment.EnvironmentName);
        var server = new TestServer(builder);

        const string uri = "http://localhost:8888/";
        server.BaseAddress = new Uri(uri);

        var clientConfig = new EurekaClientConfiguration
        {
            EurekaServerServiceUrls = $"https://bad.host:9999/,{uri}"
        };

        var client = new EurekaHttpClient(clientConfig, server.CreateClient());
        EurekaHttpResponse<InstanceInfo> resp = await client.GetInstanceAsync("DESKTOP-GNQ5SUT");
        Assert.NotNull(resp);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("GET", TestConfigServerStartup.LastRequest.Method);
        Assert.Equal("localhost:8888", TestConfigServerStartup.LastRequest.Host.Value);
        Assert.Equal("/instances/DESKTOP-GNQ5SUT", TestConfigServerStartup.LastRequest.Path.Value);
        Assert.NotNull(resp.Headers);
        Assert.NotNull(resp.Response);
        Assert.Equal("DESKTOP-GNQ5SUT", resp.Response.InstanceId);
        Assert.Equal("DESKTOP-GNQ5SUT:80", resp.Response.VipAddress);
        Assert.Equal("DESKTOP-GNQ5SUT", resp.Response.HostName);
        Assert.Equal("192.168.0.147", resp.Response.IPAddress);
        Assert.Equal(InstanceStatus.Up, resp.Response.Status);

        Assert.Equal("http://localhost:8888/", client.ServiceUrl);
    }

    [Fact]
    public async Task CancelAsync_Throws_IfAppNameNull()
    {
        var configuration = new EurekaClientConfiguration();
        var client = new EurekaHttpClient(configuration);
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => client.CancelAsync(null, "id"));
        Assert.Contains("appName", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CancelAsync_Throws_IfAppNameNotNullAndIDNull()
    {
        var configuration = new EurekaClientConfiguration();
        var client = new EurekaHttpClient(configuration);
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => client.CancelAsync("appName", null));
        Assert.Contains("id", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CancelAsync_InvokesServer_ReturnsStatusCodeAndHeaders()
    {
        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Response = string.Empty;
        TestConfigServerStartup.ReturnStatus = 200;
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(environment.EnvironmentName);
        var server = new TestServer(builder);

        const string uri = "http://localhost:8888/";
        server.BaseAddress = new Uri(uri);

        var clientConfig = new EurekaClientConfiguration
        {
            EurekaServerServiceUrls = uri
        };

        var client = new EurekaHttpClient(clientConfig, server.CreateClient());
        EurekaHttpResponse resp = await client.CancelAsync("foo", "bar");
        Assert.NotNull(resp);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.NotNull(resp.Headers);
        Assert.Equal("DELETE", TestConfigServerStartup.LastRequest.Method);
        Assert.Equal("localhost:8888", TestConfigServerStartup.LastRequest.Host.Value);
        Assert.Equal("/apps/foo/bar", TestConfigServerStartup.LastRequest.Path.Value);

        Assert.Equal("http://localhost:8888/", client.ServiceUrl);
    }

    [Fact]
    public async Task StatusUpdateAsync_Throws_IfAppNameNull()
    {
        var configuration = new EurekaClientConfiguration();
        var client = new EurekaHttpClient(configuration);
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => client.StatusUpdateAsync(null, "id", InstanceStatus.Up, null));
        Assert.Contains("appName", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StatusUpdateAsync_Throws_IfIdNull()
    {
        var configuration = new EurekaClientConfiguration();
        var client = new EurekaHttpClient(configuration);
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => client.StatusUpdateAsync("appName", null, InstanceStatus.Up, null));
        Assert.Contains("id", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StatusUpdateAsync_Throws_IfInstanceInfoNull()
    {
        var configuration = new EurekaClientConfiguration();
        var client = new EurekaHttpClient(configuration);
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => client.StatusUpdateAsync("appName", "bar", InstanceStatus.Up, null));
        Assert.Contains("info", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StatusUpdateAsync_InvokesServer_ReturnsStatusCodeAndHeaders()
    {
        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Response = string.Empty;
        TestConfigServerStartup.ReturnStatus = 200;
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(environment.EnvironmentName);
        var server = new TestServer(builder);

        const string uri = "http://localhost:8888/";
        server.BaseAddress = new Uri(uri);

        var clientConfig = new EurekaClientConfiguration
        {
            EurekaServerServiceUrls = uri
        };

        var client = new EurekaHttpClient(clientConfig, server.CreateClient());
        long now = DateTime.UtcNow.Ticks;
        long javaTime = DateTimeConversions.ToJavaMillis(new DateTime(now, DateTimeKind.Utc));

        EurekaHttpResponse resp = await client.StatusUpdateAsync("foo", "bar", InstanceStatus.Down, new InstanceInfo
        {
            LastDirtyTimestamp = now
        });

        Assert.NotNull(resp);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.NotNull(resp.Headers);
        Assert.Equal("PUT", TestConfigServerStartup.LastRequest.Method);
        Assert.Equal("localhost:8888", TestConfigServerStartup.LastRequest.Host.Value);
        Assert.Equal("/apps/foo/bar/status", TestConfigServerStartup.LastRequest.Path.Value);
        Assert.Equal($"?value=DOWN&lastDirtyTimestamp={javaTime}", TestConfigServerStartup.LastRequest.QueryString.Value);

        Assert.Equal("http://localhost:8888/", client.ServiceUrl);
    }

    [Fact]
    public async Task DeleteStatusOverrideAsync_Throws_IfAppNameNull()
    {
        var configuration = new EurekaClientConfiguration();
        var client = new EurekaHttpClient(configuration);
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteStatusOverrideAsync(null, "id", null));
        Assert.Contains("appName", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteStatusOverrideAsync_Throws_IfIdNull()
    {
        var configuration = new EurekaClientConfiguration();
        var client = new EurekaHttpClient(configuration);
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteStatusOverrideAsync("appName", null, null));
        Assert.Contains("id", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteStatusOverrideAsync_Throws_IfInstanceInfoNull()
    {
        var configuration = new EurekaClientConfiguration();
        var client = new EurekaHttpClient(configuration);
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => client.DeleteStatusOverrideAsync("appName", "bar", null));
        Assert.Contains("info", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteStatusOverrideAsync_InvokesServer_ReturnsStatusCodeAndHeaders()
    {
        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment();
        TestConfigServerStartup.Response = string.Empty;
        TestConfigServerStartup.ReturnStatus = 200;
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestConfigServerStartup>().UseEnvironment(environment.EnvironmentName);
        var server = new TestServer(builder);

        const string uri = "http://localhost:8888/";
        server.BaseAddress = new Uri(uri);

        var clientConfig = new EurekaClientConfiguration
        {
            EurekaServerServiceUrls = uri
        };

        var client = new EurekaHttpClient(clientConfig, server.CreateClient());
        long now = DateTime.UtcNow.Ticks;
        long javaTime = DateTimeConversions.ToJavaMillis(new DateTime(now, DateTimeKind.Utc));

        EurekaHttpResponse resp = await client.DeleteStatusOverrideAsync("foo", "bar", new InstanceInfo
        {
            LastDirtyTimestamp = now
        });

        Assert.NotNull(resp);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.NotNull(resp.Headers);
        Assert.Equal("DELETE", TestConfigServerStartup.LastRequest.Method);
        Assert.Equal("localhost:8888", TestConfigServerStartup.LastRequest.Host.Value);
        Assert.Equal("/apps/foo/bar/status", TestConfigServerStartup.LastRequest.Path.Value);
        Assert.Equal($"?lastDirtyTimestamp={javaTime}", TestConfigServerStartup.LastRequest.QueryString.Value);

        Assert.Equal("http://localhost:8888/", client.ServiceUrl);
    }

    [Fact]
    public void MakeServiceUrl_Throws_IfServiceUrlBad()
    {
        var ex = Assert.Throws<UriFormatException>(() => EurekaHttpClient.MakeServiceUrl("foobar\\foobar"));
        Assert.Contains("URI", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MakeServiceUrl_AppendsSlash_IfMissing()
    {
        string result = EurekaHttpClient.MakeServiceUrl("http://boo:123");
        Assert.Equal("http://boo:123/", result);
    }

    [Fact]
    public void MakeServiceUrl_DoesNotAppendSlash_IfPresent()
    {
        string result = EurekaHttpClient.MakeServiceUrl("http://boo:123/");
        Assert.Equal("http://boo:123/", result);
    }

    [Fact]
    public void GetRequestMessage_ReturnsCorrectMessage_WithAdditionalHeaders()
    {
        var headers = new Dictionary<string, string>
        {
            { "foo", "bar" }
        };

        var configuration = new EurekaClientConfiguration
        {
            EurekaServerServiceUrls = "http://boo:123/eureka/"
        };

        var client = new EurekaHttpClient(configuration, headers);
        HttpRequestMessage result = client.GetRequestMessage(HttpMethod.Post, new Uri("http://boo:123/eureka/"));
        Assert.Equal(HttpMethod.Post, result.Method);
        Assert.Equal(new Uri("http://boo:123/eureka/"), result.RequestUri);
        Assert.True(result.Headers.Contains("foo"));
    }

    [Fact]
    public void GetRequestMessage_No_Auth_When_Credentials_Not_In_Url()
    {
        var configuration = new EurekaClientConfiguration
        {
            EurekaServerServiceUrls = "http://boo:123/eureka/"
        };

        var client = new EurekaHttpClient(configuration);
        HttpRequestMessage result = client.GetRequestMessage(HttpMethod.Post, new Uri(configuration.EurekaServerServiceUrls));
        Assert.Equal(HttpMethod.Post, result.Method);
        Assert.Equal(new Uri("http://boo:123/eureka/"), result.RequestUri);
        Assert.False(result.Headers.Contains("Authorization"));

        var clientOptions = new EurekaClientOptions
        {
            ServiceUrl = "http://boo:123/eureka/"
        };

        var optionsMonitor = new TestOptionMonitorWrapper<EurekaClientOptions>(clientOptions);
        client = new EurekaHttpClient(optionsMonitor);

        result = client.GetRequestMessage(HttpMethod.Post, new Uri(clientOptions.EurekaServerServiceUrls));

        Assert.Equal(HttpMethod.Post, result.Method);
        Assert.Equal(new Uri("http://boo:123/eureka/"), result.RequestUri);
        Assert.False(result.Headers.Contains("Authorization"));
    }

    [Fact]
    public void GetRequestMessage_Adds_Auth_When_Credentials_In_Url()
    {
        var configuration = new EurekaClientConfiguration
        {
            EurekaServerServiceUrls = "http://user:pass@boo:123/eureka/"
        };

        var client = new EurekaHttpClient(configuration);
        HttpRequestMessage result = client.GetRequestMessage(HttpMethod.Post, new Uri(configuration.EurekaServerServiceUrls));
        Assert.Equal(HttpMethod.Post, result.Method);
        Assert.Equal(new Uri("http://boo:123/eureka/"), result.RequestUri);
        Assert.True(result.Headers.Contains("Authorization"));

        var clientOptions = new EurekaClientOptions
        {
            ServiceUrl = "http://user:pass@boo:123/eureka/"
        };

        var optionsMonitor = new TestOptionMonitorWrapper<EurekaClientOptions>(clientOptions);
        client = new EurekaHttpClient(optionsMonitor);

        result = client.GetRequestMessage(HttpMethod.Post, new Uri(clientOptions.EurekaServerServiceUrls));

        Assert.Equal(HttpMethod.Post, result.Method);
        Assert.Equal(new Uri("http://boo:123/eureka/"), result.RequestUri);
        Assert.True(result.Headers.Contains("Authorization"));
    }

    [Fact]
    public void GetRequestMessage_Adds_Auth_When_JustPassword_In_Url()
    {
        var configuration = new EurekaClientConfiguration
        {
            EurekaServerServiceUrls = "http://:pass@boo:123/eureka/"
        };

        var client = new EurekaHttpClient(configuration);
        HttpRequestMessage result = client.GetRequestMessage(HttpMethod.Post, new Uri(configuration.EurekaServerServiceUrls));
        Assert.Equal(HttpMethod.Post, result.Method);
        Assert.Equal(new Uri("http://boo:123/eureka/"), result.RequestUri);
        Assert.True(result.Headers.Contains("Authorization"));

        var clientOptions = new EurekaClientOptions
        {
            ServiceUrl = "http://:pass@boo:123/eureka/"
        };

        var optionsMonitor = new TestOptionMonitorWrapper<EurekaClientOptions>(clientOptions);
        client = new EurekaHttpClient(optionsMonitor);

        result = client.GetRequestMessage(HttpMethod.Post, new Uri(clientOptions.EurekaServerServiceUrls));

        Assert.Equal(HttpMethod.Post, result.Method);
        Assert.Equal(new Uri("http://boo:123/eureka/"), result.RequestUri);
        Assert.True(result.Headers.Contains("Authorization"));
    }

    [Fact]
    public void GetRequestUri_ReturnsCorrect_WithQueryArguments()
    {
        var configuration = new EurekaClientConfiguration
        {
            EurekaServerServiceUrls = "http://boo:123/eureka/"
        };

        var client = new EurekaHttpClient(configuration, new HttpClient());

        var queryArgs = new Dictionary<string, string>
        {
            { "foo", "bar" },
            { "bar", "foo" }
        };

        Uri result = client.GetRequestUri("http://boo:123/eureka", queryArgs);
        Assert.NotNull(result);
        Assert.Equal("http://boo:123/eureka?foo=bar&bar=foo", result.ToString());
    }

    [Fact]
    public void GetServiceUrlCandidates_NoFailingUrls_ReturnsExpected()
    {
        var configuration = new EurekaClientConfiguration
        {
            EurekaServerServiceUrls = "http://user:pass@boo:123/eureka/,http://user:pass@foo:123/eureka"
        };

        var client = new EurekaHttpClient(configuration);
        IList<string> result = client.GetServiceUrlCandidates();
        Assert.Contains("http://user:pass@boo:123/eureka/", result);
        Assert.Contains("http://user:pass@foo:123/eureka/", result);
    }

    [Fact]
    public void GetServiceUrlCandidates_WithFailingUrls_ReturnsExpected()
    {
        var configuration = new EurekaClientConfiguration
        {
            EurekaServerServiceUrls =
                "https://user:pass@boo:123/eureka/,https://user:pass@foo:123/eureka,https://user:pass@blah:123/eureka,https://user:pass@blah.blah:123/eureka"
        };

        var client = new EurekaHttpClient(configuration);
        client.AddToFailingServiceUrls("https://user:pass@foo:123/eureka/");
        client.AddToFailingServiceUrls("https://user:pass@blah.blah:123/eureka/");

        IList<string> result = client.GetServiceUrlCandidates();
        Assert.Contains("https://user:pass@boo:123/eureka/", result);
        Assert.Contains("https://user:pass@blah:123/eureka/", result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void GetServiceUrlCandidates_ThresholdHit_ReturnsExpected()
    {
        var configuration = new EurekaClientConfiguration
        {
            EurekaServerServiceUrls = "http://user:pass@boo:123/eureka/,http://user:pass@foo:123/eureka"
        };

        var client = new EurekaHttpClient(configuration);
        client.AddToFailingServiceUrls("http://user:pass@foo:123/eureka/");

        IList<string> result = client.GetServiceUrlCandidates();
        Assert.Contains("http://user:pass@boo:123/eureka/", result);
        Assert.Contains("http://user:pass@foo:123/eureka/", result);
    }
}
