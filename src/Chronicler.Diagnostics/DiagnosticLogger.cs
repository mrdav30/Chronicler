//=======================================================================
// DiagnosticLogger.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

namespace Chronicler.Diagnostics;

using System;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Provides reusable logger state and diagnostic channels for libraries using Chronicler diagnostics.
/// </summary>
/// <remarks>
/// Static logger facades can delegate to a derived instance of this type while keeping their existing public API.
/// </remarks>
public abstract class DiagnosticLogger
{
    private readonly DiagnosticChannel _channel;
    private readonly DiagnosticChannel _debugChannel;
    private Action<DiagnosticLevel, string, string> _logHandler;
    private Func<DiagnosticLevel, string, string, string> _customFormatter;
    private bool _enableDebugLogging;

    /// <summary>Creates normal and debug channels for a library logger.</summary>
    /// <param name="channelName">The shared channel label. Null or blank uses <c>Default</c>.</param>
    /// <remarks>Normal diagnostics start at Warning. Debug diagnostics start disabled.</remarks>
    protected DiagnosticLogger(string channelName)
    {
        _channel = CreateChannel(channelName);
        _debugChannel = CreateChannel(channelName);
        Name = _channel.Name;
        _logHandler = DefaultLogHandler;
        _customFormatter = DefaultLogFormatter;
        MinimumLevel = DiagnosticLevel.Warning;
        RefreshDebugMinimumLevel();
    }

    /// <summary>Gets the logger label.</summary>
    /// <value>The normalized name shared by its two channels.</value>
    public string Name { get; }

    /// <summary>Gets the normal diagnostics channel.</summary>
    /// <value>The same mutable channel throughout the logger's lifetime. Its initial threshold is Warning.</value>
    public DiagnosticChannel Channel => _channel;

    /// <summary>Gets the independently gated debug channel.</summary>
    /// <value>The same mutable channel throughout the logger's lifetime. Its initial threshold is None.</value>
    public DiagnosticChannel DebugChannel => _debugChannel;

    /// <summary>Gets or sets the debug gate.</summary>
    /// <value>False by default. Setting this property selects the normal threshold or disables the debug channel.</value>
    public bool EnableDebugLogging
    {
        get => _enableDebugLogging;
        set
        {
            _enableDebugLogging = value;
            RefreshDebugMinimumLevel();
        }
    }

    /// <summary>Gets or sets the normal channel's threshold.</summary>
    /// <value>Setting the property also updates the debug threshold when debug logging is enabled.</value>
    public DiagnosticLevel MinimumLevel
    {
        get => _channel.MinimumLevel;
        set
        {
            _channel.MinimumLevel = value;
            RefreshDebugMinimumLevel();
        }
    }

    /// <summary>Gets or sets the log dispatch callback.</summary>
    /// <value>The retained callback receiving severity, message, and resolved source. Null restores <see cref="DefaultLogHandler"/>.</value>
    [AllowNull]
    public Action<DiagnosticLevel, string, string> LogHandler
    {
        get => _logHandler;
        set => _logHandler = value ?? DefaultLogHandler;
    }

    /// <summary>Gets or sets the formatter used by the default log handler.</summary>
    /// <value>The retained formatter mapping severity, message, and source to one entry. Null restores <see cref="DefaultLogFormatter"/>.</value>
    [AllowNull]
    public Func<DiagnosticLevel, string, string, string> CustomFormatter
    {
        get => _customFormatter;
        set => _customFormatter = value ?? DefaultLogFormatter;
    }

    /// <summary>
    /// Determines whether normal diagnostics at the specified level are currently enabled.
    /// </summary>
    /// <param name="level">The diagnostic level to evaluate.</param>
    /// <returns><see langword="true"/> when messages at <paramref name="level"/> will be emitted; otherwise, <see langword="false"/>.</returns>
    public bool IsEnabled(DiagnosticLevel level)
    {
        return _channel.IsEnabled(level);
    }

    /// <summary>Formats a message and writes one console line.</summary>
    /// <param name="level">Error goes to standard error. Other levels go to standard output.</param>
    /// <param name="message">The text passed to the current formatter.</param>
    /// <param name="source">The origin label passed to the current formatter.</param>
    /// <remarks>Calling directly bypasses both channel thresholds.</remarks>
    public virtual void DefaultLogHandler(DiagnosticLevel level, string message, string source)
    {
        string entry = CustomFormatter(level, message, source);
        if (level == DiagnosticLevel.Error)
            Console.Error.WriteLine(entry);
        else
            Console.WriteLine(entry);
    }

    /// <summary>Formats a logger entry with severity and origin.</summary>
    /// <param name="level">The severity written in brackets.</param>
    /// <param name="message">The text appended after the logger and source prefix.</param>
    /// <param name="source">The suffix on the logger name. Null or whitespace omits the suffix.</param>
    /// <returns>Text of the form <c>[level] Name.Source: message</c>. The formatter adds no line break.</returns>
    public virtual string DefaultLogFormatter(DiagnosticLevel level, string message, string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return $"[{level}] {Name}: {message}";

        return $"[{level}] {Name}.{source}: {message}";
    }

    /// <summary>Dispatches an event from either channel through the current log handler.</summary>
    /// <param name="diagnostic">The already enabled event whose severity, message, and resolved source are forwarded.</param>
    protected virtual void HandleDiagnosticEvent(in DiagnosticEvent diagnostic)
    {
        _logHandler(diagnostic.Level, diagnostic.Message, ResolveSource(diagnostic));
    }

    /// <summary>Selects the origin label forwarded to the log handler.</summary>
    /// <param name="diagnostic">The event whose source and channel names are inspected.</param>
    /// <returns>The event's source, or its channel name when the source is blank.</returns>
    protected virtual string ResolveSource(in DiagnosticEvent diagnostic)
    {
        return string.IsNullOrWhiteSpace(diagnostic.Source)
            ? diagnostic.Channel
            : diagnostic.Source;
    }

    private DiagnosticChannel CreateChannel(string channelName)
    {
        return new DiagnosticChannel(channelName)
        {
            MinimumLevel = DiagnosticLevel.Warning,
            Sink = HandleDiagnosticEvent
        };
    }

    private void RefreshDebugMinimumLevel()
    {
        _debugChannel.MinimumLevel = _enableDebugLogging
            ? _channel.MinimumLevel
            : DiagnosticLevel.None;
    }
}
