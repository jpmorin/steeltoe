// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Test.Health.MockContributors;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Health;

public class DefaultHealthAggregatorTest : BaseTest
{
    [Fact]
    public void Aggregate_NullContributorList_ReturnsExpectedHealth()
    {
        var agg = new DefaultHealthAggregator();
        HealthCheckResult result = agg.Aggregate(null);
        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Unknown, result.Status);
        Assert.NotNull(result.Details);
    }

    [Fact]
    public void Aggregate_SingleContributor_ReturnsExpectedHealth()
    {
        var contributors = new List<IHealthContributor>
        {
            new UpContributor()
        };

        var agg = new DefaultHealthAggregator();
        HealthCheckResult result = agg.Aggregate(contributors);
        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Up, result.Status);
        Assert.NotNull(result.Details);
    }

    [Fact]
    public void Aggregate_MultipleContributor_ReturnsExpectedHealth()
    {
        var contributors = new List<IHealthContributor>
        {
            new DownContributor(),
            new UpContributor(),
            new OutOfServiceContributor(),
            new UnknownContributor()
        };

        var agg = new DefaultHealthAggregator();
        HealthCheckResult result = agg.Aggregate(contributors);
        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Down, result.Status);
        Assert.NotNull(result.Details);
    }

    [Fact]
    public void Aggregate_DuplicateContributor_ReturnsExpectedHealth()
    {
        var contributors = new List<IHealthContributor>();

        for (int i = 0; i < 10; i++)
        {
            contributors.Add(new UpContributor());
        }

        var agg = new DefaultHealthAggregator();
        HealthCheckResult result = agg.Aggregate(contributors);
        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Up, result.Status);
        Assert.Contains("Up-9", result.Details.Keys);
    }

    [Fact]
    public void Aggregate_MultipleContributor_OrderDoesNotMatter_ReturnsExpectedHealth()
    {
        var contributors = new List<IHealthContributor>
        {
            new UpContributor(),
            new OutOfServiceContributor(),
            new UnknownContributor()
        };

        var agg = new DefaultHealthAggregator();
        HealthCheckResult result = agg.Aggregate(contributors);
        Assert.NotNull(result);
        Assert.Equal(HealthStatus.OutOfService, result.Status);
        Assert.NotNull(result.Details);
    }

    [Fact]
    public void AggregatesInParallel()
    {
        var t = new Stopwatch();

        var contributors = new List<IHealthContributor>
        {
            new UpContributor(500),
            new UpContributor(500),
            new UpContributor(500)
        };

        var agg = new DefaultHealthAggregator();
        t.Start();
        HealthCheckResult result = agg.Aggregate(contributors);
        t.Stop();
        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Up, result.Status);
        Assert.InRange(t.ElapsedMilliseconds, 450, 1200);
    }
}
