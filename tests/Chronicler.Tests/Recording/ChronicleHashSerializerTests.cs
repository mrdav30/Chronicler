using FluentAssertions;
using System;
using System.Collections.Generic;
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
    public void Compute_ShouldThrow_WhenTargetIsNull()
    {
        Action act = () => ChronicleHashSerializer.Compute(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("target");
    }

    [Fact]
    public void Compute_ShouldThrow_WhenContextIsNull()
    {
        Action act = () => ChronicleHashSerializer.Compute(new GoldenRecord(), null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void Contribute_ShouldThrow_WhenTargetIsNull()
    {
        var writer = new ChronicleHashWriter();

        Action act = () => ChronicleHashSerializer.Contribute(null!, ref writer);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("target");
    }

    [Fact]
    public void Contribute_ShouldThrow_WhenContextIsNull()
    {
        var writer = new ChronicleHashWriter();

        Action act = () => ChronicleHashSerializer.Contribute(new GoldenRecord(), null!, ref writer);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
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
    public void Compute_ShouldRejectNullFieldNames()
    {
        var record = new NullFieldNameRecord();

        Action act = () => ChronicleHashSerializer.Compute(record);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("name");
    }

    [Fact]
    public void Compute_ShouldThrowWhenUnslottedLinkCannotResolveToStableId()
    {
        var record = new UnslottedLinkRecord(new LinkResource());

        Action act = () => ChronicleHashSerializer.Compute(record, new ChronicleContext());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unable to hash link 'resource'*stable id*")
            .And.Message.Should().NotContain("in slot");
    }

    [Fact]
    public void Compute_ShouldHashNullDeepClassAndNullLinkValues()
    {
        var nullRecord = new NullableReferenceRecord();

        var resource = new LinkResource();
        var presentRecord = new NullableReferenceRecord
        {
            Child = new ChildRecord { Level = 5 },
            Resource = resource
        };
        var context = new ChronicleContext();
        context.Links.RegisterInstance("resource", resource);

        ChronicleHashSerializer.Compute(nullRecord)
            .Should().NotBe(ChronicleHashSerializer.Compute(presentRecord, context));
    }

    [Fact]
    public void Compute_ShouldIncludeEverySupportedPrimitiveLeafInHash()
    {
        var baseline = new SupportedLeafRecord();
        ChronicleHash baselineHash = ChronicleHashSerializer.Compute(baseline);

        ChronicleHashSerializer.Compute(new SupportedLeafRecord()).Should().Be(baselineHash);
        EachSupportedLeafMutation()
            .Should().OnlyContain(record => ChronicleHashSerializer.Compute(record) != baselineHash);
    }

    [Fact]
    public void Compute_ShouldIncludeEveryEnumUnderlyingWidthInHash()
    {
        var baseline = new EnumWidthRecord();
        ChronicleHash baselineHash = ChronicleHashSerializer.Compute(baseline);

        ChronicleHashSerializer.Compute(new EnumWidthRecord()).Should().Be(baselineHash);
        EachEnumWidthMutation()
            .Should().OnlyContain(record => ChronicleHashSerializer.Compute(record) != baselineHash);
    }

    [Fact]
    public void Compute_ShouldUseStableTypeNamesForGenericArrayArguments()
    {
        ChronicleHash vectorHash = ChronicleHashSerializer.Compute(new GenericPairRecord<int[], string>());
        ChronicleHash gridHash = ChronicleHashSerializer.Compute(new GenericPairRecord<int[,], string>());
        ChronicleHash reversedHash = ChronicleHashSerializer.Compute(new GenericPairRecord<string, int[]>());

        vectorHash.Should().NotBe(gridHash);
        reversedHash.Should().NotBe(vectorHash);
    }

    [Fact]
    public void Compute_ShouldAllowNestedHashComputationDuringRecordData()
    {
        var record = new ReentrantHashRecord(new GoldenRecord { Count = 5, Alias = "inner" });

        ChronicleHash hash = ChronicleHashSerializer.Compute(record);

        record.InnerHash.Should().Be(ChronicleHashSerializer.Compute(new GoldenRecord { Count = 5, Alias = "inner" }));
        hash.Should().NotBe(default(ChronicleHash));
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

    private sealed class ReentrantHashRecord : IRecordable
    {
        private readonly IRecordable _inner;
        private int _value = 7;

        public ReentrantHashRecord(IRecordable inner)
        {
            _inner = inner;
        }

        public ChronicleHash InnerHash { get; private set; }

        public void RecordData(IChronicler chronicler)
        {
            InnerHash = ChronicleHashSerializer.Compute(_inner);
            RecordValues.Look(chronicler, ref _value, "value", 7);
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

    private sealed class NullableReferenceRecord : IRecordable
    {
        public ChildRecord? Child;
        public LinkResource? Resource;

        public void RecordData(IChronicler chronicler)
        {
            ChildRecord child = Child!;
            RecordDeep.Look(chronicler, ref child, "child");
            if (chronicler.Mode == SerializationMode.Loading)
                Child = child;

            RecordLinks.Look(chronicler, ref Resource, "resource");
        }
    }

    private sealed class NullFieldNameRecord : IRecordable
    {
        private int _value = 1;

        public void RecordData(IChronicler chronicler)
        {
            RecordValues.Look(chronicler, ref _value, null!);
        }
    }

    private sealed class UnslottedLinkRecord : IRecordable
    {
        private LinkResource? _resource;

        public UnslottedLinkRecord(LinkResource resource)
        {
            _resource = resource;
        }

        public void RecordData(IChronicler chronicler)
        {
            RecordLinks.Look(chronicler, ref _resource, "resource");
        }
    }

    private sealed class UnsupportedLeafRecord : IRecordable
    {
        private double _temperature = 98.6d;

        public void RecordData(IChronicler chronicler)
        {
            RecordValues.Look(chronicler, ref _temperature, "temperature");
        }
    }

    private sealed class SupportedLeafRecord : IRecordable
    {
        public bool BoolValue = true;
        public byte ByteValue = 2;
        public sbyte SByteValue = -3;
        public short Int16Value = -4;
        public ushort UInt16Value = 5;
        public int Int32Value = -6;
        public uint UInt32Value = 7;
        public long Int64Value = -8;
        public ulong UInt64Value = 9;
        public char CharValue = 'x';
        public string? StringValue = "mage";

        public void RecordData(IChronicler chronicler)
        {
            RecordValues.Look(chronicler, ref BoolValue, "bool", false);
            RecordValues.Look(chronicler, ref ByteValue, "byte", (byte)0);
            RecordValues.Look(chronicler, ref SByteValue, "sbyte", (sbyte)0);
            RecordValues.Look(chronicler, ref Int16Value, "int16", (short)0);
            RecordValues.Look(chronicler, ref UInt16Value, "uint16", (ushort)0);
            RecordValues.Look(chronicler, ref Int32Value, "int32", 0);
            RecordValues.Look(chronicler, ref UInt32Value, "uint32", 0u);
            RecordValues.Look(chronicler, ref Int64Value, "int64", 0L);
            RecordValues.Look(chronicler, ref UInt64Value, "uint64", 0UL);
            RecordValues.Look(chronicler, ref CharValue, "char", '\0');
            RecordValues.Look(chronicler, ref StringValue, "string", defaultValue: null);
        }
    }

    private static IEnumerable<SupportedLeafRecord> EachSupportedLeafMutation()
    {
        yield return new SupportedLeafRecord { BoolValue = false };
        yield return new SupportedLeafRecord { ByteValue = 12 };
        yield return new SupportedLeafRecord { SByteValue = -13 };
        yield return new SupportedLeafRecord { Int16Value = -14 };
        yield return new SupportedLeafRecord { UInt16Value = 15 };
        yield return new SupportedLeafRecord { Int32Value = -16 };
        yield return new SupportedLeafRecord { UInt32Value = 17 };
        yield return new SupportedLeafRecord { Int64Value = -18 };
        yield return new SupportedLeafRecord { UInt64Value = 19 };
        yield return new SupportedLeafRecord { CharValue = 'y' };
        yield return new SupportedLeafRecord { StringValue = null };
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

    private sealed class EnumWidthRecord : IRecordable
    {
        public SByteMode SByteMode = SByteMode.Second;
        public ByteMode ByteMode = ByteMode.Second;
        public Int16Mode Int16Mode = Int16Mode.Second;
        public UInt16Mode UInt16Mode = UInt16Mode.Second;
        public Int32Mode Int32Mode = Int32Mode.Second;
        public UInt32Mode UInt32Mode = UInt32Mode.Second;
        public Int64Mode Int64Mode = Int64Mode.Second;
        public UInt64Mode UInt64Mode = UInt64Mode.Second;

        public void RecordData(IChronicler chronicler)
        {
            RecordValues.Look(chronicler, ref SByteMode, "sbyte", SByteMode.First);
            RecordValues.Look(chronicler, ref ByteMode, "byte", ByteMode.First);
            RecordValues.Look(chronicler, ref Int16Mode, "int16", Int16Mode.First);
            RecordValues.Look(chronicler, ref UInt16Mode, "uint16", UInt16Mode.First);
            RecordValues.Look(chronicler, ref Int32Mode, "int32", Int32Mode.First);
            RecordValues.Look(chronicler, ref UInt32Mode, "uint32", UInt32Mode.First);
            RecordValues.Look(chronicler, ref Int64Mode, "int64", Int64Mode.First);
            RecordValues.Look(chronicler, ref UInt64Mode, "uint64", UInt64Mode.First);
        }
    }

    private static IEnumerable<EnumWidthRecord> EachEnumWidthMutation()
    {
        yield return new EnumWidthRecord { SByteMode = SByteMode.Third };
        yield return new EnumWidthRecord { ByteMode = ByteMode.Third };
        yield return new EnumWidthRecord { Int16Mode = Int16Mode.Third };
        yield return new EnumWidthRecord { UInt16Mode = UInt16Mode.Third };
        yield return new EnumWidthRecord { Int32Mode = Int32Mode.Third };
        yield return new EnumWidthRecord { UInt32Mode = UInt32Mode.Third };
        yield return new EnumWidthRecord { Int64Mode = Int64Mode.Third };
        yield return new EnumWidthRecord { UInt64Mode = UInt64Mode.Third };
    }

    private enum SByteMode : sbyte
    {
        First = -1,
        Second = -2,
        Third = -3
    }

    private enum ByteMode : byte
    {
        First = 1,
        Second = 2,
        Third = 3
    }

    private enum Int16Mode : short
    {
        First = -1,
        Second = -2,
        Third = -3
    }

    private enum UInt16Mode : ushort
    {
        First = 1,
        Second = 2,
        Third = 3
    }

    private enum Int32Mode
    {
        First = -1,
        Second = -2,
        Third = -3
    }

    private enum UInt32Mode : uint
    {
        First = 1,
        Second = 2,
        Third = 3
    }

    private enum Int64Mode : long
    {
        First = -1,
        Second = -2,
        Third = -3
    }

    private enum UInt64Mode : ulong
    {
        First = 1,
        Second = 2,
        Third = 3
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

    private sealed class GenericPairRecord<TFirst, TSecond> : IRecordable
    {
        private int _value = 7;

        public void RecordData(IChronicler chronicler)
        {
            RecordValues.Look(chronicler, ref _value, "value");
        }
    }
}
