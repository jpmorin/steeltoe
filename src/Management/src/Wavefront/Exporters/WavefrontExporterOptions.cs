// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Common.Net;

namespace Steeltoe.Management.Wavefront.Exporters;

public class WavefrontExporterOptions : IWavefrontExporterOptions
{
    // Note: this key is shared between tracing and metrics to mirror the Spring boot configuration settings.
    public const string WavefrontPrefix = "management:metrics:export:wavefront";

    public string Uri { get; set; }

    public string ApiToken { get; set; }

    public int Step { get; set; } = 30_000; // milliseconds

    public int BatchSize { get; set; } = 10_000;

    public int MaxQueueSize { get; set; } = 500_000;

    public WavefrontApplicationOptions ApplicationOptions { get; }

    public string Source => ApplicationOptions?.Source ?? DnsTools.ResolveHostName();

    public string Name => ApplicationOptions?.Name ?? "SteeltoeApp";

    public string Service => ApplicationOptions?.Service ?? "SteeltoeAppService";

    public string Cluster { get; set; }

    public WavefrontExporterOptions(IConfiguration configuration)
    {
        ArgumentGuard.NotNull(configuration);

        IConfigurationSection section = configuration.GetSection(WavefrontPrefix);

        if (section == null)
        {
            throw new InvalidOperationException($"Failed to locate configuration section '{WavefrontPrefix}'.");
        }

        section.Bind(this);
        ApplicationOptions = new WavefrontApplicationOptions(configuration);
    }
}
