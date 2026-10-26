using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Chronicler.Tests;

public class RecordLinkSerializationTests
{
    [Theory]
    [MemberData(nameof(SerializationTransportData.All), MemberType = typeof(SerializationTransportData))]
    public void RoundTrip_ShouldResolveImmediateLinks_UsingResolversAndSlots(SerializationTransport transport)
    {
        var sourcePrimary = new ExternalResource("source-primary");
        var sourceSecondary = new ExternalResource("source-secondary");

        ChronicleContext saveContext = CreateResolverContext(sourcePrimary, sourceSecondary);

        var source = new ResolverLinkRecord
        {
            Name = "alpha",
            Primary = sourcePrimary,
            Secondary = sourceSecondary
        };

        object payload = SerializationTestHarness.Serialize(source, transport, saveContext);

        var loadPrimary = new ExternalResource("load-primary");
        var loadSecondary = new ExternalResource("load-secondary");

        ChronicleContext loadContext = CreateResolverContext(loadPrimary, loadSecondary);

        var target = new ResolverLinkRecord();
        SerializationTestHarness.Populate(target, payload, transport, loadContext);

        target.Name.Should().Be("alpha");
        target.Primary.Should().BeSameAs(loadPrimary);
        target.Secondary.Should().BeSameAs(loadSecondary);
    }

    [Theory]
    [MemberData(nameof(SerializationTransportData.All), MemberType = typeof(SerializationTransportData))]
    public void RoundTrip_ShouldResolveDeferredLinks_AfterGraphLoad(SerializationTransport transport)
    {
        var source = CreateDeferredGraph("shared-link");

        var saveContext = new ChronicleContext();
        saveContext.Links.RegisterInstance("shared-link", source.Provider.Resource, slot: "provider");

        object payload = SerializationTestHarness.Serialize(source, transport, saveContext);

        var target = new DeferredLinkGraph();
        SerializationTestHarness.Populate(target, payload, transport, new ChronicleContext());

        target.Consumer.Label.Should().Be("consumer");
        target.Provider.Id.Should().Be("shared-link");
        target.Consumer.Resource.Should().BeSameAs(target.Provider.Resource);
        target.Consumer.Resource!.Name.Should().Be("loaded-shared-link");
    }

    [Theory]
    [MemberData(nameof(SerializationTransportData.All), MemberType = typeof(SerializationTransportData))]
    public void RoundTrip_ShouldThrow_WhenDeferredLinksRemainUnresolved(SerializationTransport transport)
    {
        var source = CreateDeferredGraph("shared-link");

        var saveContext = new ChronicleContext();
        saveContext.Links.RegisterInstance("shared-link", source.Provider.Resource, slot: "provider");

        object payload = SerializationTestHarness.Serialize(source, transport, saveContext);
        payload = SerializationTestHarness.RemoveEntry(payload, transport, "provider");

        var target = new DeferredLinkGraph();
        Action act = () => SerializationTestHarness.Populate(target, payload, transport, new ChronicleContext());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*resource:ExternalResource:shared-link@provider*");
    }

    [Theory]
    [MemberData(nameof(SerializationTransportData.All), MemberType = typeof(SerializationTransportData))]
    public void Populate_ShouldThrow_WhenImmediateLinkCannotResolve(SerializationTransport transport)
    {
        var sourcePrimary = new ExternalResource("source-primary");
        var sourceSecondary = new ExternalResource("source-secondary");

        ChronicleContext saveContext = CreateResolverContext(sourcePrimary, sourceSecondary);

        var source = new ResolverLinkRecord
        {
            Name = "alpha",
            Primary = sourcePrimary,
            Secondary = sourceSecondary
        };

        object payload = SerializationTestHarness.Serialize(source, transport, saveContext);

        var target = new ResolverLinkRecord();
        Action act = () => SerializationTestHarness.Populate(target, payload, transport, new ChronicleContext());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unable to load link 'primary'*");
    }

    [Theory]
    [MemberData(nameof(SerializationTransportData.All), MemberType = typeof(SerializationTransportData))]
    public void Serialize_ShouldThrow_WhenStableLinkIdCannotBeProduced(SerializationTransport transport)
    {
        var source = new ResolverLinkRecord
        {
            Name = "alpha",
            Primary = new ExternalResource("source-primary")
        };

        Action act = () => SerializationTestHarness.Serialize(source, transport, new ChronicleContext());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unable to save link 'primary'*");
    }

    private static ChronicleContext CreateResolverContext(
        IExternalResource primary,
        IExternalResource secondary)
    {
        var context = new ChronicleContext();
        var primaryResolver = new DictionaryLinkResolver<IExternalResource>();
        var secondaryResolver = new DictionaryLinkResolver<IExternalResource>();
        primaryResolver.Register("primary", primary);
        secondaryResolver.Register("secondary", secondary);
        context.Links.RegisterResolver(primaryResolver);
        context.Links.RegisterResolver(secondaryResolver, slot: "secondary");
        return context;
    }

    private static DeferredLinkGraph CreateDeferredGraph(string id)
    {
        var resource = new ExternalResource("source-resource");
        return new DeferredLinkGraph
        {
            Consumer = new DeferredLinkConsumer
            {
                Label = "consumer",
                Resource = resource
            },
            Provider = new DeferredLinkProvider
            {
                Id = id,
                Resource = resource
            }
        };
    }

    private interface IExternalResource
    {
        string Name { get; }
    }

    private sealed class ExternalResource : IExternalResource
    {
        public ExternalResource(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }

    private sealed class ResolverLinkRecord : IRecordable
    {
        public string Name = string.Empty;

        public IExternalResource? Primary;

        public IExternalResource? Secondary;

        public void RecordData(IChronicler chronicler)
        {
            string name = Name;
            IExternalResource? primary = Primary;
            IExternalResource? secondary = Secondary;

            RecordValues.Look(chronicler, ref name, "name", string.Empty);
            RecordLinks.Look(chronicler, ref primary, "primary");
            RecordLinks.Look(chronicler, ref secondary, "secondary", slot: "secondary");

            if (chronicler.Mode == SerializationMode.Loading)
            {
                Name = name;
                Primary = primary;
                Secondary = secondary;
            }
        }
    }

    private sealed class DeferredLinkGraph : IRecordable
    {
        public DeferredLinkConsumer Consumer = new();

        public DeferredLinkProvider Provider = new();

        public void RecordData(IChronicler chronicler)
        {
            DeferredLinkConsumer consumer = Consumer;
            DeferredLinkProvider provider = Provider;

            RecordDeep.Look(chronicler, ref consumer, "consumer");
            RecordDeep.Look(chronicler, ref provider, "provider");

            if (chronicler.Mode == SerializationMode.Loading)
            {
                Consumer = consumer;
                Provider = provider;
            }
        }
    }

    private sealed class DeferredLinkConsumer : IRecordable
    {
        public string Label = string.Empty;

        public ExternalResource? Resource;

        public void RecordData(IChronicler chronicler)
        {
            string label = Label;
            ExternalResource? resource = Resource;

            RecordValues.Look(chronicler, ref label, "label", string.Empty);
            RecordLinks.LookDeferred(
                chronicler,
                resource,
                "resource",
                resolved => Resource = resolved,
                slot: "provider");

            if (chronicler.Mode == SerializationMode.Loading)
                Label = label;
        }
    }

    private sealed class DeferredLinkProvider : IRecordable
    {
        public string Id = string.Empty;

        public ExternalResource? Resource;

        public void RecordData(IChronicler chronicler)
        {
            string id = Id;

            RecordValues.Look(chronicler, ref id, "id", string.Empty);

            if (chronicler.Mode == SerializationMode.Loading)
            {
                Id = id;
                Resource = new ExternalResource($"loaded-{id}");
                chronicler.Context.Links.RegisterInstance(id, Resource, slot: "provider");
            }
        }
    }

    private sealed class DictionaryLinkResolver<T> : IRecordLinkResolver<T>
    {
        private readonly Dictionary<string, T> _values = new(StringComparer.Ordinal);

        public void Register(string id, T value)
        {
            _values[id] = value;
        }

        public bool TryGetReferenceId(T value, [NotNullWhen(true)] out string? id)
        {
            foreach (KeyValuePair<string, T> pair in _values)
            {
                if (!ValuesMatch(pair.Value, value))
                    continue;

                id = pair.Key;
                return true;
            }

            id = string.Empty;
            return false;
        }

        public bool TryResolveReference(string id, [MaybeNullWhen(false)] out T value)
        {
            if (_values.TryGetValue(id, out T? resolvedValue))
            {
                value = resolvedValue!;
                return true;
            }

            value = default!;
            return false;
        }

        private static bool ValuesMatch(T left, T right)
        {
            if (typeof(T).IsValueType)
                return EqualityComparer<T>.Default.Equals(left, right);

            return ReferenceEquals(left, right);
        }
    }
}
