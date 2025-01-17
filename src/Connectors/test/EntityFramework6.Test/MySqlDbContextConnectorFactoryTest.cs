// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Connector.MySql;
using Steeltoe.Connector.MySql.EntityFramework6;
using Steeltoe.Connector.Services;
using Xunit;

namespace Steeltoe.Connector.EntityFramework6.Test;

public class MySqlDbContextConnectorFactoryTest
{
    [Fact]
    public void Constructor_ThrowsIfTypeNull()
    {
        var options = new MySqlProviderConnectorOptions();
        const MySqlServiceInfo si = null;
        const Type dbContextType = null;

        var ex = Assert.Throws<ArgumentNullException>(() => new MySqlDbContextConnectorFactory(si, options, dbContextType));
        Assert.Contains(nameof(dbContextType), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_ThrowsIfNoValidConstructorFound()
    {
        var options = new MySqlProviderConnectorOptions();
        const MySqlServiceInfo si = null;
        Type dbContextType = typeof(BadMySqlDbContext);

        var ex = Assert.Throws<ConnectorException>(() => new MySqlDbContextConnectorFactory(si, options, dbContextType).Create(null));
        Assert.Contains("BadMySqlDbContext", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_ReturnsDbContext()
    {
        var options = new MySqlProviderConnectorOptions
        {
            Server = "localhost",
            Port = 3306,
            Password = "password",
            Username = "username",
            Database = "database"
        };

        var si = new MySqlServiceInfo("MyId", "mysql://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355");
        var factory = new MySqlDbContextConnectorFactory(si, options, typeof(GoodMySqlDbContext));
        object context = factory.Create(null);
        Assert.NotNull(context);
        var goodMySqlDbContext = context as GoodMySqlDbContext;
        Assert.NotNull(goodMySqlDbContext);
    }
}
