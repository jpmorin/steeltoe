// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.Handler.Attributes;

/// <summary>
/// Attribute that indicates a method parameter should be bound to a template variable in a destination template string. Supported on message handling
/// methods such as those attributed with MessageMapping.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class DestinationVariableAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the destination template variable.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DestinationVariableAttribute" /> class.
    /// </summary>
    /// <param name="name">
    /// the name of the destination template variable.
    /// </param>
    public DestinationVariableAttribute(string name = null)
    {
        Name = name ?? string.Empty;
    }
}
