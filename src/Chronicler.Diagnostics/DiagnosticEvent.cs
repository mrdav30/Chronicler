//=======================================================================
// DiagnosticEvent.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

namespace Chronicler.Diagnostics;

/// <summary>
/// Represents a single diagnostic event emitted by a <see cref="DiagnosticChannel"/>.
/// </summary>
public readonly struct DiagnosticEvent
{
    /// <summary>Creates an immutable diagnostic event.</summary>
    /// <param name="channel">The channel label. Null becomes empty.</param>
    /// <param name="level">The severity attached to the event.</param>
    /// <param name="message">The rendered event text. Null becomes empty.</param>
    /// <param name="source">The optional origin label. Null or omission becomes empty.</param>
    public DiagnosticEvent(
        string channel,
        DiagnosticLevel level,
        string message,
        string source = "")
    {
        Channel = channel ?? string.Empty;
        Level = level;
        Message = message ?? string.Empty;
        Source = source ?? string.Empty;
    }

    /// <summary>Gets the channel label.</summary>
    /// <value>The emitting channel's name.</value>
    public string Channel { get; }

    /// <summary>Gets event severity.</summary>
    /// <value>The level carried through to sinks and formatters.</value>
    public DiagnosticLevel Level { get; }

    /// <summary>Gets the event text.</summary>
    /// <value>The caller's formatted message, with null normalized to empty.</value>
    public string Message { get; }

    /// <summary>Gets the origin label.</summary>
    /// <value>The supplied origin or an empty string.</value>
    public string Source { get; }
}
