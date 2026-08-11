---
title: Deterministic record hashes
description: Compare Chronicler state for replay, snapshot, and conformance checks without hashing a transport payload.
---

# Deterministic record hashes

Chronicler record hashes answer a focused question: did two runtimes record the
same schema and values in the same order?

They walk <xref:Chronicler.IRecordable.RecordData(Chronicler.IChronicler)>
through a hash backend. They do not serialize to JSON or MemoryPack first, so a
transport's formatting and envelope details do not become part of the signal.

<!-- prettier-ignore -->
> [!IMPORTANT]
> A record hash is a deterministic comparison signal. It is not a serialized
> payload and it is not a cryptographic hash.

## Start with a recordable graph

Use <xref:Chronicler.ChronicleHashSerializer.Compute(Chronicler.IRecordable)>
for a standalone graph:

```csharp
ChronicleHash hash = ChronicleHashSerializer.Compute(snapshot);
```

When the graph contains stable links, supply a context with the same
registration model used by the transports:

```csharp
ChronicleContext context = new();
context.Links.RegisterInstance("player-42", playerEntity);

ChronicleHash hash = ChronicleHashSerializer.Compute(actorState, context);
```

## Compose a domain-owned hash

Use <xref:Chronicler.ChronicleHashWriter> when a replay or simulation hash needs
domain metadata around a recordable subtree:

```csharp
var writer = new ChronicleHashWriter();
writer.WriteSection("world.replay", 1);
writer.WriteInt32(frameNumber);
ChronicleHashSerializer.Contribute(snapshot, context, ref writer);

ChronicleHash hash = writer.ToHash();
```

Explicit sections give each domain-owned payload a tag and schema version, so a
hash-contract change is intentional and reviewable.

## What becomes part of the contract?

The hash backend uses `SerializationMode.Saving` and follows `RecordData(...)`
calls exactly. It does not sort fields. These details affect the result:

- field order and field names;
- declared leaf types and default values;
- nested type names and record boundaries;
- optional nested-value presence;
- link slots, resolution mode, and stable IDs.

Changing any of them is a hash-contract change, even if the resulting runtime
state appears equivalent.

### Leaf values

`RecordValues.Look(...)` supports:

- `bool`, `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, and
  `ulong`;
- `char` and `string`;
- enums with 1-, 2-, 4-, or 8-byte underlying values.

Unsupported leaf types throw `NotSupportedException` with the field name and
declared type. Floating-point values are intentionally absent from the generic
hash backend. A deterministic numeric library can contribute its representation
through writer extensions instead.

### Owned state and stable links

`RecordDeep`, `RecordDeepStruct`, and `RecordNullableDeep` add declared type
names, presence where applicable, and explicit nested-record boundaries.

`RecordLinks` adds the field name, declared type, normalized slot, resolution
mode, and stable ID. A non-null link without a registered stable ID throws; the
hash never falls back to process-local object identity.

## Primitive byte contract

<xref:Chronicler.ChronicleHashWriter> writes deterministic primitive bytes:

- integral values are little-endian;
- `char` is written as one UTF-16 code unit;
- `string` is a nullable marker, length, then UTF-16 code units;
- enum values use their underlying integral bytes;
- section tags must be non-empty ASCII text and include an integer schema
  version.

The warmed primitive write paths are dependency-free and allocation-free. This
makes the writer suitable for higher-level libraries that need to compose their
own deterministic signals around Chronicler-owned state.

## Result format

<xref:Chronicler.ChronicleHash> is a 128-bit value with `Low` and `High`
components, value equality, and a lowercase 32-character hexadecimal string
format.

For the broader ownership model, continue with
[Values, owned state, and links](serialization-model.md).
