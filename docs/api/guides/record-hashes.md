---
title: Deterministic record hashes
description: Compare Chronicler state for replay, snapshot, and conformance checks without hashing a transport payload.
---

# Deterministic record hashes

Check that a restored player matches the recorded state before resuming a
replay. Compute a hash for each player. The hashes match at 72 health and 30
rounds. Use the comparison again after firing one round to see the recorded
state diverge.

<!-- prettier-ignore -->
> [!IMPORTANT]
> Use record hashes for deterministic state comparisons. Use the transport
> payload to restore state and a cryptographic hash for security-sensitive checks.

## Start with a recordable graph

Use
<xref:Chronicler.Hashing.ChronicleHashSerializer.Compute(Chronicler.IRecordable)>
for a standalone graph. Start with the [player walkthrough](getting-started.md).
Add `using Chronicler.Hashing;` at the top of `Program.cs` and these lines after
the restore, before the type declarations.

```csharp
ChronicleHash savedState = ChronicleHashSerializer.Compute(source);
ChronicleHash restoredState = ChronicleHashSerializer.Compute(restored);
Console.WriteLine(savedState == restoredState); // True

restored.Weapon.Ammo--;
Console.WriteLine(savedState == ChronicleHashSerializer.Compute(restored)); // False
```

`Compute` runs <xref:Chronicler.IRecordable.RecordData(Chronicler.IChronicler)>
directly. In the example both calls include the recorded ammo value of 30. JSON
omission, indentation, and MemoryPack envelopes are outside the hash input.

When comparing the
[actor with a stable target](serialization-model.md#runtime-owned-or-external-links),
supply each graph's context. In that example, add `using Chronicler.Hashing;`
and insert the comparison before the type declarations. Both contexts map their
local target to `player-42`, so these actor hashes match.

```csharp
ChronicleHash before = ChronicleHashSerializer.Compute(actorState, saveContext);
ChronicleHash after = ChronicleHashSerializer.Compute(restoredActorState, loadContext);
Console.WriteLine(before == after); // True
```

## Compose a domain-owned hash

Use <xref:Chronicler.Hashing.ChronicleHashWriter> when a replay or simulation
hash needs domain metadata around recorded state. For a replay checkpoint at
frame 120, write the section tag, version, and frame before contributing
`source` from the player walkthrough. Use the same sequence and version in every
runtime.

```csharp
var writer = new ChronicleHashWriter();
writer.WriteSection("world.replay", 1);
writer.WriteInt32(120);
ChronicleHashSerializer.Contribute(source, ref writer);

ChronicleHash hash = writer.ToHash();
```

The resulting hash covers the checkpoint metadata and `source`. Increment the
section version when you change the fields or their encoding.

## What becomes part of the contract?

The hash backend uses `SerializationMode.Saving` and processes fields in
`RecordData` call order. These details affect the result.

- field order and field names;
- declared leaf types and default values;
- nested type names and record boundaries;
- optional nested-value presence;
- link slots, resolution mode, and stable IDs.

Changing any of them is a hash-contract change, even if the resulting runtime
state appears equivalent.

Recorded type identities include their full names and generic arguments, so
moving a recorded type to another namespace or containing type can change its
hash. Moving a source file alone preserves that identity. See the
[migration guide](https://github.com/mrdav30/Chronicler/blob/main/docs/MIGRATION.md)
when upgrading existing consumers.

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
mode, and stable ID. If a non-null link's registry cannot supply an ID,
`Compute` throws `InvalidOperationException`.

## Primitive byte contract

<xref:Chronicler.Hashing.ChronicleHashWriter> writes deterministic primitive
bytes:

- integral values are little-endian;
- `char` is written as one UTF-16 code unit;
- `string` is a presence byte (0 for null, 1 for present), then, when present,
  an `Int32` code-unit count and the UTF-16 code units;
- enum values use their underlying integral bytes;
- section tags must be non-empty ASCII text and include an integer schema
  version.

The warmed primitive write paths are dependency-free and allocation-free. This
makes the writer suitable for higher-level libraries that need to compose their
own deterministic signals around Chronicler-owned state.

## Result format

<xref:Chronicler.Hashing.ChronicleHash> is a 128-bit value with `Low` and `High`
components and value equality. `ToString()` returns 32 lowercase hexadecimal
characters, with the high 64 bits before the low 64 bits.

For the broader ownership model, continue with
[Values, owned state, and links](serialization-model.md).
