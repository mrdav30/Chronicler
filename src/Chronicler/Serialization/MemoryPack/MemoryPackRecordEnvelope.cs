using MemoryPack;
using System;
using System.Collections.Generic;

namespace Chronicler;

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
        _entries = entries ?? throw new ArgumentNullException(nameof(entries));
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
