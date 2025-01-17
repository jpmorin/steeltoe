// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Steeltoe.Messaging.Handler.Invocation;

public class HandlerMethodReturnValueHandlerComposite : IAsyncHandlerMethodReturnValueHandler
{
    private readonly List<IHandlerMethodReturnValueHandler> _returnValueHandlers = new();

    public IList<IHandlerMethodReturnValueHandler> ReturnValueHandlers => new List<IHandlerMethodReturnValueHandler>(_returnValueHandlers);

    public void Clear()
    {
        _returnValueHandlers.Clear();
    }

    public HandlerMethodReturnValueHandlerComposite AddHandler(IHandlerMethodReturnValueHandler returnValueHandler)
    {
        _returnValueHandlers.Add(returnValueHandler);
        return this;
    }

    public HandlerMethodReturnValueHandlerComposite AddHandlers(IList<IHandlerMethodReturnValueHandler> handlers)
    {
        if (handlers != null)
        {
            _returnValueHandlers.AddRange(handlers);
        }

        return this;
    }

    public bool SupportsReturnType(ParameterInfo returnType)
    {
        return GetReturnValueHandler(returnType) != null;
    }

    public void HandleReturnValue(object returnValue, ParameterInfo returnType, IMessage message)
    {
        IHandlerMethodReturnValueHandler handler = GetReturnValueHandler(returnType);

        if (handler == null)
        {
            throw new InvalidOperationException($"No handler for return value type: {returnType.ParameterType}");
        }

        handler.HandleReturnValue(returnValue, returnType, message);
    }

    public bool IsAsyncReturnValue(object returnValue, ParameterInfo parameterInfo)
    {
        return GetReturnValueHandler(parameterInfo) is IAsyncHandlerMethodReturnValueHandler handler1 &&
            handler1.IsAsyncReturnValue(returnValue, parameterInfo);
    }

    private IHandlerMethodReturnValueHandler GetReturnValueHandler(ParameterInfo returnType)
    {
        foreach (IHandlerMethodReturnValueHandler handler in _returnValueHandlers)
        {
            if (handler.SupportsReturnType(returnType))
            {
                return handler;
            }
        }

        return null;
    }
}
