// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Env;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Env;

public class PropertySourceDescriptorTest : BaseTest
{
    [Fact]
    public void Constructor_SetsValues()
    {
        var properties = new Dictionary<string, PropertyValueDescriptor>
        {
            { "key1", new PropertyValueDescriptor("value") },
            { "key2", new PropertyValueDescriptor(false) }
        };

        var propDesc = new PropertySourceDescriptor("name", properties);
        Assert.Equal("name", propDesc.Name);
        Assert.Same(properties, propDesc.Properties);
    }

    [Fact]
    public void JsonSerialization_ReturnsExpected()
    {
        var properties = new Dictionary<string, PropertyValueDescriptor>
        {
            { "key1", new PropertyValueDescriptor("value") },
            { "key2", new PropertyValueDescriptor(false) }
        };

        var propDesc = new PropertySourceDescriptor("name", properties);
        string result = Serialize(propDesc);
        Assert.Equal("{\"name\":\"name\",\"properties\":{\"key1\":{\"value\":\"value\"},\"key2\":{\"value\":false}}}", result);
    }
}
