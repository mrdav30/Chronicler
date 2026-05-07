using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chronicler;

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

    /// <summary>
    /// Creates a converter that restores records from serialized state using the supplied factory.
    /// </summary>
    public StateJsonConverter(Func<TState, TRecord> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <inheritdoc />
    public override TRecord Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected JSON object for state-backed type '{typeToConvert}'.");

        if (!reader.Read()
            || reader.TokenType != JsonTokenType.PropertyName
            || !reader.ValueTextEquals(StatePropertyName))
        {
            throw new JsonException($"Expected '{StatePropertyName}' property for state-backed type '{typeToConvert}'.");
        }

        if (!reader.Read())
            throw new JsonException($"Expected '{StatePropertyName}' value for state-backed type '{typeToConvert}'.");

        TState? state = JsonSerializer.Deserialize<TState>(ref reader, options);
        if (state == null)
            throw new JsonException($"Unable to deserialize '{StatePropertyName}' for state-backed type '{typeToConvert}'.");

        if (!reader.Read() || reader.TokenType != JsonTokenType.EndObject)
            throw new JsonException($"Expected end of JSON object for state-backed type '{typeToConvert}'.");

        return _factory(state);
    }

    /// <inheritdoc />
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
