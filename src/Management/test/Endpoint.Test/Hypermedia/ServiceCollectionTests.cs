// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Hypermedia;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Hypermedia;

public class ServiceCollectionTests
{
    [Fact]
    public void AddHypermediaActuatorServices_ThrowsOnNulls()
    {
        const IServiceCollection services = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddHypermediaActuatorServices());
        Assert.Contains(nameof(services), ex.Message, StringComparison.Ordinal);
    }
}
