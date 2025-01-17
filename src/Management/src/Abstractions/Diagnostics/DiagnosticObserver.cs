// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;

namespace Steeltoe.Management.Diagnostics;

public abstract class DiagnosticObserver : IDiagnosticObserver
{
    protected ILogger Logger { get; }

    protected IDisposable Subscription { get; set; }

    public string ListenerName { get; }

    public string ObserverName { get; }

    protected DiagnosticObserver(string name, string listenerName, ILogger logger = null)
    {
        ArgumentGuard.NotNullOrEmpty(name);
        ArgumentGuard.NotNullOrEmpty(listenerName);

        ObserverName = name;
        ListenerName = listenerName;
        Logger = logger;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Subscription?.Dispose();
            Subscription = null;

            Logger?.LogInformation("DiagnosticObserver {observer} Disposed", ObserverName);
        }
    }

    public void Subscribe(DiagnosticListener listener)
    {
        if (ListenerName == listener.Name)
        {
            if (Subscription != null)
            {
                Dispose();
            }

            Subscription = listener.Subscribe(this);
            Logger?.LogInformation("DiagnosticObserver {observer} Subscribed to {listener}", ObserverName, listener.Name);
        }
    }

    public virtual void OnCompleted()
    {
    }

    public virtual void OnError(Exception error)
    {
    }

    public virtual void OnNext(KeyValuePair<string, object> @event)
    {
        try
        {
            ProcessEvent(@event.Key, @event.Value);
        }
        catch (Exception e)
        {
            Logger?.LogError(e, "ProcessEvent exception: {Id}", @event.Key);
        }
    }

    public abstract void ProcessEvent(string eventName, object value);
}
