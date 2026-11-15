//=======================================================================
// RecordValues.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

namespace Chronicler;

/// <summary>
/// Helper for reading and writing leaf values during a chronicler pass.
/// </summary>
public static class RecordValues
{
    /// <inheritdoc cref="IChronicler.LookValue{T}(ref T, string, T)" path="/*[not(self::param)]"/>
    /// <param name="chronicler">The active pass supplied to <see cref="IRecordable.RecordData"/>.</param>
    /// <param name="value">The value to write on save. On load, receives the decoded value or the declared default for a missing or null entry.</param>
    /// <param name="name">The stable field name shared by saving and loading.</param>
    /// <param name="defaultValue">The schema default. The built-in transports omit null values and values equal to this default when saving.</param>
    public static void Look<T>(IChronicler chronicler, ref T value, string name, T? defaultValue = default)
    {
        chronicler.LookValue(ref value, name, defaultValue);
    }
}
