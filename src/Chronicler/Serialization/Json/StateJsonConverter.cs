//=======================================================================
// StateJsonConverter.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chronicler.Serialization;

/// <summary>
/// Serializes and deserializes an object through its canonical state value.
/// </summary>
/// <typeparam name="TRecord">The state-backed record type.</typeparam>
/// <typeparam name="TState">The serializable state type.</typeparam>
public sealed class StateJsonConverter<TRecord, TState> : JsonConverter<TRecord>
    where TRecord : class, IStateBacked<TState>
{
    private const string StatePropertyName = "State";

    private readonly Func<TState, TRecord> _factory;

    /// <summary>Creates a converter that reconstructs records from their state.</summary>
    /// <param name="factory">The retained callback that reconstructs a record from a deserialized state value.</param>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/> is null.</exception>
    public StateJsonConverter(Func<TState, TRecord> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>Reads a state object and reconstructs the record.</summary>
    /// <param name="reader">A reader at the start of the object. On success, it is positioned on the closing object token.</param>
    /// <param name="typeToConvert">The requested record type, used in shape errors.</param>
    /// <param name="options">The serializer options used to decode the nested state.</param>
    /// <returns>The result of invoking the factory with the decoded state.</returns>
    /// <exception cref="JsonException">The object does not contain exactly one case-sensitive <c>State</c> property, or the state cannot be decoded to a non-null value.</exception>
    public override TRecord Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        EnsureStartObject(reader.TokenType, typeToConvert);
        ReadStateProperty(ref reader, typeToConvert);
        TState state = ReadStateValue(ref reader, typeToConvert, options);
        EnsureEndObject(ref reader, typeToConvert);

        return _factory(state);
    }

    private static void EnsureStartObject(JsonTokenType tokenType, Type typeToConvert)
    {
        if (tokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected JSON object for state-backed type '{typeToConvert}'.");
    }

    private static void ReadStateProperty(ref Utf8JsonReader reader, Type typeToConvert)
    {
        if (!reader.Read())
            throw new JsonException($"Expected '{StatePropertyName}' property for state-backed type '{typeToConvert}'.");
        if (reader.TokenType != JsonTokenType.PropertyName || !reader.ValueTextEquals(StatePropertyName))
            throw new JsonException($"Expected '{StatePropertyName}' property for state-backed type '{typeToConvert}'.");
    }

    private static TState ReadStateValue(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (!reader.Read())
            throw new JsonException($"Expected '{StatePropertyName}' value for state-backed type '{typeToConvert}'.");

        TState? state = JsonSerializer.Deserialize<TState>(ref reader, options);
        if (state == null)
            throw new JsonException($"Unable to deserialize '{StatePropertyName}' for state-backed type '{typeToConvert}'.");

        return state;
    }

    private static void EnsureEndObject(ref Utf8JsonReader reader, Type typeToConvert)
    {
        if (!reader.Read())
            throw new JsonException($"Expected end of JSON object for state-backed type '{typeToConvert}'.");
        if (reader.TokenType != JsonTokenType.EndObject)
            throw new JsonException($"Expected end of JSON object for state-backed type '{typeToConvert}'.");
    }

    /// <summary>Writes the record as an object with one <c>State</c> property.</summary>
    /// <param name="writer">The caller's JSON writer. Appends one value at its current position.</param>
    /// <param name="value">The object whose state is written. A null record writes a JSON null.</param>
    /// <param name="options">The serializer options used to encode the nested state.</param>
    public override void Write(Utf8JsonWriter writer, TRecord value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();
        writer.WritePropertyName(StatePropertyName);
        JsonSerializer.Serialize(writer, value.State, options);
        writer.WriteEndObject();
    }
}
