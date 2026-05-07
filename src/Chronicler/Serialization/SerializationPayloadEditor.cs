#if !CHRONICLER_DISABLE_MEMORYPACK
using MemoryPack;
#endif
using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Chronicler;

/// <summary>
/// Provides methods for editing serialized payloads without fully deserializing them.
/// </summary>
public static class SerializationPayloadEditor
{
    #region Common

#if CHRONICLER_DISABLE_MEMORYPACK

    /// <summary>
    /// Serializes a record to a payload format (JSON string or MemoryPack byte array) based on the specified options.
    /// </summary>
    /// <param name="record">The record to serialize.</param>
    /// <returns>The serialized payload.</returns>
    public static object SerializeRecord(IRecordable record)
    {
        return JsonRecordSerializer.Serialize(record, writeIndented: true);
    }

    /// <summary>
    /// Populates a record with data from a serialized payload (JSON string or MemoryPack byte array) based on the specified options.
    /// </summary>
    /// <param name="target">The record to populate.</param>
    /// <param name="payload">The serialized payload.</param>
    public static void PopulateRecord(IRecordable target, object payload)
    {
        JsonRecordSerializer.Populate(target, (string)payload);
    }

    /// <summary>
    /// Removes an entry from a serialized payload (JSON property or MemoryPack entry) at the specified path based on the specified options.
    /// </summary>
    /// <param name="payload">The serialized payload.</param>
    /// <param name="path">The path to the entry to remove.</param>
    /// <returns>The modified payload.</returns>
    public static object RemovePayloadEntry(object payload, params string[] path)
    {
        return RemoveJsonProperty((string)payload, path);
    }


    /// <summary>
    /// Sets a value in a serialized payload (JSON property or MemoryPack entry) at the specified path based on the specified options.
    /// </summary>
    /// <typeparam name="T">The type of the value to set.</typeparam>
    /// <param name="payload">The serialized payload.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="path">The path to the entry to set.</param>
    /// <returns>The modified payload.</returns>
    public static object SetPayloadValue<T>(
        object payload,
        T value,
        params string[] path)
    {
        return SetJsonValue((string)payload, value, path);
    }

#else

    /// <summary>
    /// Serializes a record to a payload format (JSON string or MemoryPack byte array) based on the specified options.
    /// </summary>
    /// <param name="record">The record to serialize.</param>
    /// <param name="useMemoryPack">Whether to use MemoryPack for serialization.</param>
    /// <returns>The serialized payload.</returns>
    public static object SerializeRecord(IRecordable record, bool useMemoryPack = true)
    {
        return useMemoryPack
            ? MemoryPackRecordSerializer.Serialize(record)
            : JsonRecordSerializer.Serialize(record, writeIndented: true);
    }

    /// <summary>
    /// Populates a record with data from a serialized payload (JSON string or MemoryPack byte array) based on the specified options.
    /// </summary>
    /// <param name="target">The record to populate.</param>
    /// <param name="payload">The serialized payload.</param>
    ///  <param name="useMemoryPack">Whether to use MemoryPack for deserialization.</param>
    public static void PopulateRecord(
        IRecordable target,
        object payload,
        bool useMemoryPack = true)
    {
        if (useMemoryPack)
        {
            MemoryPackRecordSerializer.Populate(target, (byte[])payload);
            return;
        }

        JsonRecordSerializer.Populate(target, (string)payload);
    }

    /// <summary>
    /// Removes an entry from a serialized payload (JSON property or MemoryPack entry) at the specified path based on the specified options.
    /// </summary>
    /// <param name="payload">The serialized payload.</param>
    /// <param name="useMemoryPack">Whether to use MemoryPack for deserialization.</param>
    /// <param name="path">The path to the entry to remove.</param>
    /// <returns>The modified payload.</returns>
    public static object RemovePayloadEntry(
        object payload,
        bool useMemoryPack = true,
        params string[] path)
    {
        return useMemoryPack
            ? RemoveMemoryPackEntry((byte[])payload, path)
            : RemoveJsonProperty((string)payload, path);
    }


    /// <summary>
    /// Sets a value in a serialized payload (JSON property or MemoryPack entry) at the specified path based on the specified options.
    /// </summary>
    /// <typeparam name="T">The type of the value to set.</typeparam>
    /// <param name="payload">The serialized payload.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="useMemoryPack">Whether to use MemoryPack for deserialization.</param>
    /// <param name="path">The path to the entry to set.</param>
    /// <returns>The modified payload.</returns>
    public static object SetPayloadValue<T>(
        object payload,
        T value,
        bool useMemoryPack = true,
        params string[] path)
    {
        return useMemoryPack
            ? SetMemoryPackValue((byte[])payload, value, path)
            : SetJsonValue((string)payload, value, path);
    }

#endif

    #endregion

    #region JSON Editing

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true
    };

    /// <summary>
    /// Removes a property from a JSON payload at the specified path.
    /// </summary>
    /// <param name="json">The JSON payload.</param>
    /// <param name="path">The path to the property to remove.</param>
    /// <returns>The modified JSON payload.</returns>
    /// <exception cref="ArgumentException"></exception>
    public static string RemoveJsonProperty(string json, params string[] path)
    {
        if (path == null || path.Length == 0)
            throw new ArgumentException("A JSON property path is required.", nameof(path));

        JsonObject root = ParseJsonRoot(json);
        JsonObject parent = GetJsonParent(root, path);
        parent.Remove(path[^1]);
        return root.ToJsonString(JsonOptions);
    }

    /// <summary>
    /// Sets a property value in a JSON payload at the specified path.
    /// </summary>
    /// <typeparam name="T">The type of the value to set.</typeparam>
    /// <param name="json">The JSON payload.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="path">The path to the property to set.</param>
    /// <returns>The modified JSON payload.</returns>
    /// <exception cref="ArgumentException"></exception>
    public static string SetJsonValue<T>(string json, T value, params string[] path)
    {
        if (path == null || path.Length == 0)
            throw new ArgumentException("A JSON property path is required.", nameof(path));

        JsonObject root = ParseJsonRoot(json);
        JsonObject parent = GetJsonParent(root, path);
        parent[path[^1]] = JsonSerializer.SerializeToNode(value, JsonOptions);
        return root.ToJsonString(JsonOptions);
    }

    private static JsonObject ParseJsonRoot(string json)
    {
        JsonNode rootNode = JsonNode.Parse(json)
            ?? throw new InvalidOperationException("Unable to parse JSON payload.");
        return rootNode as JsonObject
            ?? throw new InvalidOperationException("Expected JSON root object.");
    }

    private static JsonObject GetJsonParent(JsonObject root, string[] path)
    {
        JsonObject current = root;
        for (int i = 0; i < path.Length - 1; i++)
        {
            current = current[path[i]] as JsonObject
                ?? throw new InvalidOperationException(
                    $"Expected JSON object at path segment '{path[i]}'.");
        }

        return current;
    }

    #endregion

#if !CHRONICLER_DISABLE_MEMORYPACK

    #region MemoryPack Editing

    /// <summary>
    /// Removes a MemoryPack entry from a serialized payload at the specified path.
    /// </summary>
    /// <param name="data">The serialized MemoryPack payload.</param>
    /// <param name="path">The path to the entry to remove.</param>
    /// <returns>The modified serialized MemoryPack payload.</returns>
    /// <exception cref="ArgumentException"></exception>
    public static byte[] RemoveMemoryPackEntry(byte[] data, params string[] path)
    {
        if (path == null || path.Length == 0)
            throw new ArgumentException("A MemoryPack entry path is required.", nameof(path));

        MemoryPackRecordEnvelope envelope = ReadEnvelope(data);
        RemoveMemoryPackEntry(envelope, path, 0);
        return MemoryPackSerializer.Serialize(envelope);
    }

    /// <summary>
    /// Sets a MemoryPack entry value in a serialized payload at the specified path.
    /// </summary>
    /// <typeparam name="T">The type of the value to set.</typeparam>
    /// <param name="data">The serialized MemoryPack payload.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="path">The path to the entry to set.</param>
    /// <returns>The modified serialized MemoryPack payload.</returns>
    /// <exception cref="ArgumentException"></exception>
    public static byte[] SetMemoryPackValue<T>(byte[] data, T value, params string[] path)
    {
        if (path == null || path.Length == 0)
            throw new ArgumentException("A MemoryPack entry path is required.", nameof(path));

        MemoryPackRecordEnvelope envelope = ReadEnvelope(data);
        SetMemoryPackValue(envelope, path, 0, MemoryPackSerializer.Serialize(value));
        return MemoryPackSerializer.Serialize(envelope);
    }

    private static MemoryPackRecordEnvelope ReadEnvelope(byte[] data)
    {
        return MemoryPackSerializer.Deserialize<MemoryPackRecordEnvelope>(data)
            ?? new MemoryPackRecordEnvelope();
    }

    private static void RemoveMemoryPackEntry(MemoryPackRecordEnvelope envelope, string[] path, int depth)
    {
        if (depth == path.Length - 1)
        {
            envelope.Entries.Remove(path[depth]);
            return;
        }

        if (!envelope.Entries.TryGetValue(path[depth], out byte[]? nestedData)
            || nestedData == null)
        {
            throw new InvalidOperationException(
                $"Unable to locate MemoryPack entry '{path[depth]}' at depth {depth}.");
        }

        MemoryPackRecordEnvelope nestedEnvelope = ReadEnvelope(nestedData);
        RemoveMemoryPackEntry(nestedEnvelope, path, depth + 1);
        envelope.Entries[path[depth]] = MemoryPackSerializer.Serialize(nestedEnvelope);
    }

    private static void SetMemoryPackValue(
        MemoryPackRecordEnvelope envelope,
        string[] path,
        int depth,
        byte[] serializedValue)
    {
        if (depth == path.Length - 1)
        {
            envelope.Entries[path[depth]] = serializedValue;
            return;
        }

        if (!envelope.Entries.TryGetValue(path[depth], out byte[]? nestedData)
            || nestedData == null)
        {
            throw new InvalidOperationException(
                $"Unable to locate MemoryPack entry '{path[depth]}' at depth {depth}.");
        }

        MemoryPackRecordEnvelope nestedEnvelope = ReadEnvelope(nestedData);
        SetMemoryPackValue(nestedEnvelope, path, depth + 1, serializedValue);
        envelope.Entries[path[depth]] = MemoryPackSerializer.Serialize(nestedEnvelope);
    }

    #endregion

#endif
}
