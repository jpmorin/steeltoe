// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Fallback;
using Polly.Retry;
using Steeltoe.Common.Retry;
using Steeltoe.Common.Util;

namespace Steeltoe.Common.RetryPolly;

public class PollyRetryTemplate : RetryTemplate
{
    private const string RecoveryCallbackKey = "PollyRetryTemplate.RecoveryCallback";
    private const string RetryContextKey = "PollyRetryTemplate.RetryContext";

    private const string Recovered = "context.recovered";
    private const string Closed = "context.closed";
    private const string RecoveredResult = "context.recovered.result";

    private readonly BinaryExceptionClassifier _retryableExceptions;
    private readonly int _maxAttempts;
    private readonly int _backOffInitialInterval;
    private readonly double _backOffMultiplier;
    private readonly ILogger _logger;

    public PollyRetryTemplate(int maxAttempts, int backOffInitialInterval, int backOffMaxInterval, double backOffMultiplier, ILogger logger = null)
        : this(new Dictionary<Type, bool>(), maxAttempts, true, backOffInitialInterval, backOffMaxInterval, backOffMultiplier, logger)
    {
    }

    public PollyRetryTemplate(Dictionary<Type, bool> retryableExceptions, int maxAttempts, bool defaultRetryable, int backOffInitialInterval,
        int backOffMaxInterval, double backOffMultiplier, ILogger logger = null)
    {
        _retryableExceptions = new BinaryExceptionClassifier(retryableExceptions, defaultRetryable);
        _maxAttempts = maxAttempts;
        _backOffInitialInterval = backOffInitialInterval;
        _backOffMultiplier = backOffMultiplier;
        _logger = logger;
    }

    public override T Execute<T>(Func<IRetryContext, T> retryCallback)
    {
        return Execute(retryCallback, (IRecoveryCallback<T>)null);
    }

    public override T Execute<T>(Func<IRetryContext, T> retryCallback, Func<IRetryContext, T> recoveryCallback)
    {
        var callback = new FuncRecoveryCallback<T>(recoveryCallback, _logger);
        return Execute(retryCallback, callback);
    }

    public override void Execute(Action<IRetryContext> retryCallback, Action<IRetryContext> recoveryCallback)
    {
        var callback = new ActionRecoveryCallback(recoveryCallback, _logger);
        Execute(retryCallback, callback);
    }

    public override T Execute<T>(Func<IRetryContext, T> retryCallback, IRecoveryCallback<T> recoveryCallback)
    {
        Policy<T> policy = BuildPolicy<T>();
        var retryContext = new RetryContext();

        var context = new Context
        {
            { RetryContextKey, retryContext }
        };

        RetrySynchronizationManager.Register(retryContext);

        if (recoveryCallback != null)
        {
            retryContext.SetAttribute(RecoveryCallbackKey, recoveryCallback);
        }

        CallListenerOpen(retryContext);

        T result = policy.Execute(_ =>
        {
            T callbackResult = retryCallback(retryContext);

            if (recoveryCallback != null)
            {
                bool? recovered = (bool?)retryContext.GetAttribute(Recovered);

                if (recovered != null && recovered.Value)
                {
                    callbackResult = (T)retryContext.GetAttribute(RecoveredResult);
                }
            }

            return callbackResult;
        }, context);

        CallListenerClose(retryContext, retryContext.LastException);
        RetrySynchronizationManager.Clear();
        return result;
    }

    public override void Execute(Action<IRetryContext> retryCallback)
    {
        Execute(retryCallback, (IRecoveryCallback)null);
    }

    public override void Execute(Action<IRetryContext> retryCallback, IRecoveryCallback recoveryCallback)
    {
        Policy<object> policy = BuildPolicy<object>();
        var retryContext = new RetryContext();

        var context = new Context
        {
            { RetryContextKey, retryContext }
        };

        RetrySynchronizationManager.Register(retryContext);

        if (recoveryCallback != null)
        {
            retryContext.SetAttribute(RecoveryCallbackKey, recoveryCallback);
        }

        if (!CallListenerOpen(retryContext))
        {
            throw new TerminatedRetryException("Retry terminated abnormally by interceptor before first attempt");
        }

        policy.Execute(_ =>
        {
            retryCallback(retryContext);
            return null;
        }, context);

        CallListenerClose(retryContext, retryContext.LastException);
        RetrySynchronizationManager.Clear();
    }

    private Policy<T> BuildPolicy<T>()
    {
        IEnumerable<TimeSpan> delay =
            Backoff.ExponentialBackoff(TimeSpan.FromMilliseconds(_backOffInitialInterval), _maxAttempts - 1, _backOffMultiplier, true);

        RetryPolicy<T> retryPolicy = Policy<T>.HandleInner<Exception>(e => _retryableExceptions.Classify(e)).WaitAndRetry(delay, OnRetry);

        FallbackPolicy<T> fallbackPolicy = Policy<T>.Handle<Exception>().Fallback((delegateResult, context, _) =>
        {
            RetryContext retryContext = GetRetryContext(context);
            retryContext.LastException = delegateResult.Exception;
            var result = default(T);

            if (retryContext.GetAttribute(RecoveryCallbackKey) is IRecoveryCallback callback)
            {
                result = (T)callback.Recover(retryContext);
                retryContext.SetAttribute(Recovered, true);
                retryContext.SetAttribute(RecoveredResult, result);
            }
            else if (delegateResult.Exception != null)
            {
                throw delegateResult.Exception;
            }

            return result;
        }, (ex, context) =>
        {
            _logger?.LogError(ex.Exception, $"Context: {context}");

            // throw ex.Exception; throwing here doesn't allow the fall back to work.
        });

        return fallbackPolicy.Wrap(retryPolicy);
    }

    private RetryContext GetRetryContext(Context context)
    {
        if (context.TryGetValue(RetryContextKey, out object obj))
        {
            return (RetryContext)obj;
        }

        var result = new RetryContext();
        RetrySynchronizationManager.Register(result);
        return result;
    }

    private void OnRetry<T>(DelegateResult<T> delegateResult, TimeSpan time, int retryCount, Context context)
    {
        RetryContext retryContext = GetRetryContext(context);
        Exception ex = delegateResult.Exception;

        retryContext.LastException = ex;
        retryContext.RetryCount = retryCount;

        if (ex != null)
        {
            CallListenerOnError(retryContext, ex);
        }
    }

    private bool CallListenerOpen(RetryContext context)
    {
        bool running = true;

        foreach (IRetryListener listener in listeners)
        {
            running &= listener.Open(context);
        }

        return running;
    }

    private void CallListenerClose(RetryContext context, Exception ex)
    {
        context.SetAttribute(Closed, true);

        foreach (IRetryListener listener in listeners)
        {
            listener.Close(context, ex);
        }
    }

    private void CallListenerOnError(RetryContext context, Exception ex)
    {
        foreach (IRetryListener listener in listeners)
        {
            listener.OnError(context, ex);
        }
    }

    private sealed class FuncRecoveryCallback<T> : IRecoveryCallback<T>
    {
        private readonly Func<IRetryContext, T> _func;
        private readonly ILogger _logger;

        public FuncRecoveryCallback(Func<IRetryContext, T> func, ILogger logger)
        {
            _func = func;
            _logger = logger;
        }

        public T Recover(IRetryContext context)
        {
            _logger?.LogTrace($"FuncRecovery Context: {context}");
            return _func(context);
        }

        object IRecoveryCallback.Recover(IRetryContext context)
        {
            _logger?.LogTrace($"FuncRecovery Context: {context}");
            return _func(context);
        }
    }

    private sealed class ActionRecoveryCallback : IRecoveryCallback
    {
        private readonly Action<IRetryContext> _action;
        private readonly ILogger _logger;

        public ActionRecoveryCallback(Action<IRetryContext> action, ILogger logger)
        {
            _action = action;
            _logger = logger;
        }

        public object Recover(IRetryContext context)
        {
            _logger?.LogTrace($"ActionRecovery Context: {context}");
            _action(context);
            return null;
        }
    }
}
