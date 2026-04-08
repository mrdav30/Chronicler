using FluentAssertions;
using Xunit;

namespace Chronicler.Tests;

public class SerializationPayloadEditorGuardTests
{
    [Fact]
    public void RemoveJsonProperty_ShouldThrow_WhenPathIsMissing()
    {
        FluentActions.Invoking(() => SerializationPayloadEditor.RemoveJsonProperty("{}", null!))
            .Should().Throw<System.ArgumentException>()
            .WithParameterName("path");

        FluentActions.Invoking(() => SerializationPayloadEditor.RemoveJsonProperty("{}"))
            .Should().Throw<System.ArgumentException>()
            .WithParameterName("path");
    }

    [Fact]
    public void SetJsonValue_ShouldThrow_WhenPathIsMissing()
    {
        FluentActions.Invoking(() => SerializationPayloadEditor.SetJsonValue("{}", 1, null!))
            .Should().Throw<System.ArgumentException>()
            .WithParameterName("path");

        FluentActions.Invoking(() => SerializationPayloadEditor.SetJsonValue("{}", 1))
            .Should().Throw<System.ArgumentException>()
            .WithParameterName("path");
    }

    [Fact]
    public void RemoveMemoryPackEntry_ShouldThrow_WhenPathIsMissing()
    {
        byte[] payload = MemoryPackRecordSerializer.Serialize(new SimpleRecord());

        FluentActions.Invoking(() => SerializationPayloadEditor.RemoveMemoryPackEntry(payload, null!))
            .Should().Throw<System.ArgumentException>()
            .WithParameterName("path");

        FluentActions.Invoking(() => SerializationPayloadEditor.RemoveMemoryPackEntry(payload))
            .Should().Throw<System.ArgumentException>()
            .WithParameterName("path");
    }

    [Fact]
    public void SetMemoryPackValue_ShouldThrow_WhenPathIsMissing()
    {
        byte[] payload = MemoryPackRecordSerializer.Serialize(new SimpleRecord());

        FluentActions.Invoking(() => SerializationPayloadEditor.SetMemoryPackValue(payload, 1, null!))
            .Should().Throw<System.ArgumentException>()
            .WithParameterName("path");

        FluentActions.Invoking(() => SerializationPayloadEditor.SetMemoryPackValue(payload, 1))
            .Should().Throw<System.ArgumentException>()
            .WithParameterName("path");
    }

    [Fact]
    public void RemoveJsonProperty_ShouldThrow_WhenRootIsJsonNull()
    {
        FluentActions.Invoking(() => SerializationPayloadEditor.RemoveJsonProperty("null", "value"))
            .Should().Throw<System.InvalidOperationException>()
            .WithMessage("*Unable to parse JSON payload.*");
    }

    [Fact]
    public void SetJsonValue_ShouldThrow_WhenRootIsNotAnObject()
    {
        FluentActions.Invoking(() => SerializationPayloadEditor.SetJsonValue("[]", 1, "value"))
            .Should().Throw<System.InvalidOperationException>()
            .WithMessage("*Expected JSON root object.*");
    }

    [Fact]
    public void RemoveMemoryPackEntry_ShouldThrow_WhenIntermediateEntryIsMissing()
    {
        byte[] payload = MemoryPackRecordSerializer.Serialize(new SimpleRecord());

        FluentActions.Invoking(() => SerializationPayloadEditor.RemoveMemoryPackEntry(payload, "missing", "value"))
            .Should().Throw<System.InvalidOperationException>()
            .WithMessage("*Unable to locate MemoryPack entry 'missing' at depth 0.*");
    }

    private sealed class SimpleRecord : IRecordable
    {
        public int Count = 7;

        public void RecordData(IChronicler chronicler)
        {
            RecordValues.Look(chronicler, ref Count, "value", 7);
        }
    }
}
