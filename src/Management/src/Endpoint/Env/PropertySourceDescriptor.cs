// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Env;

public class PropertySourceDescriptor
{
    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("properties")]
    public IDictionary<string, PropertyValueDescriptor> Properties { get; }

    public PropertySourceDescriptor(string name, IDictionary<string, PropertyValueDescriptor> properties)
    {
        Name = name;
        Properties = properties;
    }
}
