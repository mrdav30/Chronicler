//=======================================================================
// RecordDeepStruct.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

namespace Chronicler;

/// <summary>
/// Helper for reading and writing nested recordable structs during a chronicler pass.
/// </summary>
public static class RecordDeepStruct
{
    /// <inheritdoc cref="IChronicler.LookDeepStruct{T}(ref T, string)" path="/*[not(self::param)]"/>
    /// <param name="chronicler">The active pass supplied to <see cref="IRecordable.RecordData"/>.</param>
    /// <param name="value">The value to write on save. A present record loads into a fresh value. For missing or null input, the pass runs the schema against an empty record before replacing this value.</param>
    /// <param name="name">The stable field name shared by saving and loading.</param>
    public static void Look<T>(IChronicler chronicler, ref T value, string name) where T : struct, IRecordable
    {
        chronicler.LookDeepStruct(ref value, name);
    }
}
