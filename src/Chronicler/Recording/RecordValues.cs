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
    /// <summary>
    /// Reads or writes a named value.
    /// </summary>
    public static void Look<T>(IChronicler chronicler, ref T value, string name, T? defaultValue = default)
    {
        chronicler.LookValue(ref value, name, defaultValue);
    }
}
