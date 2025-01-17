// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Common;
using Steeltoe.Common.Reflection;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.Redis;

public class RedisServiceConnectorFactory
{
    private readonly RedisServiceInfo _info;
    private readonly RedisCacheConnectorOptions _config;
    private readonly RedisCacheConfigurer _configurer = new();

    protected Type ConnectorType { get; set; }

    protected Type OptionsType { get; set; }

    protected MethodInfo Initializer { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisServiceConnectorFactory" /> class. Factory for creating Redis connections with either
    /// Microsoft.Extensions.Caching.Redis or StackExchange.Redis.
    /// </summary>
    /// <param name="serviceInfo">
    /// Service Info.
    /// </param>
    /// <param name="options">
    /// Service Configuration.
    /// </param>
    /// <param name="connectionType">
    /// Redis connection Type.
    /// </param>
    /// <param name="optionsType">
    /// Options Type used to establish connection.
    /// </param>
    /// <param name="initializer">
    /// Method used to open connection.
    /// </param>
    public RedisServiceConnectorFactory(RedisServiceInfo serviceInfo, RedisCacheConnectorOptions options, Type connectionType, Type optionsType,
        MethodInfo initializer)
    {
        ArgumentGuard.NotNull(options);

        _info = serviceInfo;
        _config = options;
        ConnectorType = connectionType;
        OptionsType = optionsType;
        Initializer = initializer;
    }

    /// <summary>
    /// Get the connection string from Configuration sources.
    /// </summary>
    /// <returns>
    /// Connection String.
    /// </returns>
    public string GetConnectionString()
    {
        RedisCacheConnectorOptions connectionOptions = _configurer.Configure(_info, _config);
        return connectionOptions.ToString();
    }

    /// <summary>
    /// Open the Redis connection.
    /// </summary>
    /// <param name="provider">
    /// IServiceProvider.
    /// </param>
    /// <returns>
    /// Initialized Redis connection.
    /// </returns>
    public virtual object Create(IServiceProvider provider)
    {
        RedisCacheConnectorOptions connectionOptions = _configurer.Configure(_info, _config);

        object result = Initializer == null
            ? CreateConnection(connectionOptions.ToMicrosoftExtensionObject(OptionsType))
            : CreateConnectionByMethod(connectionOptions.ToStackExchangeObject(OptionsType));

        return result;
    }

    private object CreateConnection(object options)
    {
        return ReflectionHelpers.CreateInstance(ConnectorType, new[]
        {
            options
        });
    }

    private object CreateConnectionByMethod(object options)
    {
        return Initializer.Invoke(ConnectorType, new[]
        {
            options,
            null
        });
    }
}
