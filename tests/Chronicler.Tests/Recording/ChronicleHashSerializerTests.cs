using FluentAssertions;
using System;
using Xunit;

namespace Chronicler.Tests;

public sealed class ChronicleHashSerializerTests
{
    [Theory]
    [MemberData(nameof(SerializationTransportData.All), MemberType = typeof(SerializationTransportData))]
    public void Compute_ShouldMatchEquivalentRecordDataAfterTransportRoundTrip(SerializationTransport transport)
    {
        var source = new RecordGraph
        {
            Count = 42,
            Alias = "mage",
            Child = new ChildRecord { Level = 5 },
            State = new StateRecord { Enabled = true },
            OptionalState = new StateRecord { Enabled = false }
        };

        object payload = SerializationTestHarness.Serialize(source, transport);
        var target = new RecordGraph
        {
            Child = new ChildRecord()
        };

        SerializationTestHarness.Populate(target, payload, transport);

        ChronicleHashSerializer.Compute(target).Should().Be(ChronicleHashSerializer.Compute(source));
    }

    [Fact]
    public void Compute_ShouldMatchStableGoldenVector()
    {
        var source = new GoldenRecord
        {
            Count = 42,
            Alias = "mage"
        };

        ChronicleHashSerializer.Compute(source).ToString()
            .Should().Be("a4736ffd6ca10cef8f69b781360bd39f");
    }

    [Fact]
    public void Compute_ShouldChangeWhenFieldOrderChanges()
    {
        var normal = new OrderedRecord(reverseOrder: false);
        var reversed = new OrderedRecord(reverseOrder: true);

        ChronicleHashSerializer.Compute(reversed).Should().NotBe(ChronicleHashSerializer.Compute(normal));
    }

    [Fact]
    public void Compute_ShouldChangeWhenFieldNameChanges()
    {
        var standard = new FieldNameRecord(useAlternateName: false);
        var renamed = new FieldNameRecord(useAlternateName: true);

        ChronicleHashSerializer.Compute(renamed).Should().NotBe(ChronicleHashSerializer.Compute(standard));
    }

    [Fact]
    public void Compute_ShouldChangeWhenDeclaredDefaultChanges()
    {
        var first = new DefaultRecord(declaredDefault: 7);
        var second = new DefaultRecord(declaredDefault: 8);

        ChronicleHashSerializer.Compute(second).Should().NotBe(ChronicleHashSerializer.Compute(first));
    }

    [Fact]
    public void Compute_ShouldChangeWhenNestedValueChanges()
    {
        var first = new RecordGraph
        {
            Count = 42,
            Alias = "mage",
            Child = new ChildRecord { Level = 5 },
            State = new StateRecord { Enabled = true }
        };
        var second = new RecordGraph
        {
            Count = 42,
            Alias = "mage",
            Child = new ChildRecord { Level = 6 },
            State = new StateRecord { Enabled = true }
        };

        ChronicleHashSerializer.Compute(second).Should().NotBe(ChronicleHashSerializer.Compute(first));
    }

    [Fact]
    public void Compute_ShouldChangeWhenNullableDeepPresenceChanges()
    {
        var absent = new RecordGraph
        {
            Count = 42,
            Alias = "mage",
            Child = new ChildRecord { Level = 5 },
            State = new StateRecord { Enabled = true },
            OptionalState = null
        };
        var present = new RecordGraph
        {
            Count = 42,
            Alias = "mage",
            Child = new ChildRecord { Level = 5 },
            State = new StateRecord { Enabled = true },
            OptionalState = new StateRecord { Enabled = true }
        };

        ChronicleHashSerializer.Compute(present).Should().NotBe(ChronicleHashSerializer.Compute(absent));
    }

    [Fact]
    public void Compute_ShouldHashStableLinkIdsAndSlots()
    {
        var firstResource = new LinkResource();
        var secondResource = new LinkResource();
        ChronicleContext context = CreateLinkContext(firstResource, secondResource);

        var first = new LinkRecord(firstResource);
        var second = new LinkRecord(secondResource);

        ChronicleHashSerializer.Compute(second, context).Should().NotBe(ChronicleHashSerializer.Compute(first, context));
    }

    [Fact]
    public void Compute_ShouldThrowWhenNonNullLinkCannotResolveToStableId()
    {
        var record = new LinkRecord(new LinkResource());

        Action act = () => ChronicleHashSerializer.Compute(record, new ChronicleContext());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unable to hash link 'resource'*stable id*");
    }

    [Fact]
    public void Compute_ShouldThrowWhenLeafValueTypeIsUnsupported()
    {
        var record = new UnsupportedLeafRecord();

        Action act = () => ChronicleHashSerializer.Compute(record);

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*Unsupported record-hash leaf value 'temperature'*System.Double*");
    }

    [Fact]
    public void Contribute_ShouldEmbedRecordablePayloadInCallerOwnedWriter()
    {
        var record = new OrderedRecord(reverseOrder: false);

        var first = new ChronicleHashWriter();
        first.WriteSection("domain", 1);
        ChronicleHashSerializer.Contribute(record, ref first);
        first.WriteInt32(9);

        var second = new ChronicleHashWriter();
        second.WriteSection("domain", 1);
        ChronicleHashSerializer.Contribute(record, ref second);
        second.WriteInt32(9);

        second.ToHash().Should().Be(first.ToHash());
    }

    [Fact]
    public void Compute_ShouldUseRuntimeIndependentGenericTypeNames()
    {
        var record = new GenericRecord<int>();

        var expected = new ChronicleHashWriter();
        expected.WriteSection("chronicler.hash", 1);
        expected.WriteSection("chronicler.record", 1);
        expected.WriteString("Chronicler.Tests.ChronicleHashSerializerTests+GenericRecord`1<System.Int32>");
        expected.WriteString("value");
        expected.WriteByte(1);
        expected.WriteString("System.Int32");
        expected.WriteByte(6);
        expected.WriteBool(true);
        expected.WriteInt32(7);
        expected.WriteBool(true);
        expected.WriteInt32(0);
        expected.WriteSection("chronicler.record.end", 1);

        ChronicleHashSerializer.Compute(record).Should().Be(expected.ToHash());
    }


    [Fact]
    public void Compute_WithProvidedContext_ShouldNotAllocateAfterWarmup()
    {
        var record = new RecordGraph
        {
            Count = 42,
            Alias = "mage",
            Child = new ChildRecord { Level = 5 },
            State = new StateRecord { Enabled = true },
            OptionalState = new StateRecord { Enabled = false }
        };
        var context = new ChronicleContext();

        long allocated = AllocationTestHelper.MeasureAfterWarmup(
            warmup: () =>
            {
                for (int i = 0; i < 32768; i++)
                {
                    _ = ChronicleHashSerializer.Compute(record, context);
                }
            },
            measured: () =>
            {
                for (int i = 0; i < 4096; i++)
                {
                    _ = ChronicleHashSerializer.Compute(record, context);
                }
            });

        allocated.Should().Be(0);
    }

    [Fact]
    public void Compute_WithEnumLeaf_ShouldNotAllocateAfterWarmup()
    {
        var record = new EnumRecord();
        var context = new ChronicleContext();

        long allocated = AllocationTestHelper.MeasureAfterWarmup(
            warmup: () =>
            {
                for (int i = 0; i < 32768; i++)
                {
                    _ = ChronicleHashSerializer.Compute(record, context);
                }
            },
            measured: () =>
            {
                for (int i = 0; i < 4096; i++)
                {
                    _ = ChronicleHashSerializer.Compute(record, context);
                }
            });

        allocated.Should().Be(0);
    }

    private static ChronicleContext CreateLinkContext(LinkResource firstResource, LinkResource secondResource)
    {
        var context = new ChronicleContext();
        context.Links.RegisterInstance("first", firstResource, slot: "primary");
        context.Links.RegisterInstance("second", secondResource, slot: "primary");
        return context;
    }

    private sealed class RecordGraph : IRecordable
    {
        public int Count = 7;
        public string? Alias;
        public ChildRecord? Child = new();
        public StateRecord State;
        public StateRecord? OptionalState;

        public void RecordData(IChronicler chronicler)
        {
            RecordValues.Look(chronicler, ref Count, "count", 7);
            RecordValues.Look(chronicler, ref Alias, "alias", defaultValue: null);

            ChildRecord child = Child!;
            RecordDeep.Look(chronicler, ref child, "child");
            if (chronicler.Mode == SerializationMode.Loading)
                Child = child;

            RecordDeepStruct.Look(chronicler, ref State, "state");
            RecordNullableDeep.Look(chronicler, ref OptionalState, "optionalState");
        }
    }

    private sealed class GoldenRecord : IRecordable
    {
        public int Count = 7;
        public string? Alias;

        public void RecordData(IChronicler chronicler)
        {
            RecordValues.Look(chronicler, ref Count, "count", 7);
            RecordValues.Look(chronicler, ref Alias, "alias", defaultValue: null);
        }
    }

    private sealed class ChildRecord : IRecordable
    {
        public int Level = 1;

        public void RecordData(IChronicler chronicler)
        {
            RecordValues.Look(chronicler, ref Level, "level", 1);
        }
    }

    private struct StateRecord : IRecordable
    {
        public bool Enabled;

        public void RecordData(IChronicler chronicler)
        {
            RecordValues.Look(chronicler, ref Enabled, "enabled", false);
        }
    }

    private sealed class OrderedRecord : IRecordable
    {
        private readonly bool _reverseOrder;
        private int _first = 1;
        private int _second = 2;

        public OrderedRecord(bool reverseOrder)
        {
            _reverseOrder = reverseOrder;
        }

        public void RecordData(IChronicler chronicler)
        {
            if (_reverseOrder)
            {
                RecordValues.Look(chronicler, ref _second, "second", 2);
                RecordValues.Look(chronicler, ref _first, "first", 1);
                return;
            }

            RecordValues.Look(chronicler, ref _first, "first", 1);
            RecordValues.Look(chronicler, ref _second, "second", 2);
        }
    }

    private sealed class FieldNameRecord : IRecordable
    {
        private readonly bool _useAlternateName;
        private int _value = 3;

        public FieldNameRecord(bool useAlternateName)
        {
            _useAlternateName = useAlternateName;
        }

        public void RecordData(IChronicler chronicler)
        {
            RecordValues.Look(chronicler, ref _value, _useAlternateName ? "renamed" : "value", 3);
        }
    }

    private sealed class DefaultRecord : IRecordable
    {
        private readonly int _declaredDefault;
        private int _value = 7;

        public DefaultRecord(int declaredDefault)
        {
            _declaredDefault = declaredDefault;
        }

        public void RecordData(IChronicler chronicler)
        {
            RecordValues.Look(chronicler, ref _value, "value", _declaredDefault);
        }
    }

    private sealed class LinkRecord : IRecordable
    {
        private LinkResource? _resource;

        public LinkRecord(LinkResource resource)
        {
            _resource = resource;
        }

        public void RecordData(IChronicler chronicler)
        {
            RecordLinks.Look(chronicler, ref _resource, "resource", slot: "primary");
        }
    }

    private sealed class LinkResource
    {
    }

    private sealed class UnsupportedLeafRecord : IRecordable
    {
        private double _temperature = 98.6d;

        public void RecordData(IChronicler chronicler)
        {
            RecordValues.Look(chronicler, ref _temperature, "temperature");
        }
    }

    private sealed class EnumRecord : IRecordable
    {
        private TestMode _mode = TestMode.Second;

        public void RecordData(IChronicler chronicler)
        {
            RecordValues.Look(chronicler, ref _mode, "mode", TestMode.First);
        }
    }

    private enum TestMode
    {
        First = 1,
        Second = 2
    }

    private sealed class GenericRecord<T> : IRecordable
        where T : struct
    {
        private int _value = 7;

        public void RecordData(IChronicler chronicler)
        {
            RecordValues.Look(chronicler, ref _value, "value");
        }
    }
}
