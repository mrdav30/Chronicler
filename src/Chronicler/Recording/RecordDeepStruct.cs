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
    /// <summary>
    /// Reads or writes a named nested struct.
    /// </summary>
    public static void Look<T>(IChronicler chronicler, ref T value, string name) where T : struct, IRecordable
    {
        chronicler.LookDeepStruct(ref value, name);
    }
}
