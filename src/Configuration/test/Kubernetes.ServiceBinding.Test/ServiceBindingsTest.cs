// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding.Test;

public sealed class ServiceBindingsTest
{
    [Fact]
    public void NullPath()
    {
        var b = new ServiceBindingConfigurationProvider.ServiceBindings(null);
        b.Bindings.Should().BeEmpty();
    }

    [Fact]
    public void PopulatesContent()
    {
        var path = new PhysicalFileProvider(GetK8SResourcesDirectory());
        var b = new ServiceBindingConfigurationProvider.ServiceBindings(path);
        b.Bindings.Count.Should().Be(3);
    }

    private static string GetK8SResourcesDirectory()
    {
        return Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "resources", "k8s");
    }
}
