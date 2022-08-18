// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.CircuitBreaker.Hystrix.Strategy;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;
using Steeltoe.Common;

namespace Steeltoe.CircuitBreaker.Hystrix;

public static class HystrixServiceCollectionExtensions
{
    public static void AddHystrixCommand<TService, TImplementation>(this IServiceCollection services, IHystrixCommandGroupKey groupKey, IConfiguration config)
        where TService : class
        where TImplementation : class, TService
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(groupKey);
        ArgumentGuard.NotNull(config);

        HystrixOptionsStrategy strategy = HystrixPlugins.OptionsStrategy;
        IHystrixDynamicOptions dynOpts = strategy.GetDynamicOptions(config);

        IHystrixCommandKey commandKey = HystrixCommandKeyDefault.AsKey(typeof(TImplementation).Name);
        IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey(groupKey.Name);

        IHystrixCommandOptions opts = new HystrixCommandOptions(commandKey, null, dynOpts)
        {
            GroupKey = groupKey,
            ThreadPoolKey = threadPoolKey
        };

        opts.ThreadPoolOptions = new HystrixThreadPoolOptions(threadPoolKey, null, dynOpts);
        services.AddTransient<TService, TImplementation>(p => (TImplementation)ActivatorUtilities.CreateInstance(p, typeof(TImplementation), opts));
    }

    public static void AddHystrixCommand<TService, TImplementation>(this IServiceCollection services, IHystrixCommandGroupKey groupKey,
        IHystrixCommandKey commandKey, IConfiguration config)
        where TService : class
        where TImplementation : class, TService
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(groupKey);
        ArgumentGuard.NotNull(commandKey);
        ArgumentGuard.NotNull(config);

        HystrixOptionsStrategy strategy = HystrixPlugins.OptionsStrategy;
        IHystrixDynamicOptions dynOpts = strategy.GetDynamicOptions(config);

        IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey(groupKey.Name);

        IHystrixCommandOptions opts = new HystrixCommandOptions(commandKey, null, dynOpts)
        {
            GroupKey = groupKey,
            ThreadPoolKey = threadPoolKey
        };

        opts.ThreadPoolOptions = new HystrixThreadPoolOptions(threadPoolKey, null, dynOpts);
        services.AddTransient<TService, TImplementation>(p => (TImplementation)ActivatorUtilities.CreateInstance(p, typeof(TImplementation), opts));
    }

    public static void AddHystrixCommand<TService>(this IServiceCollection services, IHystrixCommandGroupKey groupKey, IConfiguration config)
        where TService : class
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(groupKey);
        ArgumentGuard.NotNull(config);

        HystrixOptionsStrategy strategy = HystrixPlugins.OptionsStrategy;
        IHystrixDynamicOptions dynOpts = strategy.GetDynamicOptions(config);

        IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey(groupKey.Name);
        IHystrixCommandKey commandKey = HystrixCommandKeyDefault.AsKey(typeof(TService).Name);

        IHystrixCommandOptions opts = new HystrixCommandOptions(commandKey, null, dynOpts)
        {
            GroupKey = groupKey,
            ThreadPoolKey = threadPoolKey
        };

        opts.ThreadPoolOptions = new HystrixThreadPoolOptions(threadPoolKey, null, dynOpts);
        services.AddTransient(p => (TService)ActivatorUtilities.CreateInstance(p, typeof(TService), opts));
    }

    public static void AddHystrixCommand<TService>(this IServiceCollection services, IHystrixCommandGroupKey groupKey, IHystrixCommandKey commandKey,
        IConfiguration config)
        where TService : class
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(groupKey);
        ArgumentGuard.NotNull(commandKey);
        ArgumentGuard.NotNull(config);

        HystrixOptionsStrategy strategy = HystrixPlugins.OptionsStrategy;
        IHystrixDynamicOptions dynOpts = strategy.GetDynamicOptions(config);

        IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey(groupKey.Name);

        IHystrixCommandOptions opts = new HystrixCommandOptions(commandKey, null, dynOpts)
        {
            GroupKey = groupKey,
            ThreadPoolKey = threadPoolKey
        };

        opts.ThreadPoolOptions = new HystrixThreadPoolOptions(threadPoolKey, null, dynOpts);
        services.AddTransient(p => (TService)ActivatorUtilities.CreateInstance(p, typeof(TService), opts));
    }

    public static void AddHystrixCommand<TService>(this IServiceCollection services, string groupKey, IConfiguration config)
        where TService : class
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNullOrEmpty(groupKey);
        ArgumentGuard.NotNull(config);

        AddHystrixCommand<TService>(services, HystrixCommandGroupKeyDefault.AsKey(groupKey), config);
    }

    public static void AddHystrixCommand<TService>(this IServiceCollection services, string groupKey, string commandKey, IConfiguration config)
        where TService : class
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNullOrEmpty(groupKey);
        ArgumentGuard.NotNullOrEmpty(commandKey);
        ArgumentGuard.NotNull(config);

        AddHystrixCommand<TService>(services, HystrixCommandGroupKeyDefault.AsKey(groupKey), HystrixCommandKeyDefault.AsKey(commandKey), config);
    }

    public static void AddHystrixCommand<TService, TImplementation>(this IServiceCollection services, string groupKey, IConfiguration config)
        where TService : class
        where TImplementation : class, TService
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNullOrEmpty(groupKey);
        ArgumentGuard.NotNull(config);

        AddHystrixCommand<TService, TImplementation>(services, HystrixCommandGroupKeyDefault.AsKey(groupKey), config);
    }

    public static void AddHystrixCommand<TService, TImplementation>(this IServiceCollection services, string groupKey, string commandKey, IConfiguration config)
        where TService : class
        where TImplementation : class, TService
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNullOrEmpty(groupKey);
        ArgumentGuard.NotNullOrEmpty(commandKey);
        ArgumentGuard.NotNull(config);

        AddHystrixCommand<TService, TImplementation>(services, HystrixCommandGroupKeyDefault.AsKey(groupKey), HystrixCommandKeyDefault.AsKey(commandKey),
            config);
    }

    public static void AddHystrixCollapser<TService, TImplementation>(this IServiceCollection services, IHystrixCollapserKey collapserKey,
        IConfiguration config)
        where TService : class
        where TImplementation : class, TService
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(collapserKey);
        ArgumentGuard.NotNull(config);

        HystrixOptionsStrategy strategy = HystrixPlugins.OptionsStrategy;
        IHystrixDynamicOptions dynOpts = strategy.GetDynamicOptions(config);

        var opts = new HystrixCollapserOptions(collapserKey, null, dynOpts);

        services.AddTransient<TService, TImplementation>(p => (TImplementation)ActivatorUtilities.CreateInstance(p, typeof(TImplementation), opts));
    }

    public static void AddHystrixCollapser<TService, TImplementation>(this IServiceCollection services, IHystrixCollapserKey collapserKey,
        RequestCollapserScope scope, IConfiguration config)
        where TService : class
        where TImplementation : class, TService
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(collapserKey);
        ArgumentGuard.NotNull(config);

        HystrixOptionsStrategy strategy = HystrixPlugins.OptionsStrategy;
        IHystrixDynamicOptions dynOpts = strategy.GetDynamicOptions(config);

        var opts = new HystrixCollapserOptions(collapserKey, scope, null, dynOpts);
        services.AddTransient<TService, TImplementation>(p => (TImplementation)ActivatorUtilities.CreateInstance(p, typeof(TImplementation), opts));
    }

    public static void AddHystrixCollapser<TService>(this IServiceCollection services, IHystrixCollapserKey collapserKey, IConfiguration config)
        where TService : class
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(collapserKey);
        ArgumentGuard.NotNull(config);

        HystrixOptionsStrategy strategy = HystrixPlugins.OptionsStrategy;
        IHystrixDynamicOptions dynOpts = strategy.GetDynamicOptions(config);

        var opts = new HystrixCollapserOptions(collapserKey, null, dynOpts);

        services.AddTransient(p => (TService)ActivatorUtilities.CreateInstance(p, typeof(TService), opts));
    }

    public static void AddHystrixCollapser<TService>(this IServiceCollection services, IHystrixCollapserKey collapserKey, RequestCollapserScope scope,
        IConfiguration config)
        where TService : class
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(collapserKey);
        ArgumentGuard.NotNull(config);

        HystrixOptionsStrategy strategy = HystrixPlugins.OptionsStrategy;
        IHystrixDynamicOptions dynOpts = strategy.GetDynamicOptions(config);

        var opts = new HystrixCollapserOptions(collapserKey, scope, null, dynOpts);

        services.AddTransient(p => (TService)ActivatorUtilities.CreateInstance(p, typeof(TService), opts));
    }

    public static void AddHystrixCollapser<TService>(this IServiceCollection services, string collapserKey, IConfiguration config)
        where TService : class
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNullOrEmpty(collapserKey);
        ArgumentGuard.NotNull(config);

        AddHystrixCollapser<TService>(services, HystrixCollapserKeyDefault.AsKey(collapserKey), config);
    }

    public static void AddHystrixCollapser<TService>(this IServiceCollection services, string collapserKey, RequestCollapserScope scope, IConfiguration config)
        where TService : class
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNullOrEmpty(collapserKey);
        ArgumentGuard.NotNull(config);

        AddHystrixCollapser<TService>(services, HystrixCollapserKeyDefault.AsKey(collapserKey), scope, config);
    }

    public static void AddHystrixCollapser<TService, TImplementation>(this IServiceCollection services, string collapserKey, IConfiguration config)
        where TService : class
        where TImplementation : class, TService
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNullOrEmpty(collapserKey);
        ArgumentGuard.NotNull(config);

        AddHystrixCollapser<TService, TImplementation>(services, HystrixCollapserKeyDefault.AsKey(collapserKey), config);
    }

    public static void AddHystrixCollapser<TService, TImplementation>(this IServiceCollection services, string collapserKey, RequestCollapserScope scope,
        IConfiguration config)
        where TService : class
        where TImplementation : class, TService
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNullOrEmpty(collapserKey);
        ArgumentGuard.NotNull(config);

        AddHystrixCollapser<TService, TImplementation>(services, HystrixCollapserKeyDefault.AsKey(collapserKey), scope, config);
    }
}