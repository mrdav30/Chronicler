//=======================================================================
// MemoryPackRecordEntryTable.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

#if !CHRONICLER_DISABLE_MEMORYPACK
using System;
using System.Collections.Generic;
using MemoryPack;

namespace Chronicler.Serialization;

/// <summary>
/// Stores ordered MemoryPack entry payloads while preserving the previous SwiftDictionary wire shape.
/// </summary>
[MemoryPackable]
internal sealed partial class MemoryPackRecordEntryTable
{
    private OrderedStringMap<byte[]?> _entries;

    public MemoryPackRecordEntryTable()
    {
        _entries = new OrderedStringMap<byte[]?>(8, StringComparer.Ordinal);
    }

    internal MemoryPackRecordEntryTable(OrderedStringMap<byte[]?> entries)
    {
        _entries = entries;
    }

    [MemoryPackConstructor]
    public MemoryPackRecordEntryTable(MemoryPackRecordEntryTableState state)
        : this()
    {
        LoadState(state);
    }

    /// <summary>
    /// Gets or sets the serializable table state.
    /// </summary>
    [MemoryPackInclude]
    public MemoryPackRecordEntryTableState State
    {
        get
        {
            var items = new KeyValuePair<string, byte[]?>[_entries.Count];
            int index = 0;

            foreach (KeyValuePair<string, byte[]?> entry in _entries)
                items[index++] = entry;

            return new MemoryPackRecordEntryTableState(items);
        }
        internal set => LoadState(value);
    }

    internal byte[]? this[string name]
    {
        set => _entries[name] = value;
    }

    internal bool Remove(string name)
    {
        return _entries.Remove(name);
    }

    internal OrderedStringMap<byte[]?> ToEntryMap()
    {
        var entries = new OrderedStringMap<byte[]?>(_entries.Count, StringComparer.Ordinal);

        foreach (KeyValuePair<string, byte[]?> entry in _entries)
            entries[entry.Key] = entry.Value;

        return entries;
    }

    internal bool TryGetValue(string name, out byte[]? payload)
    {
        return _entries.TryGetValue(name, out payload);
    }

    private void LoadState(MemoryPackRecordEntryTableState state)
    {
        KeyValuePair<string, byte[]?>[]? items = state.Items;
        int capacity = items?.Length ?? 8;
        var entries = new OrderedStringMap<byte[]?>(capacity, StringComparer.Ordinal);

        if (items != null)
        {
            foreach (KeyValuePair<string, byte[]?> item in items)
                entries[item.Key] = item.Value;
        }

        _entries = entries;
    }
}
#endif
