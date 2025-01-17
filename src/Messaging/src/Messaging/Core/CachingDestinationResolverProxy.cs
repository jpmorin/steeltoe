// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Steeltoe.Common;

namespace Steeltoe.Messaging.Core;

public class CachingDestinationResolverProxy<TDestination> : IDestinationResolver<TDestination>
    where TDestination : class
{
    private readonly ConcurrentDictionary<string, TDestination> _resolvedDestinationCache = new();

    private readonly IDestinationResolver<TDestination> _targetDestinationResolver;

    public CachingDestinationResolverProxy(IDestinationResolver<TDestination> targetDestinationResolver)
    {
        ArgumentGuard.NotNull(targetDestinationResolver);

        _targetDestinationResolver = targetDestinationResolver;
    }

    public TDestination ResolveDestination(string name)
    {
        _resolvedDestinationCache.TryGetValue(name, out TDestination destination);

        if (destination == null)
        {
            destination = _targetDestinationResolver.ResolveDestination(name);
            _resolvedDestinationCache.TryAdd(name, destination);
        }

        return destination;
    }

    object IDestinationResolver.ResolveDestination(string name)
    {
        TDestination result = ResolveDestination(name);
        return result;
    }
}
