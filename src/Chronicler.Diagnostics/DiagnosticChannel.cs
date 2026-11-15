//=======================================================================
// DiagnosticChannel.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

namespace Chronicler.Diagnostics;

using System.Runtime.CompilerServices;

/// <summary>Routes diagnostics at enabled levels to a sink.</summary>
/// <remarks>The sink runs synchronously on the caller's thread. The channel starts disabled with a no-op sink.</remarks>
public sealed class DiagnosticChannel
{
    private static readonly DiagnosticSink NoopSink = static (in DiagnosticEvent _) => { };
    private DiagnosticSink _sink;

    /// <summary>Creates a disabled channel.</summary>
    /// <param name="name">The event channel name. Null, empty, or whitespace selects <c>Default</c>.</param>
    public DiagnosticChannel(string name)
    {
        Name = string.IsNullOrWhiteSpace(name) ? "Default" : name;
        _sink = NoopSink;
        MinimumLevel = DiagnosticLevel.None;
    }

    /// <summary>Gets the channel label.</summary>
    /// <value>The normalized name included in every event emitted by this channel.</value>
    public string Name { get; }

    /// <summary>Gets or sets the severity threshold.</summary>
    /// <value>The lowest enabled level. The default, <see cref="DiagnosticLevel.None"/>, disables the channel.</value>
    public DiagnosticLevel MinimumLevel { get; set; }

    /// <summary>Gets or sets the dispatch callback.</summary>
    /// <value>The retained delegate invoked for emitted events. Assign null to restore the no-op sink.</value>
    public DiagnosticSink Sink
    {
        get => _sink;
        set => _sink = value ?? NoopSink;
    }

    /// <summary>Checks the channel's current severity threshold.</summary>
    /// <param name="level">The severity being considered. <see cref="DiagnosticLevel.None"/> is always disabled.</param>
    /// <returns>True when the channel is enabled and the severity reaches its threshold.</returns>
    public bool IsEnabled(DiagnosticLevel level)
    {
        return MinimumLevel != DiagnosticLevel.None
            && level >= MinimumLevel
            && level != DiagnosticLevel.None;
    }

    /// <summary>Emits an already formatted event when the level is enabled.</summary>
    /// <param name="level">The severity tested against the current threshold.</param>
    /// <param name="message">The message text, formatted by the caller before this call.</param>
    /// <param name="source">An optional origin label copied into the event, or an empty string when omitted.</param>
    /// <remarks>Invokes the sink inline. Exceptions from the sink propagate to the caller.</remarks>
    public void Write(DiagnosticLevel level, string message, string source = "")
    {
        if (!IsEnabled(level))
            return;

        _sink(new DiagnosticEvent(Name, level, message, source));
    }

    /// <summary>Emits an interpolated event using the handler's enabled decision.</summary>
    /// <param name="level">The event severity used when creating the handler.</param>
    /// <param name="message">The handler whose captured gate controls delivery and skips interpolation when disabled.</param>
    /// <param name="source">An optional origin label copied into the event, or an empty string when omitted.</param>
    /// <remarks>Invokes the sink inline. Exceptions from the sink propagate to the caller.</remarks>
    public void Write(
        DiagnosticLevel level,
        [InterpolatedStringHandlerArgument("", nameof(level))] DiagnosticInterpolatedStringHandler message,
        string source = "")
    {
        if (!message.IsEnabled)
            return;

        _sink(new DiagnosticEvent(Name, level, message.GetFormattedText(), source));
    }
}
