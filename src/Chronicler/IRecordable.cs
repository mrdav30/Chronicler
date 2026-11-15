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
    /// Transfers the instance's declared state through the active recording pass.
    /// </summary>
    /// <param name="chronicler">Use its mode to read source state or apply loaded state. The pass is valid during this call.</param>
    /// <remarks>
    /// The sequence of recording calls defines field processing order. Keep field names stable across passes.
    /// Register host-owned link targets through <see cref="IChronicler.Context"/> when they become available during loading.
    /// </remarks>
    void RecordData(IChronicler chronicler);
}
