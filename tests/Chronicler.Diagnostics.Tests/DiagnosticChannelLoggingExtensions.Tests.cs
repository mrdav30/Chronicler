using System;
using System.Collections.Generic;
using Xunit;

namespace Chronicler.Diagnostics.Tests;

public class DiagnosticChannelLoggingExtensionsTests
{
    [Fact]
    public void StringHelpers_EmitEveryLevelAndPreserveCallerInformation()
    {
        var entries = new List<DiagnosticEvent>();
        var channel = new DiagnosticChannel("Consumer")
        {
            MinimumLevel = DiagnosticLevel.Info,
            Sink = (in DiagnosticEvent entry) => entries.Add(entry)
        };

        channel.Info("info");
        channel.Warn("warn");
        channel.Error("error");
        channel.Log(DiagnosticLevel.Warning, "log", source: "ExplicitSource");

        const string source = "DiagnosticChannelLoggingExtensions.Tests." + nameof(StringHelpers_EmitEveryLevelAndPreserveCallerInformation);
        Assert.Collection(entries,
            entry => AssertEntry(entry, DiagnosticLevel.Info, "info", source),
            entry => AssertEntry(entry, DiagnosticLevel.Warning, "warn", source),
            entry => AssertEntry(entry, DiagnosticLevel.Error, "error", source),
            entry => AssertEntry(entry, DiagnosticLevel.Warning, "log", "ExplicitSource"));
    }

    [Fact]
    public void StringHelpers_FormatBeforeDisabledCallsButCanGuardFormatting()
    {
        var entries = new List<DiagnosticEvent>();
        var channel = new DiagnosticChannel("Consumer")
        {
            Sink = (in DiagnosticEvent entry) => entries.Add(entry)
        };
        int evaluations = 0;

        channel.Info(Format("info"));
        channel.Warn(Format("warn"));
        channel.Error(Format("error"));
        channel.Log(DiagnosticLevel.Warning, Format("log"));
        Assert.Equal(4, evaluations);
        Assert.Empty(entries);

        if (channel.IsEnabled(DiagnosticLevel.Warning))
            channel.Warn(Format("warn"));
        Assert.Equal(4, evaluations);

        channel.MinimumLevel = DiagnosticLevel.Warning;
        if (channel.IsEnabled(DiagnosticLevel.Warning))
            channel.Warn(Format("warn"));
        Assert.Equal(5, evaluations);
        AssertEntry(Assert.Single(entries), DiagnosticLevel.Warning, "warn 5",
            "DiagnosticChannelLoggingExtensions.Tests." + nameof(StringHelpers_FormatBeforeDisabledCallsButCanGuardFormatting));

        string Format(string prefix) => $"{prefix} {++evaluations}";
    }

    [Fact]
    public void InterpolatedHelpers_PreferLazyHandlersForEveryLevel()
    {
        var channel = new DiagnosticChannel("Disabled");
        int evaluations = 0;

        channel.Info($"info {Evaluate()}");
        channel.Warn($"warn {Evaluate()}");
        channel.Error($"error {Evaluate()}");
        channel.Log(DiagnosticLevel.Warning, $"log {Evaluate()}");

        Assert.Equal(0, evaluations);

        int Evaluate() => ++evaluations;
    }

    [Fact]
    public void InterpolatedHelpers_EmitEveryLevelWithCallerInformation()
    {
        var entries = new List<DiagnosticEvent>();
        var channel = new DiagnosticChannel("Consumer")
        {
            MinimumLevel = DiagnosticLevel.Info,
            Sink = (in DiagnosticEvent entry) => entries.Add(entry)
        };

        channel.Info($"info {1}");
        channel.Warn($"warn {2}");
        channel.Error($"error {3}");
        channel.Log(DiagnosticLevel.Warning, $"log {4}");

        const string source = "DiagnosticChannelLoggingExtensions.Tests." + nameof(InterpolatedHelpers_EmitEveryLevelWithCallerInformation);
        Assert.Collection(entries,
            entry => AssertEntry(entry, DiagnosticLevel.Info, "info 1", source),
            entry => AssertEntry(entry, DiagnosticLevel.Warning, "warn 2", source),
            entry => AssertEntry(entry, DiagnosticLevel.Error, "error 3", source),
            entry => AssertEntry(entry, DiagnosticLevel.Warning, "log 4", source));
    }

    [Fact]
    public void StringHelpers_WithNullChannel_PreserveArgumentValidation()
    {
        DiagnosticChannel channel = null;

        Assert.Equal("channel", Assert.Throws<ArgumentNullException>(() => channel.Info("info")).ParamName);
        Assert.Equal("channel", Assert.Throws<ArgumentNullException>(() => channel.Warn("warn")).ParamName);
        Assert.Equal("channel", Assert.Throws<ArgumentNullException>(() => channel.Error("error")).ParamName);
        Assert.Equal("channel", Assert.Throws<ArgumentNullException>(() => channel.Log(DiagnosticLevel.Warning, "log")).ParamName);
    }

    [Fact]
    public void Warn_WhenDisabled_SkipsExpressionsAndAllocations()
    {
        var channel = new DiagnosticChannel("Disabled");
        int evaluations = 0;
        for (int i = 0; i < 100; i++)
            channel.Warn($"hidden {Evaluate()}");

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++)
            channel.Warn($"hidden {Evaluate()}");
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, evaluations);
        Assert.Equal(0, allocated);

        int Evaluate() => ++evaluations;
    }

    [Fact]
    public void Info_DoesNotEvaluateFormattedExpressions_WhenDisabled()
    {
        var channel = new DiagnosticChannel("Gameplay")
        {
            MinimumLevel = DiagnosticLevel.Warning,
            Sink = (in DiagnosticEvent _) => throw new InvalidOperationException("Disabled diagnostics should not emit.")
        };
        int evaluationCount = 0;

        channel.Info($"expensive {Evaluate()}");

        Assert.Equal(0, evaluationCount);

        string Evaluate()
        {
            evaluationCount++;
            return "value";
        }
    }

    [Fact]
    public void Warn_EmitsDiagnosticWithResolvedCallerSource()
    {
        DiagnosticEvent captured = default;
        var channel = new DiagnosticChannel("Gameplay")
        {
            MinimumLevel = DiagnosticLevel.Info,
            Sink = (in DiagnosticEvent diagnostic) => captured = diagnostic
        };

        channel.Warn(
            $"value {42}",
            method: "Run",
            filePath: "/tmp/LoggerSource.cs");

        Assert.Equal("Gameplay", captured.Channel);
        Assert.Equal(DiagnosticLevel.Warning, captured.Level);
        Assert.Equal("value 42", captured.Message);
        Assert.Equal("LoggerSource.Run", captured.Source);
    }

    [Fact]
    public void Info_EmitsWhenEnabledAndFallsBackToMethodWithoutFilePath()
    {
        DiagnosticEvent captured = default;
        var channel = new DiagnosticChannel("Gameplay")
        {
            MinimumLevel = DiagnosticLevel.Info,
            Sink = (in DiagnosticEvent diagnostic) => captured = diagnostic
        };

        channel.Info($"ready", method: "Tick", filePath: "");

        Assert.Equal(DiagnosticLevel.Info, captured.Level);
        Assert.Equal("ready", captured.Message);
        Assert.Equal("Tick", captured.Source);
    }

    [Fact]
    public void Error_EmitsWhenEnabledAndFallsBackToClassWithoutMethod()
    {
        DiagnosticEvent captured = default;
        var channel = new DiagnosticChannel("Gameplay")
        {
            MinimumLevel = DiagnosticLevel.Info,
            Sink = (in DiagnosticEvent diagnostic) => captured = diagnostic
        };

        channel.Error($"failed", method: "", filePath: "/tmp/CombatSystem.cs");

        Assert.Equal(DiagnosticLevel.Error, captured.Level);
        Assert.Equal("failed", captured.Message);
        Assert.Equal("CombatSystem", captured.Source);
    }

    [Fact]
    public void Error_DoesNotEvaluateFormattedExpressions_WhenDisabled()
    {
        var channel = new DiagnosticChannel("Gameplay")
        {
            MinimumLevel = DiagnosticLevel.None,
            Sink = (in DiagnosticEvent _) => throw new InvalidOperationException("Disabled diagnostics should not emit.")
        };
        int evaluationCount = 0;

        channel.Error($"hidden {Evaluate()}");

        Assert.Equal(0, evaluationCount);

        string Evaluate()
        {
            evaluationCount++;
            return "value";
        }
    }

    [Fact]
    public void Log_UsesDynamicLevelForEnablement()
    {
        var entries = new List<DiagnosticEvent>();
        var channel = new DiagnosticChannel("Gameplay")
        {
            MinimumLevel = DiagnosticLevel.Warning,
            Sink = (in DiagnosticEvent diagnostic) => entries.Add(diagnostic)
        };
        int evaluationCount = 0;

        channel.Log(DiagnosticLevel.Info, $"hidden {Evaluate()}");
        channel.Log(DiagnosticLevel.Error, $"visible {Evaluate()}", method: "Write", filePath: "/tmp/DynamicLogger.cs");

        DiagnosticEvent result = Assert.Single(entries);
        Assert.Equal(1, evaluationCount);
        Assert.Equal(DiagnosticLevel.Error, result.Level);
        Assert.Equal("visible value", result.Message);
        Assert.Equal("DynamicLogger.Write", result.Source);

        string Evaluate()
        {
            evaluationCount++;
            return "value";
        }
    }

    [Fact]
    public void Log_UsesExplicitSourceWhenProvided()
    {
        DiagnosticEvent captured = default;
        var channel = new DiagnosticChannel("Gameplay")
        {
            MinimumLevel = DiagnosticLevel.Info,
            Sink = (in DiagnosticEvent diagnostic) => captured = diagnostic
        };

        channel.Log(DiagnosticLevel.Warning, $"visible", source: "ManualSource", method: "Ignored", filePath: "");

        Assert.Equal("ManualSource", captured.Source);
        Assert.Equal(DiagnosticLevel.Warning, captured.Level);
    }

    [Fact]
    public void Info_WithNullChannel_ThrowsAfterEnabledMessageIsPassedToWriteCore()
    {
        var enabledChannel = new DiagnosticChannel("Enabled")
        {
            MinimumLevel = DiagnosticLevel.Info
        };
        var message = new DiagnosticMessageHandler<InfoDiagnosticLevel>(0, 0, enabledChannel, out bool isEnabled);
        message.AppendLiteral("message");

        Assert.True(isEnabled);
        try
        {
            DiagnosticChannelLoggingExtensions.Info(null, message);
            throw new InvalidOperationException("Expected null channel to throw.");
        }
        catch (ArgumentNullException)
        {
        }
    }
    private static void AssertEntry(DiagnosticEvent entry, DiagnosticLevel level, string message, string source)
    {
        Assert.Equal("Consumer", entry.Channel);
        Assert.Equal(level, entry.Level);
        Assert.Equal(message, entry.Message);
        Assert.Equal(source, entry.Source);
    }
}
