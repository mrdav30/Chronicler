using MemoryPack;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Chronicler;

/// <summary>
/// Provides methods for editing serialized payloads without fully deserializing them.
/// </summary>
public static class SerializationPayloadEditor
{
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
}
