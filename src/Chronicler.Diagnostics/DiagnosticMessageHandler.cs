//=======================================================================
// DiagnosticMessageHandler.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

namespace Chronicler.Diagnostics;

using System.Runtime.CompilerServices;

/// <summary>Builds a diagnostic message using an enablement decision captured at construction.</summary>
/// <typeparam name="TLevel">The default provider for fixed-level calls. The dynamic constructor takes a level explicitly.</typeparam>
/// <remarks>The compiler uses the constructor's gate to skip interpolation expressions when disabled. Append calls on a disabled handler have no effect.</remarks>
[InterpolatedStringHandler]
public ref struct DiagnosticMessageHandler<TLevel>
    where TLevel : struct, IDiagnosticLevelProvider
{
    private DiagnosticInterpolatedStringHandler _message;

    /// <summary>
    /// Initializes a new handler for a fixed-level diagnostic message.
    /// </summary>
    /// <param name="literalLength">The compiler-supplied count of literal UTF-16 code units.</param>
    /// <param name="formattedCount">The compiler-supplied number of interpolation holes.</param>
    /// <param name="channel">The diagnostic channel that receives the message.</param>
    /// <param name="isEnabled">Set to true to evaluate interpolation expressions, or false to skip them.</param>
    /// <exception cref="System.ArgumentNullException">The channel is null.</exception>
    public DiagnosticMessageHandler(
        int literalLength,
        int formattedCount,
        DiagnosticChannel channel,
        out bool isEnabled)
        : this(literalLength, formattedCount, channel, default(TLevel).Level, out isEnabled)
    {
    }

    /// <summary>
    /// Initializes a new handler for a dynamic-level diagnostic message.
    /// </summary>
    /// <param name="literalLength">The compiler-supplied count of literal UTF-16 code units.</param>
    /// <param name="formattedCount">The compiler-supplied number of interpolation holes.</param>
    /// <param name="channel">The diagnostic channel that receives the message.</param>
    /// <param name="level">The diagnostic level being evaluated.</param>
    /// <param name="isEnabled">Set to true to evaluate interpolation expressions, or false to skip them.</param>
    /// <exception cref="System.ArgumentNullException">The channel is null.</exception>
    public DiagnosticMessageHandler(
        int literalLength,
        int formattedCount,
        DiagnosticChannel channel,
        DiagnosticLevel level,
        out bool isEnabled)
    {
        ChronicleGuard.ThrowIfNull(channel, nameof(channel));
        _message = new DiagnosticInterpolatedStringHandler(literalLength, formattedCount, channel, level, out isEnabled);
    }

    /// <summary>Gets the captured formatting gate.</summary>
    /// <value>True when the handler was constructed to build a message.</value>
    public bool IsEnabled => _message.IsEnabled;

    internal DiagnosticInterpolatedStringHandler Message => _message;

    /// <inheritdoc cref="DiagnosticInterpolatedStringHandler.AppendLiteral(string)"/>
    public void AppendLiteral(string value)
    {
        _message.AppendLiteral(value);
    }

    /// <inheritdoc cref="DiagnosticInterpolatedStringHandler.AppendFormatted{T}(T)"/>
    public void AppendFormatted<T>(T value)
    {
        _message.AppendFormatted(value);
    }

    /// <inheritdoc cref="DiagnosticInterpolatedStringHandler.AppendFormatted{T}(T, string)"/>
    public void AppendFormatted<T>(T value, string format)
    {
        _message.AppendFormatted(value, format);
    }

    /// <inheritdoc cref="DiagnosticInterpolatedStringHandler.AppendFormatted{T}(T, int)"/>
    public void AppendFormatted<T>(T value, int alignment)
    {
        _message.AppendFormatted(value, alignment);
    }

    /// <inheritdoc cref="DiagnosticInterpolatedStringHandler.AppendFormatted{T}(T, int, string)"/>
    public void AppendFormatted<T>(T value, int alignment, string format)
    {
        _message.AppendFormatted(value, alignment, format);
    }
}
