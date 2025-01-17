// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Filter = System.Func<string, Microsoft.Extensions.Logging.LogLevel, bool>;

namespace Steeltoe.Logging;

public class MessageProcessingLogger : ILogger
{
    private protected readonly IEnumerable<IDynamicMessageProcessor> MessageProcessors;

    public ILogger Delegate { get; }

    public Filter Filter { get; internal set; }

    public string Name { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageProcessingLogger" /> class. Wraps an ILogger and decorates log messages via
    /// <see cref="IDynamicMessageProcessor" />.
    /// </summary>
    /// <param name="logger">
    /// The <see cref="ILogger" /> being wrapped.
    /// </param>
    /// <param name="messageProcessors">
    /// The list of <see cref="IDynamicMessageProcessor" />s.
    /// </param>
    public MessageProcessingLogger(ILogger logger, IEnumerable<IDynamicMessageProcessor> messageProcessors = null)
    {
        MessageProcessors = messageProcessors;
        Delegate = logger;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return Delegate.BeginScope(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return Filter.Invoke(Name, logLevel);
    }

    public virtual void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        ArgumentGuard.NotNull(formatter);

        if (!IsEnabled(logLevel))
        {
            return;
        }

        Func<TState, Exception, string> compositeFormatter = (innerState, innerException) =>
            ApplyMessageProcessors(innerState, innerException, formatter, MessageProcessors);

        Delegate.Log(logLevel, eventId, state, exception, compositeFormatter);
    }

    private static string ApplyMessageProcessors<TState>(TState state, Exception exception, Func<TState, Exception, string> formatter,
        IEnumerable<IDynamicMessageProcessor> processors)
    {
        string message = formatter(state, exception);

        if (processors != null)
        {
            foreach (IDynamicMessageProcessor processor in processors)
            {
                message = processor.Process(message);
            }
        }

        return message;
    }
}
