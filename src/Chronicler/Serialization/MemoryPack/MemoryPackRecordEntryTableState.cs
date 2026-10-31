//=======================================================================
// MemoryPackRecordEntryTableState.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

#if !CHRONICLER_DISABLE_MEMORYPACK
using System.Collections.Generic;
using MemoryPack;

namespace Chronicler.Serialization;

/// <summary>
/// Represents the serializable state of a MemoryPack record entry table.
/// </summary>
[MemoryPackable]
internal readonly partial struct MemoryPackRecordEntryTableState
{
    /// <summary>
    /// Gets the ordered entry items.
    /// </summary>
    [MemoryPackInclude]
    public readonly KeyValuePair<string, byte[]?>[]? Items;

    [MemoryPackConstructor]
    public MemoryPackRecordEntryTableState(KeyValuePair<string, byte[]?>[]? items)
    {
        Items = items;
    }
}
#endif
