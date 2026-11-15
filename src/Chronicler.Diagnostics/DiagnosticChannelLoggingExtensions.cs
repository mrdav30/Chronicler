//=======================================================================
// DiagnosticChannelLoggingExtensions.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

namespace Chronicler.Diagnostics;

using System.IO;
using System.Runtime.CompilerServices;

/// <summary>
/// Emits fixed- or dynamic-level messages through a <see cref="DiagnosticChannel"/>.
/// </summary>
/// <remarks>
/// A nonempty source takes precedence over caller information. An empty source combines the caller file stem
/// and member name when available. Sinks run inline and their exceptions propagate.
/// </remarks>
public static class DiagnosticChannelLoggingExtensions
{
    /// <summary>
    /// Writes an already formatted info message when info diagnostics are enabled.
    /// </summary>
    /// <param name="channel">The diagnostic channel that receives the message.</param>
    /// <param name="message">The already formatted diagnostic message.</param>
    /// <param name="source">The explicit source label. Empty or omitted selects the caller-derived source.</param>
    /// <param name="method">The captured caller member name used in the fallback source.</param>
    /// <param name="filePath">The captured caller path. The file name without its extension supplies the first part of the fallback source.</param>
    /// <exception cref="System.ArgumentNullException">The channel is null.</exception>
    public static void Info(
        this DiagnosticChannel channel,
        string message,
        string source = "",
        [CallerMemberName] string method = "",
        [CallerFilePath] string filePath = "") =>
        Log(channel, DiagnosticLevel.Info, message, source, method, filePath);

    /// <summary>
    /// Writes an already formatted warning message when warning diagnostics are enabled.
    /// </summary>
    /// <param name="channel">The diagnostic channel that receives the message.</param>
    /// <param name="message">The already formatted diagnostic message.</param>
    /// <param name="source">The explicit source label. Empty or omitted selects the caller-derived source.</param>
    /// <param name="method">The captured caller member name used in the fallback source.</param>
    /// <param name="filePath">The captured caller path. The file name without its extension supplies the first part of the fallback source.</param>
    /// <exception cref="System.ArgumentNullException">The channel is null.</exception>
    public static void Warn(
        this DiagnosticChannel channel,
        string message,
        string source = "",
        [CallerMemberName] string method = "",
        [CallerFilePath] string filePath = "") =>
        Log(channel, DiagnosticLevel.Warning, message, source, method, filePath);

    /// <summary>
    /// Writes an already formatted error message when error diagnostics are enabled.
    /// </summary>
    /// <param name="channel">The diagnostic channel that receives the message.</param>
    /// <param name="message">The already formatted diagnostic message.</param>
    /// <param name="source">The explicit source label. Empty or omitted selects the caller-derived source.</param>
    /// <param name="method">The captured caller member name used in the fallback source.</param>
    /// <param name="filePath">The captured caller path. The file name without its extension supplies the first part of the fallback source.</param>
    /// <exception cref="System.ArgumentNullException">The channel is null.</exception>
    public static void Error(
        this DiagnosticChannel channel,
        string message,
        string source = "",
        [CallerMemberName] string method = "",
        [CallerFilePath] string filePath = "") =>
        Log(channel, DiagnosticLevel.Error, message, source, method, filePath);

    /// <summary>
    /// Writes an already formatted message when the requested diagnostic level is enabled.
    /// </summary>
    /// <param name="channel">The diagnostic channel that receives the message.</param>
    /// <param name="level">The severity level of the diagnostic event.</param>
    /// <param name="message">The already formatted diagnostic message.</param>
    /// <param name="source">The explicit source label. Empty or omitted selects the caller-derived source.</param>
    /// <param name="method">The captured caller member name used in the fallback source.</param>
    /// <param name="filePath">The captured caller path. The file name without its extension supplies the first part of the fallback source.</param>
    /// <exception cref="System.ArgumentNullException">The channel is null.</exception>
    public static void Log(
        this DiagnosticChannel channel,
        DiagnosticLevel level,
        string message,
        string source = "",
        [CallerMemberName] string method = "",
        [CallerFilePath] string filePath = "")
    {
        ChronicleGuard.ThrowIfNull(channel, nameof(channel));
        if (!channel.IsEnabled(level))
            return;

        channel.Write(level, message, ResolveSource(source, method, filePath));
    }

    /// <summary>
    /// Writes an info diagnostic message without evaluating formatted expressions when info diagnostics are disabled.
    /// </summary>
    /// <param name="channel">The diagnostic channel that receives the message.</param>
    /// <param name="message">The interpolated diagnostic message.</param>
    /// <param name="source">The explicit source label. Empty or omitted selects the caller-derived source.</param>
    /// <param name="method">The captured caller member name used in the fallback source.</param>
    /// <param name="filePath">The captured caller path. The file name without its extension supplies the first part of the fallback source.</param>
    /// <exception cref="System.ArgumentNullException">The channel is null.</exception>
    public static void Info(
        this DiagnosticChannel channel,
        [InterpolatedStringHandlerArgument("channel")] DiagnosticMessageHandler<InfoDiagnosticLevel> message,
        string source = "",
        [CallerMemberName] string method = "",
        [CallerFilePath] string filePath = "")
    {
        if (!message.IsEnabled)
            return;

        WriteCore(channel, DiagnosticLevel.Info, message.Message, ResolveSource(source, method, filePath));
    }

    /// <summary>
    /// Writes a warning diagnostic message without evaluating formatted expressions when warning diagnostics are disabled.
    /// </summary>
    /// <param name="channel">The diagnostic channel that receives the message.</param>
    /// <param name="message">The interpolated diagnostic message.</param>
    /// <param name="source">The explicit source label. Empty or omitted selects the caller-derived source.</param>
    /// <param name="method">The captured caller member name used in the fallback source.</param>
    /// <param name="filePath">The captured caller path. The file name without its extension supplies the first part of the fallback source.</param>
    /// <exception cref="System.ArgumentNullException">The channel is null.</exception>
    public static void Warn(
        this DiagnosticChannel channel,
        [InterpolatedStringHandlerArgument("channel")] DiagnosticMessageHandler<WarningDiagnosticLevel> message,
        string source = "",
        [CallerMemberName] string method = "",
        [CallerFilePath] string filePath = "")
    {
        if (!message.IsEnabled)
            return;

        WriteCore(channel, DiagnosticLevel.Warning, message.Message, ResolveSource(source, method, filePath));
    }

    /// <summary>
    /// Writes an error diagnostic message without evaluating formatted expressions when error diagnostics are disabled.
    /// </summary>
    /// <param name="channel">The diagnostic channel that receives the message.</param>
    /// <param name="message">The interpolated diagnostic message.</param>
    /// <param name="source">The explicit source label. Empty or omitted selects the caller-derived source.</param>
    /// <param name="method">The captured caller member name used in the fallback source.</param>
    /// <param name="filePath">The captured caller path. The file name without its extension supplies the first part of the fallback source.</param>
    /// <exception cref="System.ArgumentNullException">The channel is null.</exception>
    public static void Error(
        this DiagnosticChannel channel,
        [InterpolatedStringHandlerArgument("channel")] DiagnosticMessageHandler<ErrorDiagnosticLevel> message,
        string source = "",
        [CallerMemberName] string method = "",
        [CallerFilePath] string filePath = "")
    {
        if (!message.IsEnabled)
            return;

        WriteCore(channel, DiagnosticLevel.Error, message.Message, ResolveSource(source, method, filePath));
    }

    /// <summary>
    /// Writes a dynamic-level diagnostic message without evaluating formatted expressions when the level is disabled.
    /// </summary>
    /// <param name="channel">The diagnostic channel that receives the message.</param>
    /// <param name="level">The severity level of the diagnostic event.</param>
    /// <param name="message">The interpolated diagnostic message.</param>
    /// <param name="source">The explicit source label. Empty or omitted selects the caller-derived source.</param>
    /// <param name="method">The captured caller member name used in the fallback source.</param>
    /// <param name="filePath">The captured caller path. The file name without its extension supplies the first part of the fallback source.</param>
    /// <exception cref="System.ArgumentNullException">The channel is null.</exception>
    public static void Log(
        this DiagnosticChannel channel,
        DiagnosticLevel level,
        [InterpolatedStringHandlerArgument("channel", "level")] DiagnosticMessageHandler<DynamicDiagnosticLevel> message,
        string source = "",
        [CallerMemberName] string method = "",
        [CallerFilePath] string filePath = "")
    {
        if (!message.IsEnabled)
            return;

        WriteCore(channel, level, message.Message, ResolveSource(source, method, filePath));
    }

    private static void WriteCore(
        DiagnosticChannel channel,
        DiagnosticLevel level,
        DiagnosticInterpolatedStringHandler message,
        string source)
    {
        ChronicleGuard.ThrowIfNull(channel, nameof(channel));
        channel.Write(level, message, source);
    }

    private static string ResolveSource(string source, string method, string filePath)
    {
        if (!string.IsNullOrEmpty(source))
            return source;

        string className = Path.GetFileNameWithoutExtension(filePath);
        if (string.IsNullOrEmpty(className))
            return method;

        if (string.IsNullOrEmpty(method))
            return className;

        return $"{className}.{method}";
    }
}
