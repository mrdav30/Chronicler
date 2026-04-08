using FluentAssertions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Chronicler.Tests;

public class RecordValueSerializationTests
{
    [Fact]
    public void JsonSerialize_ShouldOmitCanonicalDefaultValueEntries()
    {
        var source = new ValueRecord();

        string json = JsonRecordSerializer.Serialize(source);

        json.Should().Be("{}");
    }

    [Theory]
    [MemberData(nameof(SerializationTransportData.All), MemberType = typeof(SerializationTransportData))]
    public void Populate_ShouldApplyCanonicalDefaults_WhenValueEntriesAreMissing(SerializationTransport transport)
    {
        var source = new ValueRecord
        {
            Count = 42,
            Enabled = false,
            Alias = "mage"
        };

        object payload = SerializationTestHarness.Serialize(source, transport);
        payload = SerializationTestHarness.RemoveEntry(payload, transport, "count");
        payload = SerializationTestHarness.RemoveEntry(payload, transport, "enabled");
        payload = SerializationTestHarness.RemoveEntry(payload, transport, "alias");

        var target = new ValueRecord
        {
            Count = 99,
            Enabled = false,
            Alias = "keep-me"
        };

        SerializationTestHarness.Populate(target, payload, transport);

        target.Count.Should().Be(ValueRecord.DefaultCount);
        target.Enabled.Should().BeTrue();
        target.Alias.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(SerializationTransportData.All), MemberType = typeof(SerializationTransportData))]
    public void Populate_ShouldApplyDeclaredNullDefault_WhenEntryIsExplicitNull(SerializationTransport transport)
    {
        var source = new ValueRecord
        {
            Alias = "present"
        };

        object payload = SerializationTestHarness.Serialize(source, transport);
        payload = SerializationTestHarness.SetValue<string?>(payload, transport, null, "alias");

        var target = new ValueRecord
        {
            Alias = "keep-me"
        };

        SerializationTestHarness.Populate(target, payload, transport);

        target.Alias.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(SerializationTransportData.All), MemberType = typeof(SerializationTransportData))]
    public void RoundTrip_ShouldPreserveNonDefaultValues(SerializationTransport transport)
    {
        var source = new ValueRecord
        {
            Count = 42,
            Enabled = false,
            Alias = "mage"
        };

        object payload = SerializationTestHarness.Serialize(source, transport);

        var target = new ValueRecord();
        SerializationTestHarness.Populate(target, payload, transport);

        target.Count.Should().Be(42);
        target.Enabled.Should().BeFalse();
        target.Alias.Should().Be("mage");
    }

    [Fact]
    public void JsonPopulate_ShouldFallbackToDefault_WhenCustomLeafConverterReturnsNull()
    {
        var source = new ConverterBackedValueRecord
        {
            Value = new ConverterBackedLeaf { Raw = "present" }
        };

        string payload = JsonRecordSerializer.Serialize(source);

        var target = new ConverterBackedValueRecord
        {
            Value = new ConverterBackedLeaf { Raw = "keep-me" }
        };

        JsonRecordSerializer.Populate(target, payload);

        target.Value.Should().NotBeNull();
        target.Value!.Raw.Should().Be("fallback");
    }

    private sealed class ValueRecord : IRecordable
    {
        public const int DefaultCount = 7;

        public int Count = DefaultCount;

        public bool Enabled = true;

        public string? Alias;

        public void RecordData(IChronicler chronicler)
        {
            RecordValues.Look(chronicler, ref Count, "count", DefaultCount);
            RecordValues.Look(chronicler, ref Enabled, "enabled", true);
            RecordValues.Look(chronicler, ref Alias, "alias", defaultValue: null);
        }
    }

    private sealed class ConverterBackedValueRecord : IRecordable
    {
        public ConverterBackedLeaf? Value = new() { Raw = "fallback" };

        public void RecordData(IChronicler chronicler)
        {
            RecordValues.Look(
                chronicler,
                ref Value,
                "value",
                defaultValue: new ConverterBackedLeaf { Raw = "fallback" });
        }
    }

    [JsonConverter(typeof(NullReturningLeafConverter))]
    private sealed class ConverterBackedLeaf
    {
        public string Raw { get; init; } = string.Empty;
    }

    private sealed class NullReturningLeafConverter : JsonConverter<ConverterBackedLeaf>
    {
        public override ConverterBackedLeaf? Read(
            ref Utf8JsonReader reader,
            System.Type typeToConvert,
            JsonSerializerOptions options)
        {
            reader.Skip();
            return null;
        }

        public override void Write(
            Utf8JsonWriter writer,
            ConverterBackedLeaf value,
            JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("raw", value.Raw);
            writer.WriteEndObject();
        }
    }
}
