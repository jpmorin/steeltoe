// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.


namespace Steeltoe.Management.MetricCollectors.Metrics;
#pragma warning disable S1135 // Track uses of "TODO" tags
// TODO: Move to common 
public static class MetricLabelExtensions
#pragma warning restore S1135 // Track uses of "TODO" tags
{
    public static ReadOnlySpan<KeyValuePair<string, object>> AsReadonlySpan(this IDictionary<string, object> keyValuePairs)
    {
        return new ReadOnlySpan<KeyValuePair<string, object>>(keyValuePairs.ToArray());
    }

    public static ReadOnlySpan<KeyValuePair<string, object>> AsReadonlySpan(this IEnumerable<KeyValuePair<string, object>> keyValuePairs)
    {
        return new ReadOnlySpan<KeyValuePair<string, object>>(keyValuePairs.ToArray());
    }
}
