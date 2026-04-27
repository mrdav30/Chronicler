using FluentAssertions;
using System;
using Xunit;

namespace Chronicler.Tests;

public class SerializerGuardTests
{
    [Fact]
    public void JsonSerialize_ShouldThrow_WhenTargetIsNull()
    {
        Action act = () => JsonRecordSerializer.Serialize(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("target");
    }

    [Fact]
    public void JsonPopulate_ShouldThrow_WhenTargetIsNull()
    {
        Action act = () => JsonRecordSerializer.Populate(null!, "{}");

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("target");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void JsonPopulate_ShouldThrow_WhenPayloadIsBlank(string payload)
    {
        Action act = () => JsonRecordSerializer.Populate(new SimpleRecord(), payload);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("json");
    }

    [Fact]
    public void JsonPopulate_OverloadWithoutContext_ShouldPopulateSuccessfully()
    {
        var source = new SimpleRecord { Count = 12 };
        string payload = JsonRecordSerializer.Serialize(source);
        var target = new SimpleRecord();

        JsonRecordSerializer.Populate(target, payload);

        target.Count.Should().Be(12);
    }

#if !CHRONICLER_DISABLE_MEMORYPACK
    [Fact]
    public void MemoryPackSerialize_ShouldThrow_WhenTargetIsNull()
    {
        Action act = () => MemoryPackRecordSerializer.Serialize(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("target");
    }

    [Fact]
    public void MemoryPackPopulateByteArray_ShouldThrow_WhenTargetIsNull()
    {
        byte[] data = new byte[] { 1 };

        Action act = () => MemoryPackRecordSerializer.Populate(null!, data);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("target");
    }

    [Fact]
    public void MemoryPackPopulateByteArray_ShouldThrow_WhenDataIsNull()
    {
        Action act = () => MemoryPackRecordSerializer.Populate(new SimpleRecord(), (byte[])null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("data");
    }

    [Fact]
    public void MemoryPackPopulateSpan_ShouldThrow_WhenDataIsEmpty()
    {
        Action act = () => MemoryPackRecordSerializer.Populate(new SimpleRecord(), ReadOnlySpan<byte>.Empty);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("data");
    }

    [Fact]
    public void MemoryPackPopulate_ByteArrayOverloadWithoutContext_ShouldPopulateSuccessfully()
    {
        var source = new SimpleRecord { Count = 12 };
        byte[] payload = MemoryPackRecordSerializer.Serialize(source);
        var target = new SimpleRecord();

        MemoryPackRecordSerializer.Populate(target, payload);

        target.Count.Should().Be(12);
    }

    [Fact]
    public void MemoryPackPopulate_SpanOverloadWithoutContext_ShouldPopulateSuccessfully()
    {
        var source = new SimpleRecord { Count = 12 };
        byte[] payload = MemoryPackRecordSerializer.Serialize(source);
        var target = new SimpleRecord();

        MemoryPackRecordSerializer.Populate(target, payload.AsSpan());

        target.Count.Should().Be(12);
    }
#endif

    private sealed class SimpleRecord : IRecordable
    {
        public int Count = 7;

        public void RecordData(IChronicler chronicler)
        {
            RecordValues.Look(chronicler, ref Count, "count", 7);
        }
    }
}
