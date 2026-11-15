//=======================================================================
// SerializationPayloadEditor.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

#if !CHRONICLER_DISABLE_MEMORYPACK
using MemoryPack;
#endif
using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Chronicler.Serialization;

/// <summary>
/// Edits JSON properties and MemoryPack entries without populating a recordable object graph.
/// </summary>
/// <remarks>
/// Paths and replacement values must match the recorded schema. Replacement values use the
/// selected transport serializer directly; editing does not invoke <see cref="IRecordable.RecordData"/>.
/// Use the format-specific record serializers to serialize and populate records.
/// A serialized null MemoryPack envelope is treated as an empty record.
/// </remarks>
public static class SerializationPayloadEditor
{
    #region JSON Editing

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true
    };

    /// <summary>Removes a recorded JSON property if it exists.</summary>
    /// <param name="json">A payload whose root is a JSON object.</param>
    /// <param name="path">Ordered property names from the root to the target. Each intermediate name must identify a nested object.</param>
    /// <returns>The complete edited object serialized as JSON.</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> is null or has no segments.</exception>
    /// <exception cref="InvalidOperationException">The root or an intermediate node is missing or is not a JSON object.</exception>
    /// <exception cref="JsonException">The JSON codec rejects the payload.</exception>
    public static string RemoveJsonProperty(string json, params string[] path)
    {
        if (path == null || path.Length == 0)
            throw new ArgumentException("A JSON property path is required.", nameof(path));

        JsonObject root = ParseJsonRoot(json);
        JsonObject parent = GetJsonParent(root, path);
        parent.Remove(path[^1]);
        return root.ToJsonString(JsonOptions);
    }

    /// <summary>Adds or replaces a recorded JSON property.</summary>
    /// <typeparam name="T">The declared type used to encode the replacement.</typeparam>
    /// <param name="json">A payload whose root is a JSON object.</param>
    /// <param name="value">The replacement to encode with the transport's value codec.</param>
    /// <param name="path">Ordered property names from the root to the target. Each intermediate name must identify a nested object.</param>
    /// <returns>The complete edited object serialized as JSON.</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> is null or has no segments.</exception>
    /// <exception cref="InvalidOperationException">The root or an intermediate node is missing or is not a JSON object.</exception>
    /// <exception cref="JsonException">The JSON codec rejects the payload or replacement value.</exception>
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

    /// <summary>Removes a recorded MemoryPack entry if it exists.</summary>
    /// <param name="data">The source record bytes. The input buffer is read as-is and remains caller-owned.</param>
    /// <param name="path">Ordered entry names from the root to the target. Each intermediate name must identify a nested record.</param>
    /// <returns>The complete edited record in a new caller-owned array.</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> is null or has no segments.</exception>
    /// <exception cref="InvalidOperationException">An intermediate entry is missing or null.</exception>
    /// <exception cref="MemoryPackSerializationException">The MemoryPack codec rejects the payload.</exception>
    public static byte[] RemoveMemoryPackEntry(byte[] data, params string[] path)
    {
        if (path == null || path.Length == 0)
            throw new ArgumentException("A MemoryPack entry path is required.", nameof(path));

        MemoryPackRecordEnvelope envelope = ReadEnvelope(data);
        RemoveMemoryPackEntry(envelope, path, 0);
        return MemoryPackSerializer.Serialize(envelope);
    }

    /// <summary>Adds or replaces a recorded MemoryPack entry.</summary>
    /// <typeparam name="T">The declared type used to encode the replacement.</typeparam>
    /// <param name="data">The source record bytes. The input buffer is read as-is and remains caller-owned.</param>
    /// <param name="value">The replacement to encode with the transport's value codec.</param>
    /// <param name="path">Ordered entry names from the root to the target. Each intermediate name must identify a nested record.</param>
    /// <returns>The complete edited record in a new caller-owned array.</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> is null or has no segments.</exception>
    /// <exception cref="InvalidOperationException">An intermediate entry is missing or null.</exception>
    /// <exception cref="MemoryPackSerializationException">The MemoryPack codec rejects the payload or replacement value.</exception>
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
        return MemoryPackRecordEnvelope.Deserialize(data)
            ?? new MemoryPackRecordEnvelope();
    }

    private static void RemoveMemoryPackEntry(MemoryPackRecordEnvelope envelope, string[] path, int depth)
    {
        if (depth == path.Length - 1)
        {
            envelope.RemoveEntry(path[depth]);
            return;
        }

        if (!envelope.TryGetEntry(path[depth], out byte[]? nestedData)
            || nestedData == null)
        {
            throw new InvalidOperationException(
                $"Unable to locate MemoryPack entry '{path[depth]}' at depth {depth}.");
        }

        MemoryPackRecordEnvelope nestedEnvelope = ReadEnvelope(nestedData);
        RemoveMemoryPackEntry(nestedEnvelope, path, depth + 1);
        envelope.SetEntry(path[depth], MemoryPackSerializer.Serialize(nestedEnvelope));
    }

    private static void SetMemoryPackValue(
        MemoryPackRecordEnvelope envelope,
        string[] path,
        int depth,
        byte[] serializedValue)
    {
        if (depth == path.Length - 1)
        {
            envelope.SetEntry(path[depth], serializedValue);
            return;
        }

        if (!envelope.TryGetEntry(path[depth], out byte[]? nestedData)
            || nestedData == null)
        {
            throw new InvalidOperationException(
                $"Unable to locate MemoryPack entry '{path[depth]}' at depth {depth}.");
        }

        MemoryPackRecordEnvelope nestedEnvelope = ReadEnvelope(nestedData);
        SetMemoryPackValue(nestedEnvelope, path, depth + 1, serializedValue);
        envelope.SetEntry(path[depth], MemoryPackSerializer.Serialize(nestedEnvelope));
    }

    #endregion

#endif
}
