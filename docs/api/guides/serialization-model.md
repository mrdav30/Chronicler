---
title: Values, owned state, and links
description: Understand Chronicler's explicit schema, runtime-shell ownership, defaults, and stable references.
---

# Values, owned state, and links

Chronicler separates state by ownership. That distinction keeps schemas explicit
and prevents a restore pass from quietly taking control of a runtime's object
lifecycle.

## One schema, ordered by code

Every recordable type implements
<xref:Chronicler.IRecordable.RecordData(Chronicler.IChronicler)>:

```csharp
public void RecordData(IChronicler chronicler)
{
    RecordValues.Look(chronicler, ref Health, "health", 100);
    RecordDeep.Look(chronicler, ref Inventory, "inventory");
    RecordLinks.Look(chronicler, ref Target, "target");
}
```

Saving and loading run this method in the same order. Field names, declared
defaults, and the choice of recording helper are durable parts of the schema.
The transports do not discover members with reflection or infer ownership from
the object graph.

## Pick a lane by ownership

### Values

Use <xref:Chronicler.RecordValues> for leaf state. Pass the canonical default
explicitly:

```csharp
RecordValues.Look(chronicler, ref Health, "health", 100);
```

When `health` is absent during loading, the field becomes `100`. The result does
not depend on the target object's previous value.

### Owned nested state

Use the deep helpers when the current object owns the nested state:

- <xref:Chronicler.RecordDeep> for a recordable class;
- <xref:Chronicler.RecordDeepStruct> for a non-nullable recordable struct;
- <xref:Chronicler.RecordNullableDeep> for a nullable recordable struct.

For class state, initialize the nested object before loading:

```csharp
public InventoryState Inventory = new();

public void RecordData(IChronicler chronicler)
{
    RecordDeep.Look(chronicler, ref Inventory, "inventory");
}
```

The host remains responsible for construction, dependency injection, pooling,
and runtime registration. Chronicler only transfers the recorded state.

### Runtime-owned or external links

Use <xref:Chronicler.RecordLinks> when a field points to an object that should
not be serialized inline. A <xref:Chronicler.ChronicleContext> carries the
session's <xref:Chronicler.ChronicleLinkRegistry>:

```csharp
public sealed class ActorState : IRecordable
{
    public RuntimeEntity? Target;

    public void RecordData(IChronicler chronicler)
    {
        RecordLinks.Look(chronicler, ref Target, "target");
    }
}

ChronicleContext context = new();
context.Links.RegisterInstance("player-42", runtimeEntity);

string json = JsonRecordSerializer.Serialize(actorState, context);
JsonRecordSerializer.Populate(restoredActorState, json, context);
```

The registry maps a stable ID to the runtime instance and can also use a custom
<xref:Chronicler.IRecordLinkResolver`1>. Optional `slot` values let one runtime
type use multiple independent identity domains.

Immediate links must resolve during the load pass. If the referenced object is
registered later in the graph, use `RecordLinks.LookDeferred(...)` with an
assignment callback. The serializer resolves queued links after the graph has
loaded and throws if any remain unresolved.

## Transport behavior

<xref:Chronicler.JsonRecordSerializer> and, in the standard package,
<xref:Chronicler.MemoryPackRecordSerializer> implement the same
<xref:Chronicler.IChronicler> contract. They both:

- call the type-owned schema directly;
- populate an existing target;
- apply declared defaults for missing values;
- use a context for stable links;
- resolve deferred links after loading.

Transport payloads are not intended to be interchangeable. The shared contract
is the state behavior expressed by `RecordData(...)`.

## State-backed JSON types

<xref:Chronicler.IStateBacked`1> covers a separate System.Text.Json integration
for helper objects that expose one canonical state value. Register
<xref:Chronicler.StateJsonConverterFactory> when each record type implements one
`IStateBacked<TState>` contract and has a public constructor accepting that
exact state type:

```csharp
var options = new JsonSerializerOptions();
options.Converters.Add(new StateJsonConverterFactory());
```

The converter writes an object with a single `State` property and reconstructs
the helper from that state constructor. This is not the same as populating an
`IRecordable` runtime shell; use it only when construction from canonical state
is the intended contract.

## Payload editing

<xref:Chronicler.SerializationPayloadEditor> can remove or replace entries in a
serialized payload. It is useful for compatibility tests and controlled schema
migrations. Its common overloads operate on JSON in Lean builds and default to
MemoryPack in the standard build, so prefer the format-specific methods when
call-site clarity matters.

## Package boundaries

| Package                     | Public surface                                                                                                                 |
| --------------------------- | ------------------------------------------------------------------------------------------------------------------------------ |
| `Chronicler.Core`           | Core recording APIs, JSON, MemoryPack, record hashes, link services, and payload editing for both built-in formats.            |
| `Chronicler.Core.Lean`      | Core recording APIs, JSON, record hashes, link services, and JSON payload editing. MemoryPack-specific source is compiled out. |
| `Chronicler.MemoryPackShim` | Compatibility attributes in the `MemoryPack` namespace for annotated Lean assemblies. It does not serialize data.              |

Continue with [Deterministic record hashes](record-hashes.md) when the goal is
state comparison rather than transport.
