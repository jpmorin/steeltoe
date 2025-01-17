// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using A.B.C.D;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;
using Steeltoe.Common.TestResources;
using Xunit;

namespace Steeltoe.Logging.DynamicSerilog.Test;

public class SerilogDynamicLoggerProviderTest
{
    public SerilogDynamicLoggerProviderTest()
    {
        SerilogDynamicProvider.ClearLogger();
    }

    [Fact]
    public void Create_CreatesCorrectLogger()
    {
        var provider = new SerilogDynamicProvider(GetConfiguration());
        var fac = new LoggerFactory();
        fac.AddProvider(provider);

        ILogger logger = fac.CreateLogger(typeof(TestClass));
        Assert.NotNull(logger);
        Assert.True(logger.IsEnabled(LogLevel.Information));
        Assert.False(logger.IsEnabled(LogLevel.Debug));
    }

    [Fact]
    public void SetLogLevel_UpdatesLogger()
    {
        var provider = new SerilogDynamicProvider(GetConfiguration());
        var fac = new LoggerFactory();
        fac.AddProvider(provider);

        ILogger logger = fac.CreateLogger(typeof(TestClass));
        Assert.NotNull(logger);
        Assert.True(logger.IsEnabled(LogLevel.Critical));
        Assert.True(logger.IsEnabled(LogLevel.Error));
        Assert.True(logger.IsEnabled(LogLevel.Warning));
        Assert.True(logger.IsEnabled(LogLevel.Information));
        Assert.False(logger.IsEnabled(LogLevel.Debug));

        provider.SetLogLevel("A", LogLevel.Debug);
        Assert.True(logger.IsEnabled(LogLevel.Information));
        Assert.True(logger.IsEnabled(LogLevel.Debug));

        provider.SetLogLevel("A", LogLevel.Information);
        Assert.True(logger.IsEnabled(LogLevel.Information));
        Assert.False(logger.IsEnabled(LogLevel.Debug));
    }

    [Fact]
    public void SetLogLevel_UpdatesNamespaceDescendants()
    {
        // arrange (A* should log at Information)
        var provider = new SerilogDynamicProvider(GetConfiguration());

        // act I: with original setup
        ILogger childLogger = provider.CreateLogger("A.B.C");
        ICollection<ILoggerConfiguration> configurations = provider.GetLoggerConfigurations();
        ILoggerConfiguration tierOneNamespace = configurations.First(n => n.Name == "A");

        // assert I: base namespace is in the response, correctly
        Assert.Equal(LogLevel.Information, tierOneNamespace.EffectiveLevel);

        // act II: set A.B* to log at Trace
        provider.SetLogLevel("A.B", LogLevel.Trace);
        configurations = provider.GetLoggerConfigurations();

        tierOneNamespace = configurations.First(n => n.Name == "A");
        ILoggerConfiguration tierTwoNamespace = configurations.First(n => n.Name == "A.B");

        // assert II:  base hasn't changed but the one set at runtime and all descendants (including a concrete logger) have
        Assert.Equal(LogLevel.Information, tierOneNamespace.EffectiveLevel);
        Assert.Equal(LogLevel.Trace, tierTwoNamespace.EffectiveLevel);
        Assert.True(childLogger.IsEnabled(LogLevel.Trace));

        // act III: set A to something else, make sure it inherits down
        provider.SetLogLevel("A", LogLevel.Error);
        configurations = provider.GetLoggerConfigurations();
        tierOneNamespace = configurations.First(n => n.Name == "A");
        tierTwoNamespace = configurations.First(n => n.Name == "A.B");
        ILogger grandchildLogger = provider.CreateLogger("A.B.C.D");

        // assert again
        Assert.Equal(LogLevel.Error, tierOneNamespace.EffectiveLevel);
        Assert.Equal(LogLevel.Error, tierTwoNamespace.EffectiveLevel);
        Assert.False(childLogger.IsEnabled(LogLevel.Warning));
        Assert.False(grandchildLogger.IsEnabled(LogLevel.Warning));
    }

    [Fact]
    public void SetLogLevel_Can_Reset_to_Default()
    {
        // arrange (A* should log at Information)
        var provider = new SerilogDynamicProvider(GetConfiguration());

        // act I: with original setup
        ILogger firstLogger = provider.CreateLogger("A.B.C");
        ICollection<ILoggerConfiguration> configurations = provider.GetLoggerConfigurations();
        ILoggerConfiguration tierOneNamespace = configurations.First(n => n.Name == "A");

        // assert I: base namespace is in the response, correctly
        Assert.Equal(LogLevel.Information, tierOneNamespace.EffectiveLevel);

        // act II: set A.B* to log at Trace
        provider.SetLogLevel("A.B", LogLevel.Trace);
        configurations = provider.GetLoggerConfigurations();
        tierOneNamespace = configurations.First(n => n.Name == "A");
        ILoggerConfiguration tierTwoNamespace = configurations.First(n => n.Name == "A.B");

        // assert II: base hasn't changed but the one set at runtime and all descendants (including a concrete logger) have
        Assert.Equal(LogLevel.Information, tierOneNamespace.EffectiveLevel);
        Assert.Equal(LogLevel.Trace, tierTwoNamespace.EffectiveLevel);
        Assert.True(firstLogger.IsEnabled(LogLevel.Trace));

        // act III: reset A.B
        provider.SetLogLevel("A.B", null);
        configurations = provider.GetLoggerConfigurations();
        tierOneNamespace = configurations.First(n => n.Name == "A");
        tierTwoNamespace = configurations.First(n => n.Name == "A.B");
        ILogger secondLogger = provider.CreateLogger("A.B.C.D");

        // assert again
        Assert.Equal(LogLevel.Information, tierOneNamespace.EffectiveLevel);
        Assert.Equal(LogLevel.Information, tierTwoNamespace.EffectiveLevel);
        Assert.True(firstLogger.IsEnabled(LogLevel.Information));
        Assert.True(secondLogger.IsEnabled(LogLevel.Information));
    }

    [Fact]
    public void GetLoggerConfigurations_ReturnsExpected()
    {
        var provider = new SerilogDynamicProvider(GetConfiguration());
        var fac = new LoggerFactory();
        fac.AddProvider(provider);

        fac.CreateLogger(typeof(TestClass));

        ICollection<ILoggerConfiguration> logConfig = provider.GetLoggerConfigurations();
        Assert.Equal(6, logConfig.Count);
        Assert.Contains(new DynamicLoggerConfiguration("Default", LogLevel.Information, LogLevel.Information), logConfig);
        Assert.Contains(new DynamicLoggerConfiguration("A.B.C.D.TestClass", null, LogLevel.Information), logConfig);
        Assert.Contains(new DynamicLoggerConfiguration("A.B.C.D", null, LogLevel.Information), logConfig);
        Assert.Contains(new DynamicLoggerConfiguration("A.B.C", LogLevel.Information, LogLevel.Information), logConfig);
        Assert.Contains(new DynamicLoggerConfiguration("A.B", null, LogLevel.Information), logConfig);
        Assert.Contains(new DynamicLoggerConfiguration("A", LogLevel.Information, LogLevel.Information), logConfig);
    }

    [Fact]
    public void GetLoggerConfigurations_ReturnsExpected_After_SetLogLevel()
    {
        var provider = new SerilogDynamicProvider(GetConfiguration());
        var fac = new LoggerFactory();
        fac.AddProvider(provider);

        fac.CreateLogger(typeof(TestClass));
        ICollection<ILoggerConfiguration> logConfig = provider.GetLoggerConfigurations();

        Assert.Equal(6, logConfig.Count);
        Assert.Contains(new DynamicLoggerConfiguration("Default", LogLevel.Information, LogLevel.Information), logConfig);
        Assert.Contains(new DynamicLoggerConfiguration("A.B.C.D.TestClass", null, LogLevel.Information), logConfig);
        Assert.Contains(new DynamicLoggerConfiguration("A.B.C.D", null, LogLevel.Information), logConfig);
        Assert.Contains(new DynamicLoggerConfiguration("A.B.C", LogLevel.Information, LogLevel.Information), logConfig);
        Assert.Contains(new DynamicLoggerConfiguration("A.B", null, LogLevel.Information), logConfig);
        Assert.Contains(new DynamicLoggerConfiguration("A", LogLevel.Information, LogLevel.Information), logConfig);

        provider.SetLogLevel("A.B", LogLevel.Trace);
        logConfig = provider.GetLoggerConfigurations();

        Assert.Equal(6, logConfig.Count);
        Assert.Contains(new DynamicLoggerConfiguration("Default", LogLevel.Information, LogLevel.Information), logConfig);
        Assert.Contains(new DynamicLoggerConfiguration("A.B.C.D.TestClass", null, LogLevel.Trace), logConfig);
        Assert.Contains(new DynamicLoggerConfiguration("A.B.C.D", null, LogLevel.Trace), logConfig);
        Assert.Contains(new DynamicLoggerConfiguration("A.B.C", LogLevel.Information, LogLevel.Trace), logConfig);
        Assert.Contains(new DynamicLoggerConfiguration("A.B", null, LogLevel.Trace), logConfig);
        Assert.Contains(new DynamicLoggerConfiguration("A", LogLevel.Information, LogLevel.Information), logConfig);
    }

    [Fact]
    public void SetLogLevel_Works_OnDefault()
    {
        var provider = new SerilogDynamicProvider(GetConfiguration());
        var fac = new LoggerFactory();
        fac.AddProvider(provider);
        ICollection<ILoggerConfiguration> originalLogConfig = provider.GetLoggerConfigurations();

        provider.SetLogLevel("Default", LogLevel.Trace);
        ICollection<ILoggerConfiguration> updatedLogConfig = provider.GetLoggerConfigurations();

        Assert.Contains(new DynamicLoggerConfiguration("Default", LogLevel.Information, LogLevel.Information), originalLogConfig);
        Assert.Contains(new DynamicLoggerConfiguration("Default", LogLevel.Information, LogLevel.Trace), updatedLogConfig);
    }

    [Fact]
    public void ResetLogLevel_Works_OnDefault()
    {
        var provider = new SerilogDynamicProvider(GetConfiguration());
        var fac = new LoggerFactory();
        fac.AddProvider(provider);
        ICollection<ILoggerConfiguration> originalLogConfig = provider.GetLoggerConfigurations();

        provider.SetLogLevel("Default", LogLevel.Debug);
        ICollection<ILoggerConfiguration> updatedLogConfig = provider.GetLoggerConfigurations();
        provider.SetLogLevel("Default", null);
        ICollection<ILoggerConfiguration> resetConfig = provider.GetLoggerConfigurations();

        Assert.Contains(new DynamicLoggerConfiguration("Default", LogLevel.Information, LogLevel.Information), originalLogConfig);
        Assert.Contains(new DynamicLoggerConfiguration("Default", LogLevel.Information, LogLevel.Debug), updatedLogConfig);
        Assert.Contains(new DynamicLoggerConfiguration("Default", LogLevel.Information, LogLevel.Information), resetConfig);
    }

    [Fact]
    public void LoggerLogsWithEnrichers()
    {
        var provider = new SerilogDynamicProvider(GetConfigurationFromFile());

        var fac = new LoggerFactory();
        fac.AddProvider(provider);
        ILogger logger = fac.CreateLogger(typeof(TestClass));

        // act I - log at all levels, expect Info and above to work
        using var unConsole = new ConsoleOutputBorrower();

        using (LogContext.PushProperty("A", 1))
        {
            logger.LogInformation("Carries property A = 1");

            using (LogContext.PushProperty("A", 2))
            {
                using (LogContext.PushProperty("B", 1))
                {
                    logger.LogInformation("Carries A = 2 and B = 1");
                }
            }

            logger.LogInformation("Carries property A = 1, again");
        }

        string logged = unConsole.ToString();

        // assert I
        Assert.Contains(@"A.B.C.D.TestClass: {A=1, Application=""Sample""}", logged, StringComparison.Ordinal);
        Assert.Contains(@"Carries property A = 1", logged, StringComparison.Ordinal);
        Assert.Contains(@"A.B.C.D.TestClass: {B=1, A=2, Application=""Sample""}", logged, StringComparison.Ordinal);
        Assert.Contains(@"Carries A = 2 and B = 1", logged, StringComparison.Ordinal);
        Assert.Contains(@"A.B.C.D.TestClass: {A=1, Application=""Sample""}", logged, StringComparison.Ordinal);
        Assert.Contains(@"Carries property A = 1, again", logged, StringComparison.Ordinal);
        Assert.Matches(new Regex(@"ThreadId:<\d+>"), logged);
    }

    [Fact]
    public void LoggerLogsWithDestructuring()
    {
        var provider = new SerilogDynamicProvider(GetConfigurationFromFile());
        var fac = new LoggerFactory();
        fac.AddProvider(provider);
        ILogger logger = fac.CreateLogger(typeof(TestClass));

        // act I - log at all levels, expect Info and above to work
        using var unConsole = new ConsoleOutputBorrower();

        logger.LogInformation("Info {@TestInfo}", new
        {
            Info1 = "information1",
            Info2 = "information2"
        });

        string logged = unConsole.ToString();

        Assert.Contains("Info {\"Info1\": \"information1\", \"Info2\": \"information2\"}", logged, StringComparison.Ordinal);
    }

    [Fact]
    public void LoggerLogs_At_Configured_Setting()
    {
        var provider = new SerilogDynamicProvider(GetConfiguration());
        var fac = new LoggerFactory();
        fac.AddProvider(provider);
        ILogger logger = fac.CreateLogger(typeof(TestClass));

        // act I - log at all levels, expect Info and above to work
        using (var unConsole = new ConsoleOutputBorrower())
        {
            WriteLogEntries(logger);

            string logged = unConsole.ToString();

            // assert I
            Assert.Contains("Critical message", logged, StringComparison.Ordinal);
            Assert.Contains("Error message", logged, StringComparison.Ordinal);
            Assert.Contains("Warning message", logged, StringComparison.Ordinal);
            Assert.Contains("Informational message", logged, StringComparison.Ordinal);
            Assert.DoesNotContain("Debug message", logged, StringComparison.Ordinal);
            Assert.DoesNotContain("Trace message", logged, StringComparison.Ordinal);
        }

        // act II - adjust rules, expect Error and above to work
        provider.SetLogLevel("A.B.C.D", LogLevel.Error);

        using (var unConsole = new ConsoleOutputBorrower())
        {
            WriteLogEntries(logger);

            string logged2 = unConsole.ToString();

            // assert II
            Assert.Contains("Critical message", logged2, StringComparison.Ordinal);
            Assert.Contains("Error message", logged2, StringComparison.Ordinal);
            Assert.DoesNotContain("Warning message", logged2, StringComparison.Ordinal);
            Assert.DoesNotContain("Informational message", logged2, StringComparison.Ordinal);
            Assert.DoesNotContain("Debug message", logged2, StringComparison.Ordinal);
            Assert.DoesNotContain("Trace message", logged2, StringComparison.Ordinal);
        }

        // act III - adjust rules, expect Trace and above to work
        provider.SetLogLevel("A", LogLevel.Trace);

        using (var unConsole = new ConsoleOutputBorrower())
        {
            WriteLogEntries(logger);

            string logged3 = unConsole.ToString();

            // assert III
            Assert.Contains("Critical message", logged3, StringComparison.Ordinal);
            Assert.Contains("Error message", logged3, StringComparison.Ordinal);
            Assert.Contains("Warning message", logged3, StringComparison.Ordinal);
            Assert.Contains("Informational message", logged3, StringComparison.Ordinal);
            Assert.Contains("Debug message", logged3, StringComparison.Ordinal);
            Assert.Contains("Trace message", logged3, StringComparison.Ordinal);
        }

        // act IV - adjust rules, expect nothing to work
        provider.SetLogLevel("A", LogLevel.None);

        using (var unConsole = new ConsoleOutputBorrower())
        {
            WriteLogEntries(logger);

            string logged4 = unConsole.ToString();

            // assert IV
            Assert.DoesNotContain("Critical message", logged4, StringComparison.Ordinal);
            Assert.DoesNotContain("Error message", logged4, StringComparison.Ordinal);
            Assert.DoesNotContain("Warning message", logged4, StringComparison.Ordinal);
            Assert.DoesNotContain("Informational message", logged4, StringComparison.Ordinal);
            Assert.DoesNotContain("Debug message", logged4, StringComparison.Ordinal);
            Assert.DoesNotContain("Trace message", logged4, StringComparison.Ordinal);
        }

        // act V - reset the rules, expect Info and above to work
        provider.SetLogLevel("A", null);

        using (var unConsole = new ConsoleOutputBorrower())
        {
            WriteLogEntries(logger);

            string logged5 = unConsole.ToString();

            // assert V
            Assert.NotNull(provider.GetLoggerConfigurations().First(c => c.Name == "A"));
            Assert.Contains("Critical message", logged5, StringComparison.Ordinal);
            Assert.Contains("Error message", logged5, StringComparison.Ordinal);
            Assert.Contains("Warning message", logged5, StringComparison.Ordinal);
            Assert.Contains("Informational message", logged5, StringComparison.Ordinal);
            Assert.DoesNotContain("Debug message", logged5, StringComparison.Ordinal);
            Assert.DoesNotContain("Trace message", logged5, StringComparison.Ordinal);
        }
    }

    private void WriteLogEntries(ILogger logger)
    {
        logger.LogCritical("Critical message");
        logger.LogError("Error message");
        logger.LogWarning("Warning message");
        logger.LogInformation("Informational message");
        logger.LogDebug("Debug message");
        logger.LogTrace("Trace message");
    }

    private IOptionsMonitor<SerilogOptions> GetConfiguration()
    {
        var appSettings = new Dictionary<string, string>
        {
            { "Serilog:MinimumLevel:Default", "Information" },
            { "Serilog:MinimumLevel:Override:Microsoft", "Warning" },
            { "Serilog:MinimumLevel:Override:Steeltoe.Extensions", "Verbose" },
            { "Serilog:MinimumLevel:Override:Steeltoe", "Information" },
            { "Serilog:MinimumLevel:Override:A", "Information" },
            { "Serilog:MinimumLevel:Override:A.B.C", "Information" },
            { "Serilog:WriteTo:Name", "Console" }
        };

        IConfigurationBuilder builder = new ConfigurationBuilder().AddInMemoryCollection(appSettings);
        IConfigurationRoot configuration = builder.Build();

        var serilogOptions = new SerilogOptions();
        serilogOptions.SetSerilogOptions(configuration);
        return new TestOptionsMonitor<SerilogOptions>(serilogOptions);
    }

    private IOptionsMonitor<SerilogOptions> GetConfigurationFromFile()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("serilogSettings.json");
        IConfigurationRoot configuration = builder.Build();
        var serilogOptions = new SerilogOptions();
        serilogOptions.SetSerilogOptions(configuration);
        return new TestOptionsMonitor<SerilogOptions>(serilogOptions);
    }
}
