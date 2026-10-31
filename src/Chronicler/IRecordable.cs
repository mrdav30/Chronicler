//=======================================================================
// IRecordable.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

namespace Chronicler;

/// <summary>
/// Defines a type that can record its serializable state through a chronicler pass.
/// </summary>
public interface IRecordable
{
    /// <summary>
    /// Records the current instance state to the provided chronicler.
    /// </summary>
    /// <param name="chronicler">The active chronicler pass.</param>
    void RecordData(IChronicler chronicler);
}
