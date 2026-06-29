using FluentAssertions;
using System;
using Xunit;

namespace Chronicler.Tests;

public sealed class ChronicleHashWriterTests
{
    [Fact]
    public void ChronicleHash_ShouldFormatAsLowercaseHighThenLowHex()
    {
        var hash = new ChronicleHash(
            low: 0x0123_4567_89ab_cdefUL,
            high: 0xfedc_ba98_7654_3210UL);

        hash.ToString().Should().Be("fedcba98765432100123456789abcdef");
        hash.GetHashCode().Should().Be(new ChronicleHash(0x0123_4567_89ab_cdefUL, 0xfedc_ba98_7654_3210UL).GetHashCode());
        hash.Should().Be(new ChronicleHash(0x0123_4567_89ab_cdefUL, 0xfedc_ba98_7654_3210UL));
        hash.Should().NotBe(new ChronicleHash(0x0123_4567_89ab_cdeeUL, 0xfedc_ba98_7654_3210UL));
        (hash == new ChronicleHash(0x0123_4567_89ab_cdefUL, 0xfedc_ba98_7654_3210UL)).Should().BeTrue();
        (hash != new ChronicleHash(0x0123_4567_89ab_cdeeUL, 0xfedc_ba98_7654_3210UL)).Should().BeTrue();
        hash.Equals("not-a-hash").Should().BeFalse();
    }

    [Fact]
    public void DefaultWriter_ShouldUseSameSeedAsConstructedWriter()
    {
        ChronicleHashWriter defaultWriter = default;
        var constructedWriter = new ChronicleHashWriter();

        defaultWriter.ToHash().Should().Be(constructedWriter.ToHash());

        defaultWriter.WriteByte(7);
        constructedWriter.WriteByte(7);

        defaultWriter.ToHash().Should().Be(constructedWriter.ToHash());
    }

    [Fact]
    public void Writer_ShouldProduceSameHashForSamePrimitiveSequence()
    {
        ChronicleHash first = WritePrimitiveSequence();
        ChronicleHash second = WritePrimitiveSequence();

        second.Should().Be(first);
    }

    [Fact]
    public void Writer_ShouldMatchStableGoldenVectors()
    {
        new ChronicleHashWriter().ToHash().ToString()
            .Should().Be("2c4bf5ffd2995eb6efd01f60ba992926");

        WritePrimitiveSequence().ToString()
            .Should().Be("5cf27411c5ed8ef4acf00558a46ce432");
    }

    [Fact]
    public void Writer_ShouldChangeHashWhenSectionOrderChanges()
    {
        var first = new ChronicleHashWriter();
        first.WriteSection("first", 1);
        first.WriteInt32(7);
        first.WriteSection("second", 1);
        first.WriteInt32(11);

        var second = new ChronicleHashWriter();
        second.WriteSection("second", 1);
        second.WriteInt32(11);
        second.WriteSection("first", 1);
        second.WriteInt32(7);

        second.ToHash().Should().NotBe(first.ToHash());
    }

    [Fact]
    public void Writer_ShouldUseLittleEndianPrimitiveBytes()
    {
        var primitive = new ChronicleHashWriter();
        primitive.WriteUInt16(0x1234);
        primitive.WriteInt16(unchecked((short)0x9876));
        primitive.WriteUInt32(0x89ab_cdef);
        primitive.WriteInt32(unchecked((int)0x7654_3210));
        primitive.WriteUInt64(0x0123_4567_89ab_cdefUL);
        primitive.WriteInt64(unchecked((long)0xfedc_ba98_7654_3210UL));
        primitive.WriteChar('\u2211');

        var bytes = new ChronicleHashWriter();
        bytes.WriteByte(0x34);
        bytes.WriteByte(0x12);
        bytes.WriteByte(0x76);
        bytes.WriteByte(0x98);
        bytes.WriteByte(0xef);
        bytes.WriteByte(0xcd);
        bytes.WriteByte(0xab);
        bytes.WriteByte(0x89);
        bytes.WriteByte(0x10);
        bytes.WriteByte(0x32);
        bytes.WriteByte(0x54);
        bytes.WriteByte(0x76);
        bytes.WriteByte(0xef);
        bytes.WriteByte(0xcd);
        bytes.WriteByte(0xab);
        bytes.WriteByte(0x89);
        bytes.WriteByte(0x67);
        bytes.WriteByte(0x45);
        bytes.WriteByte(0x23);
        bytes.WriteByte(0x01);
        bytes.WriteByte(0x10);
        bytes.WriteByte(0x32);
        bytes.WriteByte(0x54);
        bytes.WriteByte(0x76);
        bytes.WriteByte(0x98);
        bytes.WriteByte(0xba);
        bytes.WriteByte(0xdc);
        bytes.WriteByte(0xfe);
        bytes.WriteByte(0x11);
        bytes.WriteByte(0x22);

        bytes.ToHash().Should().Be(primitive.ToHash());
    }

    [Fact]
    public void Writer_ShouldEncodeStringsAsNullableLengthPrefixedUtf16CodeUnits()
    {
        var canonical = new ChronicleHashWriter();
        canonical.WriteString(null);
        canonical.WriteString(string.Empty);
        canonical.WriteString("A\u2211");

        var manual = new ChronicleHashWriter();
        manual.WriteBool(false);
        manual.WriteBool(true);
        manual.WriteInt32(0);
        manual.WriteBool(true);
        manual.WriteInt32(2);
        manual.WriteChar('A');
        manual.WriteChar('\u2211');

        canonical.ToHash().Should().Be(manual.ToHash());
    }

    [Fact]
    public void WriteSection_ShouldRejectMissingEmptyOrNonAsciiTags()
    {
        var writer = new ChronicleHashWriter();

        Action nullTag = () => writer.WriteSection(null!, 1);
        Action emptyTag = () => writer.WriteSection(string.Empty, 1);
        Action nonAsciiTag = () => writer.WriteSection("na\u00efve", 1);

        nullTag.Should().Throw<ArgumentNullException>();
        emptyTag.Should().Throw<ArgumentException>();
        nonAsciiTag.Should().Throw<ArgumentException>()
            .WithMessage("*ASCII*");
    }

    [Fact]
    public void WriteEnum_ShouldHashUnderlyingWidthBytesWithoutBoxingSemantics()
    {
        var enumWriter = new ChronicleHashWriter();
        enumWriter.WriteEnum(SByteBacked.Negative);
        enumWriter.WriteEnum(ByteBacked.Value);
        enumWriter.WriteEnum(ShortBacked.Negative);
        enumWriter.WriteEnum(UShortBacked.Value);
        enumWriter.WriteEnum(IntBacked.Negative);
        enumWriter.WriteEnum(UIntBacked.Value);
        enumWriter.WriteEnum(LongBacked.Negative);
        enumWriter.WriteEnum(ULongBacked.Value);

        var primitiveWriter = new ChronicleHashWriter();
        primitiveWriter.WriteSByte(-7);
        primitiveWriter.WriteByte(0xab);
        primitiveWriter.WriteInt16(-0x1234);
        primitiveWriter.WriteUInt16(0x1234);
        primitiveWriter.WriteInt32(-9);
        primitiveWriter.WriteUInt32(0x89ab_cdef);
        primitiveWriter.WriteInt64(-0x0123_4567_89ab_cdefL);
        primitiveWriter.WriteUInt64(0x0123_4567_89ab_cdefUL);

        enumWriter.ToHash().Should().Be(primitiveWriter.ToHash());
    }

    [Fact]
    public void Writer_ShouldNotAllocateAfterWarmup()
    {
        long allocated = AllocationTestHelper.MeasureAfterWarmup(
            warmup: () =>
            {
                for (int i = 0; i < 32768; i++)
                {
                    _ = WritePrimitiveSequence();
                }
            },
            measured: () =>
            {
                for (int i = 0; i < 4096; i++)
                {
                    _ = WritePrimitiveSequence();
                }
            });

        allocated.Should().Be(0);
    }

    private static ChronicleHash WritePrimitiveSequence()
    {
        var writer = new ChronicleHashWriter();
        writer.WriteSection("primitive", 1);
        writer.WriteBool(true);
        writer.WriteByte(17);
        writer.WriteSByte(-18);
        writer.WriteInt16(-19);
        writer.WriteUInt16(20);
        writer.WriteInt32(-21);
        writer.WriteUInt32(22);
        writer.WriteInt64(-23);
        writer.WriteUInt64(24);
        writer.WriteChar('Z');
        writer.WriteString("mage");
        writer.WriteEnum(IntBacked.Negative);
        return writer.ToHash();
    }

    private enum SByteBacked : sbyte
    {
        Negative = -7
    }

    private enum ByteBacked : byte
    {
        Value = 0xab
    }

    private enum ShortBacked : short
    {
        Negative = -0x1234
    }

    private enum UShortBacked : ushort
    {
        Value = 0x1234
    }

    private enum IntBacked
    {
        Negative = -9
    }

    private enum UIntBacked : uint
    {
        Value = 0x89ab_cdef
    }

    private enum LongBacked : long
    {
        Negative = -0x0123_4567_89ab_cdefL
    }

    private enum ULongBacked : ulong
    {
        Value = 0x0123_4567_89ab_cdefUL
    }
}
