// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Health.Contributor;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Health.Contributor;

public class DiskSpaceContributorTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var contrib = new DiskSpaceContributor();
        Assert.Equal("diskSpace", contrib.Id);
    }

    [Fact]
    public void Health_InitializedWithDefaults_ReturnsExpected()
    {
        var contrib = new DiskSpaceContributor();
        Assert.Equal("diskSpace", contrib.Id);
        HealthCheckResult result = contrib.Health();
        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Up, result.Status);
        Assert.NotNull(result.Details);
        Assert.True(result.Details.ContainsKey("total"));
        Assert.True(result.Details.ContainsKey("free"));
        Assert.True(result.Details.ContainsKey("threshold"));
        Assert.True(result.Details.ContainsKey("status"));
    }
}
