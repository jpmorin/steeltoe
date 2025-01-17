// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;

namespace Steeltoe.Integration.RabbitMQ.Support;

public class ReturnedRabbitMessageException : MessagingException
{
    public int ReplyCode { get; }
    public string ReplyText { get; }
    public string Exchange { get; }
    public string RoutingKey { get; }

    public ReturnedRabbitMessageException(IMessage failedMessage, int replyCode, string replyText, string exchange, string routingKey)
        : base(failedMessage)
    {
        ReplyCode = replyCode;
        ReplyText = replyText;
        Exchange = exchange;
        RoutingKey = routingKey;
    }

    public override string ToString()
    {
        return $"{base.ToString()}, replyCode={ReplyCode}, replyText={ReplyText}, exchange={Exchange}, routingKey={RoutingKey}]";
    }
}
