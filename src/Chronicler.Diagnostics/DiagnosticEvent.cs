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
/// <remarks>
/// Initializes a new instance of the <see cref="DiagnosticEvent"/> struct.
/// </remarks>
/// <param name="channel">The channel that emitted the event.</param>
/// <param name="level">The severity level of the event.</param>
/// <param name="message">The event message.</param>
/// <param name="source">An optional source identifier.</param>
public readonly struct DiagnosticEvent(
    string channel,
    DiagnosticLevel level,
    string message,
    string source = "")
{

    /// <summary>
    /// Gets the channel that emitted the event.
    /// </summary>
    public string Channel { get; } = channel ?? string.Empty;

    /// <summary>
    /// Gets the severity level of the event.
    /// </summary>
    public DiagnosticLevel Level { get; } = level;

    /// <summary>
    /// Gets the diagnostic message.
    /// </summary>
    public string Message { get; } = message ?? string.Empty;

    /// <summary>
    /// Gets the optional source identifier.
    /// </summary>
    public string Source { get; } = source ?? string.Empty;
}
