// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Common.Reflection;

namespace Steeltoe.Connector.Redis;

public class RedisCacheConnectorOptions : AbstractServiceConnectorOptions
{
    private const string DefaultHost = "localhost";
    private const int DefaultPort = 6379;
    private const string RedisClientSectionPrefix = "redis:client";
    private readonly bool _bindingsFound;

    // Configure either a single Host/Port or optionally provide
    // a list of endpoints (ie. host1:port1,host2:port2)
    public string Host { get; set; } = DefaultHost;

    public int Port { get; set; } = DefaultPort;

    public string EndPoints { get; set; }

    public string Password { get; set; }

    public bool AllowAdmin { get; set; }

    public string ClientName { get; set; }

    public int ConnectRetry { get; set; }

    public int ConnectTimeout { get; set; }

    public bool AbortOnConnectFail { get; set; } = true;

    public int KeepAlive { get; set; }

    public bool ResolveDns { get; set; }

    public string ServiceName { get; set; }

    public bool Ssl { get; set; }

    public string SslHost { get; set; }

    public string TieBreaker { get; set; }

    public int WriteBuffer { get; set; }

    public int SyncTimeout { get; set; }

    // You can use this instead of configuring each option separately
    // If a connection string is provided, the string will be used and
    // the options above will be ignored
    public string ConnectionString { get; set; }

    // This configuration option specific to https://github.com/aspnet/Caching
    public string InstanceName { get; set; }

    public RedisCacheConnectorOptions()
        : base(',', DefaultSeparator)
    {
    }

    public RedisCacheConnectorOptions(IConfiguration configuration)
        : base(',', DefaultSeparator)
    {
        ArgumentGuard.NotNull(configuration);

        IConfigurationSection section = configuration.GetSection(RedisClientSectionPrefix);
        section.Bind(this);

        _bindingsFound = configuration.HasCloudFoundryServiceConfigurations() || configuration.HasKubernetesServiceBindings();
    }

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(ConnectionString) && !_bindingsFound)
        {
            return ConnectionString;
        }

        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(EndPoints))
        {
            string endpoints = EndPoints.Trim();
            sb.Append(endpoints);
            sb.Append(',');
        }
        else
        {
            sb.Append($"{Host}:{Port}");
            sb.Append(',');
        }

        AddKeyValue(sb, "password", Password);
        AddKeyValue(sb, "allowAdmin", AllowAdmin);
        AddKeyValue(sb, "name", ClientName);

        if (ConnectRetry > 0)
        {
            AddKeyValue(sb, "connectRetry", ConnectRetry);
        }

        if (ConnectTimeout > 0)
        {
            AddKeyValue(sb, "connectTimeout", ConnectTimeout);
        }

        AddKeyValue(sb, "abortConnect", AbortOnConnectFail);

        if (KeepAlive > 0)
        {
            AddKeyValue(sb, "keepAlive", KeepAlive);
        }

        AddKeyValue(sb, "resolveDns", ResolveDns);
        AddKeyValue(sb, "serviceName", ServiceName);
        AddKeyValue(sb, "ssl", Ssl);
        AddKeyValue(sb, "sslHost", SslHost);
        AddKeyValue(sb, "tiebreaker", TieBreaker);

        if (WriteBuffer > 0)
        {
            AddKeyValue(sb, "writeBuffer", WriteBuffer);
        }

        if (SyncTimeout > 0)
        {
            AddKeyValue(sb, "syncTimeout", SyncTimeout);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Get a Redis configuration object for use with Microsoft.Extensions.Caching.Redis.
    /// </summary>
    /// <param name="optionsType">
    /// Expects Microsoft.Extensions.Caching.Redis.RedisCacheOptions.
    /// </param>
    /// <returns>
    /// This object typed as RedisCacheOptions.
    /// </returns>
    public object ToMicrosoftExtensionObject(Type optionsType)
    {
        object microsoftConnection = Activator.CreateInstance(optionsType);
        microsoftConnection.GetType().GetProperty("Configuration").SetValue(microsoftConnection, ToString());
        microsoftConnection.GetType().GetProperty("InstanceName").SetValue(microsoftConnection, InstanceName);

        return microsoftConnection;
    }

    /// <summary>
    /// Get a Redis configuration object for use with StackExchange.Redis.
    /// </summary>
    /// <param name="optionsType">
    /// Expects StackExchange.Redis.ConfigurationOptions.
    /// </param>
    /// <returns>
    /// This object typed as ConfigurationOptions.
    /// </returns>
    /// <remarks>
    /// Includes comma in password detection and workaround for https://github.com/SteeltoeOSS/Connectors/issues/10.
    /// </remarks>
    public object ToStackExchangeObject(Type optionsType)
    {
        object stackObject = Activator.CreateInstance(optionsType);

        // to remove this comma workaround, follow up on https://github.com/StackExchange/StackExchange.Redis/issues/680
        string tempPassword = Password;
        bool resetPassword = false;

        if (Password?.Contains(',') == true)
        {
            Password = string.Empty;
            resetPassword = true;
        }

        // this return is effectively "StackExchange.Redis.ConfigurationOptions.Parse(this.ToString())"
        object configuration = optionsType.GetMethod(nameof(int.Parse), new[]
        {
            typeof(string)
        }).Invoke(stackObject, new object[]
        {
            ToString()
        });

        if (resetPassword)
        {
            ReflectionHelpers.TrySetProperty(configuration, "Password", tempPassword);
        }

        return configuration;
    }

    // Note: The code below lifted from StackExchange.Redis.Format
    internal static EndPoint TryParseEndPoint(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return null;
        }

        string host;
        int port;
        int i = endpoint.IndexOf(':');

        if (i < 0)
        {
            host = endpoint;
            port = 0;
        }
        else
        {
            host = endpoint.Substring(0, i);
            string portAsString = endpoint.Substring(i + 1);

            if (string.IsNullOrEmpty(portAsString))
            {
                return null;
            }

            if (int.TryParse(portAsString, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out port))
            {
                return null;
            }
        }

        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        return ParseEndPoint(host, port);
    }

    internal static EndPoint ParseEndPoint(string host, int port)
    {
        if (IPAddress.TryParse(host, out IPAddress ip))
        {
            return new IPEndPoint(ip, port);
        }

        return new DnsEndPoint(host, port);
    }
}
