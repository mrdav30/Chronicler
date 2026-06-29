using FluentAssertions;
using System;
using Xunit;

namespace Chronicler.Tests;

public class SerializationPayloadEditorTests
{
#if !CHRONICLER_DISABLE_MEMORYPACK
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WrapperMethods_ShouldRoundTripAndEditPayloadUsingSelectedTransport(bool useMemoryPack)
    {
        var source = new PayloadRoot
        {
            State = new PayloadState
            {
                Count = 12,
                Enabled = false
            }
        };

        object payload = SerializationPayloadEditor.SerializeRecord(source, useMemoryPack);
        payload = SerializationPayloadEditor.SetPayloadValue(payload, 42, useMemoryPack, "state", nameof(PayloadState.Count));
        payload = SerializationPayloadEditor.RemovePayloadEntry(payload, useMemoryPack, "state", nameof(PayloadState.Enabled));

        var target = new PayloadRoot();
        SerializationPayloadEditor.PopulateRecord(target, payload, useMemoryPack);

        target.State.Count.Should().Be(42);
        target.State.Enabled.Should().BeTrue();
    }
#endif

    [Theory]
    [MemberData(nameof(SerializationTransportData.All), MemberType = typeof(SerializationTransportData))]
    public void SetValue_ShouldMutateNestedValue(SerializationTransport transport)
    {
        var source = new PayloadRoot
        {
            State = new PayloadState
            {
                Count = 12,
                Enabled = false
            }
        };

        object payload = SerializationTestHarness.Serialize(source, transport);
        payload = SerializationTestHarness.SetValue(payload, transport, 42, "state", nameof(PayloadState.Count));

        var target = new PayloadRoot();
        SerializationTestHarness.Populate(target, payload, transport);

        target.State.Count.Should().Be(42);
        target.State.Enabled.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(SerializationTransportData.All), MemberType = typeof(SerializationTransportData))]
    public void RemoveEntry_ShouldRemoveNestedValueAndFallbackToDefault(SerializationTransport transport)
    {
        var source = new PayloadRoot
        {
            State = new PayloadState
            {
                Count = 12,
                Enabled = false
            }
        };

        object payload = SerializationTestHarness.Serialize(source, transport);
        payload = SerializationTestHarness.RemoveEntry(payload, transport, "state", nameof(PayloadState.Count));

        var target = new PayloadRoot();
        SerializationTestHarness.Populate(target, payload, transport);

        target.State.Count.Should().Be(PayloadState.DefaultCount);
        target.State.Enabled.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(SerializationTransportData.All), MemberType = typeof(SerializationTransportData))]
    public void SetValue_ShouldThrow_WhenIntermediatePathIsMissing(SerializationTransport transport)
    {
        var source = new PayloadRoot
        {
            State = new PayloadState
            {
                Count = 12,
                Enabled = false
            }
        };

        object payload = SerializationTestHarness.Serialize(source, transport);
        payload = SerializationTestHarness.RemoveEntry(payload, transport, "state");

        Action act = () => SerializationTestHarness.SetValue(payload, transport, 42, "state", nameof(PayloadState.Count));

        var assertions = act.Should().Throw<InvalidOperationException>();
        if (transport == SerializationTransport.Json)
        {
            assertions.WithMessage("*Expected JSON object at path segment 'state'.*");
            return;
        }

        assertions.WithMessage("*Unable to locate*state*");
    }

    private sealed class PayloadRoot : IRecordable
    {
        public PayloadState State = new();

        public void RecordData(IChronicler chronicler)
        {
            RecordDeep.Look(chronicler, ref State, "state");
        }
    }

    private sealed class PayloadState : IRecordable
    {
        public const int DefaultCount = 7;

        public int Count = DefaultCount;

        public bool Enabled = true;

        public void RecordData(IChronicler chronicler)
        {
            RecordValues.Look(chronicler, ref Count, nameof(Count), DefaultCount);
            RecordValues.Look(chronicler, ref Enabled, nameof(Enabled), true);
        }
    }
}
