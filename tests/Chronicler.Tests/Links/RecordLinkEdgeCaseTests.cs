using FluentAssertions;
using System;
using Xunit;

namespace Chronicler.Tests;

public class RecordLinkEdgeCaseTests
{
    [Theory]
    [MemberData(nameof(SerializationTransportData.All), MemberType = typeof(SerializationTransportData))]
    public void Populate_ShouldClearImmediateLink_WhenEntryIsMissing(SerializationTransport transport)
    {
        var resource = new LinkResource("source");
        var saveContext = CreateContext(resource);
        var source = new ImmediateLinkRecord { Resource = resource };

        object payload = SerializationTestHarness.Serialize(source, transport, saveContext);
        payload = SerializationTestHarness.RemoveEntry(payload, transport, "resource");

        var target = new ImmediateLinkRecord { Resource = new LinkResource("keep-me") };
        SerializationTestHarness.Populate(target, payload, transport, new ChronicleContext());

        target.Resource.Should().BeNull();
    }

    [Fact]
    public void JsonPopulate_ShouldClearImmediateLink_WhenEntryIsExplicitNull()
    {
        var resource = new LinkResource("source");
        var saveContext = CreateContext(resource);
        var source = new ImmediateLinkRecord { Resource = resource };

        object payload = SerializationTestHarness.Serialize(source, SerializationTransport.Json, saveContext);
        payload = SerializationTestHarness.SetValue<string?>(payload, SerializationTransport.Json, null, "resource");

        var target = new ImmediateLinkRecord { Resource = new LinkResource("keep-me") };
        SerializationTestHarness.Populate(target, payload, SerializationTransport.Json, new ChronicleContext());

        target.Resource.Should().BeNull();
    }

    [Fact]
    public void MemoryPackPopulate_ShouldClearImmediateLink_WhenLinkIdPayloadIsNull()
    {
        var resource = new LinkResource("source");
        var saveContext = CreateContext(resource);
        var source = new ImmediateLinkRecord { Resource = resource };

        object payload = SerializationTestHarness.Serialize(source, SerializationTransport.MemoryPack, saveContext);
        payload = SerializationTestHarness.SetValue<string?>(payload, SerializationTransport.MemoryPack, null, "resource");

        var target = new ImmediateLinkRecord { Resource = new LinkResource("keep-me") };
        SerializationTestHarness.Populate(target, payload, SerializationTransport.MemoryPack, new ChronicleContext());

        target.Resource.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(SerializationTransportData.All), MemberType = typeof(SerializationTransportData))]
    public void Populate_ShouldResolveDeferredLinkImmediately_WhenContextAlreadyContainsTarget(SerializationTransport transport)
    {
        var resource = new LinkResource("source");
        var saveContext = CreateContext(resource);
        var source = new DeferredImmediateResolutionRecord { Resource = resource };

        object payload = SerializationTestHarness.Serialize(source, transport, saveContext);

        var resolved = new LinkResource("resolved");
        var loadContext = new ChronicleContext();
        loadContext.Links.RegisterInstance("resource-id", resolved);

        var target = new DeferredImmediateResolutionRecord();
        SerializationTestHarness.Populate(target, payload, transport, loadContext);

        target.Resource.Should().BeSameAs(resolved);
    }

    [Theory]
    [MemberData(nameof(SerializationTransportData.All), MemberType = typeof(SerializationTransportData))]
    public void Populate_ShouldThrow_WhenDeferredLinkHasNoAssignmentCallback(SerializationTransport transport)
    {
        var resource = new LinkResource("source");
        var saveContext = CreateContext(resource);
        var source = new DeferredLinkWithoutCallbackRecord { Resource = resource };

        object payload = SerializationTestHarness.Serialize(source, transport, saveContext);

        var target = new DeferredLinkWithoutCallbackRecord();
        Action act = () => SerializationTestHarness.Populate(target, payload, transport, new ChronicleContext());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*requires an assignment callback*");
    }

    [Theory]
    [MemberData(nameof(SerializationTransportData.All), MemberType = typeof(SerializationTransportData))]
    public void Populate_ShouldDescribeUnresolvedDeferredLinks_WithoutSlotSuffix(SerializationTransport transport)
    {
        var resource = new LinkResource("source");
        var saveContext = CreateContext(resource);
        var source = new DeferredImmediateResolutionRecord { Resource = resource };

        object payload = SerializationTestHarness.Serialize(source, transport, saveContext);

        var target = new DeferredImmediateResolutionRecord();
        Action act = () => SerializationTestHarness.Populate(target, payload, transport, new ChronicleContext());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*[resource:LinkResource:resource-id]*")
            .And.Message.Should().NotContain("@");
    }

    [Fact]
    public void LookDeferred_ShouldThrow_WhenAssignmentCallbackIsNull()
    {
        var chronicler = new NoOpChronicler();
        LinkResource? resource = new("source");

        Action act = () => RecordLinks.LookDeferred(chronicler, resource, "resource", null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("assignLoadedValue");
    }

    private static ChronicleContext CreateContext(LinkResource resource)
    {
        var context = new ChronicleContext();
        context.Links.RegisterInstance("resource-id", resource);
        return context;
    }

    private sealed class ImmediateLinkRecord : IRecordable
    {
        public LinkResource? Resource;

        public void RecordData(IChronicler chronicler)
        {
            LinkResource? resource = Resource;
            RecordLinks.Look(chronicler, ref resource, "resource");

            if (chronicler.Mode == SerializationMode.Loading)
                Resource = resource;
        }
    }

    private sealed class DeferredImmediateResolutionRecord : IRecordable
    {
        public LinkResource? Resource;

        public void RecordData(IChronicler chronicler)
        {
            LinkResource? resource = Resource;
            RecordLinks.LookDeferred(
                chronicler,
                resource,
                "resource",
                resolved => Resource = resolved);

            if (chronicler.Mode == SerializationMode.Loading && resource is null)
                Resource ??= resource;
        }
    }

    private sealed class DeferredLinkWithoutCallbackRecord : IRecordable
    {
        public LinkResource? Resource;

        public void RecordData(IChronicler chronicler)
        {
            LinkResource? resource = Resource;
            chronicler.LookLink(
                ref resource,
                "resource",
                resolveMode: RecordLinkResolveMode.Deferred,
                assignLoadedValue: null);

            if (chronicler.Mode == SerializationMode.Loading)
                Resource = resource;
        }
    }

    private sealed class LinkResource
    {
        public LinkResource(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }

    private sealed class NoOpChronicler : IChronicler
    {
        public ChronicleContext Context { get; } = new();

        public SerializationMode Mode => SerializationMode.Saving;

        public void LookValue<T>(ref T value, string name, T? defaultValue = default)
        {
        }

        public void LookDeep<T>(ref T value, string name) where T : class, IRecordable
        {
        }

        public void LookDeepStruct<T>(ref T value, string name) where T : struct, IRecordable
        {
        }

        public void LookNullableDeep<T>(ref T? value, string name) where T : struct, IRecordable
        {
        }

        public void LookLink<T>(
            ref T value,
            string name,
            string? slot = null,
            RecordLinkResolveMode resolveMode = RecordLinkResolveMode.Immediate,
            Action<T>? assignLoadedValue = null)
        {
        }
    }
}
