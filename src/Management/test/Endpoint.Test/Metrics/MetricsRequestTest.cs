// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Metrics;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Metrics;

public class MetricsRequestTest : BaseTest
{
    [Fact]
    public void Constructor_SetsValues()
    {
        var tags = new List<KeyValuePair<string, string>>();
        var req = new MetricsRequest("foo.bar", tags);
        Assert.Equal("foo.bar", req.MetricName);
        Assert.Same(tags, req.Tags);
    }
}
