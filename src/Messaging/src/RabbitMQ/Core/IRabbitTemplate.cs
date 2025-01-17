// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using RabbitMQ.Client.Events;
using Steeltoe.Common.Services;
using Steeltoe.Messaging.RabbitMQ.Connection;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Core;

public interface IRabbitTemplate : IServiceNameAware
{
    IConnectionFactory ConnectionFactory { get; }

    T Execute<T>(Func<RC.IModel, T> channelCallback);

    T Invoke<T>(Func<IRabbitTemplate, T> operationsCallback)
    {
        return Invoke(operationsCallback, null, null);
    }

    T Invoke<T>(Func<IRabbitTemplate, T> operationsCallback, Action<object, BasicAckEventArgs> acks, Action<object, BasicNackEventArgs> nacks);

    bool WaitForConfirms(int timeoutInMilliseconds);

    void WaitForConfirmsOrDie(int timeoutInMilliseconds);

    void Send(string exchange, string routingKey, IMessage message);

    void Send(string exchange, string routingKey, IMessage message, CorrelationData correlationData);

    Task SendAsync(string exchange, string routingKey, IMessage message, CancellationToken cancellationToken = default);

    Task SendAsync(string exchange, string routingKey, IMessage message, CorrelationData correlationData, CancellationToken cancellationToken = default);

    void ConvertAndSend(string exchange, string routingKey, object message, CorrelationData correlationData);

    void ConvertAndSend(object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData);

    void ConvertAndSend(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData);

    void ConvertAndSend(string exchange, string routingKey, object message);

    void ConvertAndSend(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor);

    T ConvertSendAndReceive<T>(object message, CorrelationData correlationData);

    T ConvertSendAndReceive<T>(string exchange, string routingKey, object message, CorrelationData correlationData);

    T ConvertSendAndReceive<T>(object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData);

    T ConvertSendAndReceive<T>(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData);

    T ConvertSendAndReceive<T>(string exchange, string routingKey, object message);

    IMessage Receive(int timeoutInMilliseconds);

    IMessage Receive(string queueName, int timeoutInMilliseconds);

    T ReceiveAndConvert<T>(int timeoutMillis);

    T ReceiveAndConvert<T>(string queueName, int timeoutInMilliseconds);

    bool ReceiveAndReply<TReceive, TReply>(Func<TReceive, TReply> callback);

    bool ReceiveAndReply<TReceive, TReply>(string queueName, Func<TReceive, TReply> callback);

    bool ReceiveAndReply<TReceive, TReply>(Func<TReceive, TReply> callback, string replyExchange, string replyRoutingKey);

    bool ReceiveAndReply<TReceive, TReply>(string queueName, Func<TReceive, TReply> callback, string replyExchange, string replyRoutingKey);

    bool ReceiveAndReply<TReceive, TReply>(Func<TReceive, TReply> callback, Func<IMessage, TReply, Address> replyToAddressCallback);

    bool ReceiveAndReply<TReceive, TReply>(string queueName, Func<TReceive, TReply> callback, Func<IMessage, TReply, Address> replyToAddressCallback);

    IMessage SendAndReceive(string exchange, string routingKey, IMessage message);

    public interface IOperationsCallback<out T>
    {
        T DoInRabbit(IRabbitTemplate operations);
    }
}
