using FluentAssertions;
using System;
using Xunit;

namespace Chronicler.Tests;

public class RecordDeepSerializationTests
{
    [Theory]
    [MemberData(nameof(SerializationTransportData.All), MemberType = typeof(SerializationTransportData))]
    public void Populate_ShouldApplyCanonicalDefaults_WhenStructDeepEntryIsMissing(SerializationTransport transport)
    {
        var source = new DeepStructContainer
        {
            State = new TestRecordableStruct { Count = 12, Enabled = false }
        };

        object payload = SerializationTestHarness.Serialize(source, transport);
        payload = SerializationTestHarness.RemoveEntry(payload, transport, "state");

        var target = new DeepStructContainer
        {
            State = new TestRecordableStruct { Count = 99, Enabled = false }
        };

        SerializationTestHarness.Populate(target, payload, transport);

        target.State.Count.Should().Be(TestRecordableStruct.DefaultCount);
        target.State.Enabled.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(SerializationTransportData.All), MemberType = typeof(SerializationTransportData))]
    public void RoundTrip_ShouldRestoreCanonicalDefaults_WhenStructDeepPayloadIsPresentButEmpty(SerializationTransport transport)
    {
        var source = new DeepStructContainer
        {
            State = new TestRecordableStruct
            {
                Count = TestRecordableStruct.DefaultCount,
                Enabled = true
            }
        };

        object payload = SerializationTestHarness.Serialize(source, transport);

        var target = new DeepStructContainer
        {
            State = new TestRecordableStruct { Count = 99, Enabled = false }
        };

        SerializationTestHarness.Populate(target, payload, transport);

        target.State.Count.Should().Be(TestRecordableStruct.DefaultCount);
        target.State.Enabled.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(SerializationTransportData.All), MemberType = typeof(SerializationTransportData))]
    public void Populate_ShouldClearNullableStruct_WhenNullableDeepEntryIsMissing(SerializationTransport transport)
    {
        var source = new DeepStructContainer
        {
            OptionalState = new TestRecordableStruct { Count = 12, Enabled = false }
        };

        object payload = SerializationTestHarness.Serialize(source, transport);
        payload = SerializationTestHarness.RemoveEntry(payload, transport, "optionalState");

        var target = new DeepStructContainer
        {
            OptionalState = new TestRecordableStruct { Count = 99, Enabled = false }
        };

        SerializationTestHarness.Populate(target, payload, transport);

        target.OptionalState.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(SerializationTransportData.All), MemberType = typeof(SerializationTransportData))]
    public void RoundTrip_ShouldRestoreNullableStruct_WhenPayloadEntryIsPresentEvenIfInnerValuesAreDefault(SerializationTransport transport)
    {
        var source = new DeepStructContainer
        {
            OptionalState = new TestRecordableStruct
            {
                Count = TestRecordableStruct.DefaultCount,
                Enabled = true
            }
        };

        object payload = SerializationTestHarness.Serialize(source, transport);

        var target = new DeepStructContainer();
        SerializationTestHarness.Populate(target, payload, transport);

        target.OptionalState.Should().NotBeNull();
        target.OptionalState!.Value.Count.Should().Be(TestRecordableStruct.DefaultCount);
        target.OptionalState!.Value.Enabled.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(SerializationTransportData.All), MemberType = typeof(SerializationTransportData))]
    public void RoundTrip_ShouldPopulateExistingReferenceDeepObject(SerializationTransport transport)
    {
        var source = new DeepObjectContainer
        {
            Child = new NestedObjectRecord
            {
                Level = 5,
                Name = "alpha"
            }
        };

        object payload = SerializationTestHarness.Serialize(source, transport);

        var target = new DeepObjectContainer
        {
            Child = new NestedObjectRecord
            {
                Level = 99,
                Name = "placeholder"
            }
        };

        SerializationTestHarness.Populate(target, payload, transport);

        target.Child.Should().NotBeNull();
        target.Child!.Level.Should().Be(5);
        target.Child.Name.Should().Be("alpha");
    }

    [Theory]
    [MemberData(nameof(SerializationTransportData.All), MemberType = typeof(SerializationTransportData))]
    public void Populate_ShouldThrow_WhenReferenceDeepTargetIsMissing(SerializationTransport transport)
    {
        var source = new DeepObjectContainer
        {
            Child = new NestedObjectRecord
            {
                Level = 5,
                Name = "alpha"
            }
        };

        object payload = SerializationTestHarness.Serialize(source, transport);

        var target = new DeepObjectContainer
        {
            Child = null
        };

        Action act = () => SerializationTestHarness.Populate(target, payload, transport);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must already be instantiated for a deep chronicler load*");
    }

    private sealed class DeepObjectContainer : IRecordable
    {
        public NestedObjectRecord? Child = new();

        public void RecordData(IChronicler chronicler)
        {
            NestedObjectRecord child = Child!;
            RecordDeep.Look(chronicler, ref child, "child");

            if (chronicler.Mode == SerializationMode.Loading)
                Child = child;
        }
    }

    private sealed class NestedObjectRecord : IRecordable
    {
        public int Level = 1;

        public string Name = string.Empty;

        public void RecordData(IChronicler chronicler)
        {
            RecordValues.Look(chronicler, ref Level, "level", 1);
            RecordValues.Look(chronicler, ref Name, "name", string.Empty);
        }
    }

    private sealed class DeepStructContainer : IRecordable
    {
        public TestRecordableStruct State;

        public TestRecordableStruct? OptionalState;

        public void RecordData(IChronicler chronicler)
        {
            RecordDeepStruct.Look(chronicler, ref State, "state");
            RecordNullableDeep.Look(chronicler, ref OptionalState, "optionalState");
        }
    }

    private struct TestRecordableStruct : IRecordable
    {
        public const int DefaultCount = 7;

        public int Count;

        public bool Enabled;

        public void RecordData(IChronicler chronicler)
        {
            chronicler.LookValue(ref Count, nameof(Count), DefaultCount);
            chronicler.LookValue(ref Enabled, nameof(Enabled), true);
        }
    }
}
