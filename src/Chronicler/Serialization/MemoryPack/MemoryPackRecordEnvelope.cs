//=======================================================================
// MemoryPackRecordEnvelope.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

#if !CHRONICLER_DISABLE_MEMORYPACK
using System;
using MemoryPack;

namespace Chronicler.Serialization;

/// <summary>
/// Stores named field payloads for the MemoryPack chronicler transport.
/// </summary>
[MemoryPackable]
internal sealed partial class MemoryPackRecordEnvelope
{
    /// <summary>
    /// Serialized payloads keyed by record name.
    /// </summary>
    public MemoryPackRecordEntryTable? Entries { get; set; } = new();

    internal static MemoryPackRecordEnvelope FromEntries(OrderedStringMap<byte[]?> entries)
    {
        return new MemoryPackRecordEnvelope
        {
            Entries = new MemoryPackRecordEntryTable(entries)
        };
    }

    internal OrderedStringMap<byte[]?> ToEntryMap()
    {
        return Entries?.ToEntryMap() ?? new OrderedStringMap<byte[]?>(8, StringComparer.Ordinal);
    }

    internal bool RemoveEntry(string name)
    {
        return Entries?.Remove(name) ?? false;
    }

    internal void SetEntry(string name, byte[]? payload)
    {
        Entries ??= new MemoryPackRecordEntryTable();
        Entries[name] = payload;
    }

    internal bool TryGetEntry(string name, out byte[]? payload)
    {
        if (Entries == null)
        {
            payload = null;
            return false;
        }

        return Entries.TryGetValue(name, out payload);
    }
}
#endif
