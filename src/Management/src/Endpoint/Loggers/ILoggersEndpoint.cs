// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Logging;

namespace Steeltoe.Management.Endpoint.Loggers;

public interface ILoggersEndpoint : IEndpoint<Dictionary<string, object>, LoggersChangeRequest>
{
    void AddLevels(Dictionary<string, object> result);

    void SetLogLevel(IDynamicLoggerProvider provider, string name, string level);

    ICollection<ILoggerConfiguration> GetLoggerConfigurations(IDynamicLoggerProvider provider);
}
