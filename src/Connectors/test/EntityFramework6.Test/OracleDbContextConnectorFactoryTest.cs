// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Connector.Oracle;
using Steeltoe.Connector.Oracle.EntityFramework6;
using Steeltoe.Connector.Services;
using Xunit;

namespace Steeltoe.Connector.EntityFramework6.Test;

public class OracleDbContextConnectorFactoryTest
{
    [Fact]
    public void Constructor_ThrowsIfTypeNull()
    {
        var options = new OracleProviderConnectorOptions();
        const OracleServiceInfo si = null;
        const Type dbContextType = null;

        var ex = Assert.Throws<ArgumentNullException>(() => new OracleDbContextConnectorFactory(si, options, dbContextType));
        Assert.Contains(nameof(dbContextType), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_ThrowsIfNoValidConstructorFound()
    {
        var options = new OracleProviderConnectorOptions();
        const OracleServiceInfo si = null;
        Type dbContextType = typeof(BadOracleDbContext);

        var ex = Assert.Throws<ConnectorException>(() => new OracleDbContextConnectorFactory(si, options, dbContextType).Create(null));
        Assert.Contains("BadOracleDbContext", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_ReturnsDbContext()
    {
        var options = new OracleProviderConnectorOptions
        {
            Server = "localhost",
            Port = 1521,
            Password = "I2rK7m8vGPs=1",
            Username = "SYSTEM",
            ServiceName = "ORCLCDB"
        };

        var si = new OracleServiceInfo("MyId", "Oracle://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355");
        var factory = new OracleDbContextConnectorFactory(si, options, typeof(GoodOracleDbContext));
        object context = factory.Create(null);
        Assert.NotNull(context);
        var goodOracleDbContext = context as GoodOracleDbContext;
        Assert.NotNull(goodOracleDbContext);
    }
}
