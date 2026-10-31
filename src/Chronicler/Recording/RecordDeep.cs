//=======================================================================
// RecordDeep.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

namespace Chronicler;

/// <summary>
/// Helper for reading and writing nested recordable objects during a chronicler pass.
/// </summary>
public static class RecordDeep
{
    /// <summary>
    /// Reads or writes a named nested object.
    /// </summary>
    public static void Look<T>(IChronicler chronicler, ref T value, string name) where T : class, IRecordable
    {
        chronicler.LookDeep(ref value, name);
    }
}
