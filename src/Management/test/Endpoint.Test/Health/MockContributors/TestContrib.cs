// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Management.Endpoint.Test.Health.MockContributors;

internal sealed class TestContrib : IHealthContributor
{
    public bool Called { get; private set; }
    public bool Throws { get; }

    public string Id { get; }

    public TestContrib(string id)
    {
        Id = id;
        Throws = false;
    }

    public TestContrib(string id, bool throws)
    {
        Id = id;
        Throws = throws;
    }

    public HealthCheckResult Health()
    {
        if (Throws)
        {
            throw new Exception();
        }

        Called = true;

        return new HealthCheckResult
        {
            Status = HealthStatus.Up
        };
    }
}
