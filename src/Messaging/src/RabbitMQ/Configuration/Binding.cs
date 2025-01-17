// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.RabbitMQ.Configuration;

public class Binding : AbstractDeclarable, IBinding
{
    public string ServiceName { get; set; }

    public string Destination { get; set; }

    public string Exchange { get; set; }

    public string RoutingKey { get; set; }

    public DestinationType Type { get; set; }

    public bool IsDestinationQueue => Type == DestinationType.Queue;

    public string BindingName { get; set; }

    public Binding(string bindingName)
        : base(null)
    {
        BindingName = ServiceName = bindingName;
    }

    public Binding(string bindingName, string destination, DestinationType destinationType, string exchange, string routingKey,
        Dictionary<string, object> arguments)
        : base(arguments)
    {
        BindingName = ServiceName = bindingName;
        Destination = destination;
        Type = destinationType;
        Exchange = exchange;
        RoutingKey = routingKey;
    }

    internal static IBinding Create(string bindingName, string destination, DestinationType destinationType, string exchange, string routingKey,
        Dictionary<string, object> arguments)
    {
        if (destinationType == DestinationType.Exchange)
        {
            return new ExchangeBinding(bindingName, destination, exchange, routingKey, arguments);
        }

        return new QueueBinding(bindingName, destination, exchange, routingKey, arguments);
    }

    public override string ToString()
    {
        return $"Binding [bindingName={BindingName}, destination={Destination}, exchange={Exchange}, routingKey={RoutingKey}, arguments={Arguments}]";
    }

    public enum DestinationType
    {
        Queue,
        Exchange
    }
}
