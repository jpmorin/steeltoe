// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Steeltoe.Management.Diagnostics;

public static class DiagnosticHelpers
{
    public static T GetProperty<T>(object o, string name)
    {
        PropertyInfo property = o.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);

        if (property == null)
        {
            return default;
        }

        return (T)property.GetValue(o);
    }
}
