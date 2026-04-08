using FluentAssertions;
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
}
