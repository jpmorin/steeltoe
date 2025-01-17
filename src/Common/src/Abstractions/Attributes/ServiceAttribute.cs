// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public sealed class ServiceAttribute : Attribute
{
    public string Name { get; } = string.Empty;

    public ServiceAttribute()
    {
    }

    public ServiceAttribute(string name)
    {
        Name = name;
    }
}
