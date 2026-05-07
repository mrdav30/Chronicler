using FluentAssertions;
using System;
using Xunit;

namespace Chronicler.Tests;

[Collection("MemoryPack compatibility")]
public class MemoryPackCompatibilityTests
{
#if !CHRONICLER_DISABLE_MEMORYPACK
    [Fact]
    public void Populate_ShouldReadPayloadWrittenWithSwiftDictionaryEnvelope()
    {
        byte[] payload = Convert.FromBase64String("AQEBAQAAAPr///8FAAAAY291bnQEAAAAKgAAAA==");
        var target = new SimpleRecord();

        MemoryPackRecordSerializer.Populate(target, payload);

        target.Count.Should().Be(42);
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

[CollectionDefinition("MemoryPack compatibility", DisableParallelization = true)]
public sealed class MemoryPackCompatibilityCollection
{
}
