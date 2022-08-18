// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.Oracle;

public class OracleConnectionInfo : IConnectionInfo
{
    public Connection Get(IConfiguration configuration, string serviceName)
    {
        OracleServiceInfo info = string.IsNullOrEmpty(serviceName)
            ? configuration.GetSingletonServiceInfo<OracleServiceInfo>()
            : configuration.GetRequiredServiceInfo<OracleServiceInfo>(serviceName);

        return GetConnection(info, configuration);
    }

    public Connection Get(IConfiguration configuration, IServiceInfo serviceInfo)
    {
        return GetConnection((OracleServiceInfo)serviceInfo, configuration);
    }

    public bool IsSameType(string serviceType)
    {
        return serviceType.Equals("oracle", StringComparison.InvariantCultureIgnoreCase) ||
            serviceType.Equals("oracledb", StringComparison.InvariantCultureIgnoreCase);
    }

    public bool IsSameType(IServiceInfo serviceInfo)
    {
        return serviceInfo is OracleServiceInfo;
    }

    private Connection GetConnection(OracleServiceInfo info, IConfiguration configuration)
    {
        var oracleConfig = new OracleProviderConnectorOptions(configuration);
        var configurer = new OracleProviderConfigurer();
        return new Connection(configurer.Configure(info, oracleConfig), "Oracle", info);
    }
}