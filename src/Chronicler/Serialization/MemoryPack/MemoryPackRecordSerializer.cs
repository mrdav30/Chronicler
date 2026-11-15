//=======================================================================
// MemoryPackRecordSerializer.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

#if !CHRONICLER_DISABLE_MEMORYPACK

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MemoryPack;

namespace Chronicler.Serialization;

/// <summary>
/// Saves recorded state as MemoryPack bytes and restores it into host-created objects.
/// </summary>
/// <remarks>Each call made without a session context creates one with an empty link registry.</remarks>
public static class MemoryPackRecordSerializer
{
    private static readonly byte[] EmptyRecordBytes = MemoryPackSerializer.Serialize(new MemoryPackRecordEnvelope());

    /// <inheritdoc cref="Serialize(IRecordable, ChronicleContext)"/>
    public static byte[] Serialize(IRecordable target)
        => Serialize(target, context: null);

    /// <summary>
    /// Saves the state declared by a recordable instance.
    /// </summary>
    /// <param name="target">The source whose schema runs in <see cref="SerializationMode.Saving"/> mode.</param>
    /// <param name="context">The session that supplies stable link IDs. Null creates a fresh context.</param>
    /// <returns>The recorded payload in a new byte array owned by the caller.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="target"/> is null.</exception>
    /// <exception cref="InvalidOperationException">A non-null recorded link has no stable ID.</exception>
    public static byte[] Serialize(IRecordable target, ChronicleContext? context)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        context ??= new ChronicleContext();

        var chronicler = new MemoryPackRecordWriter(context);
        target.RecordData(chronicler);
        return chronicler.ToArray();
    }

    /// <inheritdoc cref="Populate(IRecordable, byte[], ChronicleContext)"/>
    public static void Populate(IRecordable target, byte[] data)
        => Populate(target, data, context: null);

    /// <inheritdoc cref="Populate(IRecordable, ReadOnlySpan{byte}, ChronicleContext)"
    ///     path="/*[not(self::exception[@cref='T:System.ArgumentNullException'])]"/>
    /// <exception cref="ArgumentNullException"><paramref name="target"/> or <paramref name="data"/> is null.</exception>
    public static void Populate(IRecordable target, byte[] data, ChronicleContext? context)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        Populate(target, data.AsSpan(), context);
    }

    /// <inheritdoc cref="Populate(IRecordable, ReadOnlySpan{byte}, ChronicleContext)"/>
    public static void Populate(IRecordable target, ReadOnlySpan<byte> data)
        => Populate(target, data, context: null);

    /// <summary>
    /// Applies a MemoryPack record to an existing instance.
    /// </summary>
    /// <param name="target">The object to mutate. Initialize owned class instances needed by present deep records.</param>
    /// <param name="data">The nonempty payload to read. The serializer leaves the caller's buffer intact and retains no reference to it.</param>
    /// <param name="context">The destination session for link resolution. Null creates a fresh context.</param>
    /// <remarks>
    /// Runs the target schema, then calls <see cref="ChronicleContext.ResolveDeferredLinks"/>.
    /// A failure can leave earlier assignments applied. A supplied context retains pending callbacks.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="target"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="data"/> is empty.</exception>
    /// <exception cref="MemoryPackSerializationException">The MemoryPack decoder rejects the payload or a recorded value.</exception>
    /// <exception cref="InvalidOperationException">A present deep class lacks an initialized target, or link resolution fails.</exception>
    public static void Populate(IRecordable target, ReadOnlySpan<byte> data, ChronicleContext? context)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));
        if (data.IsEmpty)
            throw new ArgumentException("Serialized bytes must not be empty.", nameof(data));

        context ??= new ChronicleContext();

        var chronicler = new MemoryPackRecordReader(data, context);
        target.RecordData(chronicler);
        context.ResolveDeferredLinks();
    }

    private sealed class MemoryPackRecordWriter : IChronicler
    {
        private readonly OrderedStringMap<byte[]?> _entries = new(8, StringComparer.Ordinal);

        public MemoryPackRecordWriter(ChronicleContext context)
        {
            Context = context;
        }

        public ChronicleContext Context { get; }

        public SerializationMode Mode => SerializationMode.Saving;

        public void LookValue<T>(ref T value, string name, T? defaultValue = default)
        {
            if (value is null || EqualityComparer<T>.Default.Equals(value, defaultValue!))
                return;

            _entries[name] = MemoryPackSerializer.Serialize(value);
        }

        public void LookDeep<T>(ref T value, string name) where T : class, IRecordable
        {
            if (value == null)
            {
                _entries[name] = null;
                return;
            }

            var nested = new MemoryPackRecordWriter(Context);
            value.RecordData(nested);
            _entries[name] = nested.ToArray();
        }

        public void LookDeepStruct<T>(ref T value, string name) where T : struct, IRecordable
        {
            var nested = new MemoryPackRecordWriter(Context);
            value.RecordData(nested);
            _entries[name] = nested.ToArray();
        }

        public void LookNullableDeep<T>(ref T? value, string name) where T : struct, IRecordable
        {
            if (!value.HasValue)
                return;

            T nestedValue = value.Value;
            var nested = new MemoryPackRecordWriter(Context);
            nestedValue.RecordData(nested);
            _entries[name] = nested.ToArray();
        }

        public void LookLink<T>(
            ref T value,
            string name,
            string? slot = null,
            RecordLinkResolveMode resolveMode = RecordLinkResolveMode.Immediate,
            Action<T>? assignLoadedValue = null)
        {
            string? id = null;
            if (value is not null
                && !Context.Links.TryGetReferenceId(value, out id, slot))
            {
                throw new InvalidOperationException(
                    $"Unable to save link '{name}' of type {typeof(T).Name} because no stable id could be produced{FormatSlot(slot)}.");
            }

            _entries[name] = MemoryPackSerializer.Serialize(id);
        }

        public byte[] ToArray()
        {
            return MemoryPackSerializer.Serialize(MemoryPackRecordEnvelope.FromEntries(_entries));
        }
    }

    private sealed class MemoryPackRecordReader : IChronicler
    {
        private readonly OrderedStringMap<byte[]?> _entries;

        public MemoryPackRecordReader(ReadOnlySpan<byte> data, ChronicleContext context)
        {
            MemoryPackRecordEnvelope? envelope = MemoryPackRecordEnvelope.Deserialize(data);
            _entries = envelope == null
                ? new OrderedStringMap<byte[]?>(8, StringComparer.Ordinal)
                : envelope.ToEntryMap();
            Context = context;
        }

        public ChronicleContext Context { get; }

        public SerializationMode Mode => SerializationMode.Loading;

        public void LookValue<T>(ref T value, string name, T? defaultValue = default)
        {
            if (!_entries.TryGetValue(name, out byte[]? entry)
                || entry == null)
            {
                value = defaultValue!;
                return;
            }

            T? loadedValue = MemoryPackSerializer.Deserialize<T>(entry);
            if (loadedValue is null)
            {
                value = defaultValue!;
                return;
            }

            value = loadedValue;
        }

        public void LookDeep<T>(ref T value, string name) where T : class, IRecordable
        {
            if (!_entries.TryGetValue(name, out byte[]? entry)
                || entry == null)
                return;

            if (value == null)
                throw new InvalidOperationException(
                    $"Unable to load '{name}' because {typeof(T).Name} must already be instantiated for a deep chronicler load.");

            var nested = new MemoryPackRecordReader(entry, Context);
            value.RecordData(nested);
        }

        public void LookDeepStruct<T>(ref T value, string name) where T : struct, IRecordable
        {
            if (!_entries.TryGetValue(name, out byte[]? entry)
                || entry == null)
            {
                value = CreateDefaultDeepStruct<T>();
                return;
            }

            value = new();
            var nested = new MemoryPackRecordReader(entry, Context);
            value.RecordData(nested);
        }

        public void LookNullableDeep<T>(ref T? value, string name) where T : struct, IRecordable
        {
            if (!_entries.TryGetValue(name, out byte[]? entry)
                || entry == null)
            {
                value = null;
                return;
            }

            T nestedValue = new();
            var nested = new MemoryPackRecordReader(entry, Context);
            nestedValue.RecordData(nested);
            value = nestedValue;
        }

        public void LookLink<T>(
            ref T value,
            string name,
            string? slot = null,
            RecordLinkResolveMode resolveMode = RecordLinkResolveMode.Immediate,
            Action<T>? assignLoadedValue = null)
        {
            if (!TryReadLinkId(name, out string? id))
            {
                value = default!;
                assignLoadedValue?.Invoke(value);
                return;
            }

            if (resolveMode == RecordLinkResolveMode.Deferred)
            {
                LoadDeferredLink(ref value, name, id, slot, assignLoadedValue);
                return;
            }

            LoadImmediateLink(ref value, name, id, slot, assignLoadedValue);
        }

        private bool TryReadLinkId(string name, [NotNullWhen(true)] out string? id)
        {
            if (!_entries.TryGetValue(name, out byte[]? entry)
                || entry == null)
            {
                id = null;
                return false;
            }

            id = MemoryPackSerializer.Deserialize<string>(entry);
            return id != null;
        }

        private void LoadDeferredLink<T>(
            ref T value,
            string name,
            string id,
            string? slot,
            Action<T>? assignLoadedValue)
        {
            if (assignLoadedValue == null)
                throw new InvalidOperationException(
                    $"Deferred link '{name}' of type {typeof(T).Name} requires an assignment callback.");

            if (Context.Links.TryResolve(id, out T? deferredValue, slot))
            {
                value = deferredValue!;
                assignLoadedValue(deferredValue!);
                return;
            }

            Context.QueueDeferredLink(name, id, slot, assignLoadedValue);
            value = default!;
        }

        private void LoadImmediateLink<T>(
            ref T value,
            string name,
            string id,
            string? slot,
            Action<T>? assignLoadedValue)
        {
            if (!Context.Links.TryResolve(id, out T? resolvedValue, slot))
                throw new InvalidOperationException(
                    $"Unable to load link '{name}' of type {typeof(T).Name} with id '{id}'{FormatSlot(slot)}.");

            value = resolvedValue!;
            assignLoadedValue?.Invoke(resolvedValue!);
        }

        private T CreateDefaultDeepStruct<T>() where T : struct, IRecordable
        {
            T defaultValue = new();
            var nested = new MemoryPackRecordReader(EmptyRecordBytes, Context);
            defaultValue.RecordData(nested);
            return defaultValue;
        }
    }

    private static string FormatSlot(string? slot)
    {
        return string.IsNullOrEmpty(slot)
            ? string.Empty
            : $" in slot '{slot}'";
    }
}

#endif
