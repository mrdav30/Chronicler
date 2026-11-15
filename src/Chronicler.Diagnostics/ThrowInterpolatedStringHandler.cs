//=======================================================================
// ThrowInterpolatedStringHandler.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

namespace Chronicler.Diagnostics;

using System;
using System.Runtime.CompilerServices;

/// <summary>Builds an exception message only for a failing guard.</summary>
/// <remarks>The compiler uses the constructor's gate to skip interpolation expressions when disabled. Append calls on a disabled handler have no effect.</remarks>
[InterpolatedStringHandler]
public ref struct ThrowInterpolatedStringHandler
{
    private DiagnosticInterpolatedStringHandler _message;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThrowInterpolatedStringHandler"/> struct.
    /// </summary>
    /// <param name="literalLength">The compiler-supplied count of literal UTF-16 code units.</param>
    /// <param name="formattedCount">The compiler-supplied number of interpolation holes.</param>
    /// <param name="condition">The throw condition that controls whether formatting occurs.</param>
    /// <param name="isEnabled">Set to true to evaluate interpolation expressions, or false to skip them.</param>
    public ThrowInterpolatedStringHandler(
        int literalLength,
        int formattedCount,
        bool condition,
        out bool isEnabled)
    {
        _message = new DiagnosticInterpolatedStringHandler(literalLength, formattedCount, condition, out isEnabled);
    }

    /// <summary>Gets the captured formatting gate.</summary>
    /// <value>True when the handler was constructed to build a message.</value>
    public bool IsEnabled => _message.IsEnabled;

    internal string GetFormattedText() => _message.GetFormattedText();

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
    public void AppendFormatted<T>(T value, string? format)
    {
        _message.AppendFormatted(value, format);
    }

    /// <inheritdoc cref="DiagnosticInterpolatedStringHandler.AppendFormatted{T}(T, int)"/>
    public void AppendFormatted<T>(T value, int alignment)
    {
        _message.AppendFormatted(value, alignment);
    }

    /// <inheritdoc cref="DiagnosticInterpolatedStringHandler.AppendFormatted{T}(T, int, string)"/>
    public void AppendFormatted<T>(T value, int alignment, string? format)
    {
        _message.AppendFormatted(value, alignment, format);
    }

    /// <inheritdoc cref="DiagnosticInterpolatedStringHandler.AppendFormatted(string)"/>
    public void AppendFormatted(string? value)
    {
        _message.AppendFormatted(value);
    }

    /// <inheritdoc cref="DiagnosticInterpolatedStringHandler.AppendFormatted(string, int)"/>
    public void AppendFormatted(string? value, int alignment)
    {
        _message.AppendFormatted(value, alignment);
    }

    /// <inheritdoc cref="DiagnosticInterpolatedStringHandler.AppendFormatted(string, int, string)"/>
    public void AppendFormatted(string? value, int alignment, string? format)
    {
        _message.AppendFormatted(value, alignment, format);
    }

    /// <inheritdoc cref="DiagnosticInterpolatedStringHandler.AppendFormatted(ReadOnlySpan{char})"/>
    public void AppendFormatted(ReadOnlySpan<char> value)
    {
        _message.AppendFormatted(value);
    }

    /// <inheritdoc cref="DiagnosticInterpolatedStringHandler.AppendFormatted(ReadOnlySpan{char}, int)"/>
    public void AppendFormatted(ReadOnlySpan<char> value, int alignment)
    {
        _message.AppendFormatted(value, alignment);
    }

    /// <inheritdoc cref="DiagnosticInterpolatedStringHandler.AppendFormatted(ReadOnlySpan{char}, int, string)"/>
    public void AppendFormatted(ReadOnlySpan<char> value, int alignment, string? format)
    {
        _message.AppendFormatted(value, alignment, format);
    }
}
