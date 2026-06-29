# Record Hashes

Chronicler record hashes provide a deterministic comparison signal for replay,
snapshot, and conformance checks. They traverse `IRecordable.RecordData(...)`
directly through a hash backend instead of serializing to JSON, MemoryPack, or
another transport first.

Record hashes are not cryptographic hashes and are not transport payloads. Use
them to answer questions such as "did two peers record the same state schema and
values in the same order?"

## Public API

- `ChronicleHash` is a 128-bit value with `Low`, `High`, equality, and a
  lowercase 32-character hexadecimal string format.
- `ChronicleHashWriter` is a mutable primitive writer for domain-owned hash
  streams.
- `ChronicleHashSerializer.Compute(...)` hashes an `IRecordable` graph.
- `ChronicleHashSerializer.Contribute(...)` embeds an `IRecordable` graph into
  a caller-owned `ChronicleHashWriter`.

## Writer Contract

`ChronicleHashWriter` writes deterministic primitive bytes:

- integral primitives are little-endian.
- `char` is a UTF-16 code unit.
- `string` is a nullable marker, length, then UTF-16 code units.
- enum values are written by their underlying integral bytes.
- sections require non-empty ASCII tags plus an integer schema version.

The writer is dependency-free and allocation-free for warmed primitive write
paths. It is suitable for higher-level libraries that need to compose their own
domain hashes before or after embedding Chronicler recordable state.

## Record Traversal Contract

The hash backend uses `SerializationMode.Saving` and follows the order of
`RecordData(...)` calls exactly. It does not sort fields. This makes field order,
field names, declared defaults, nested presence, and stable link IDs part of the
hash contract.

`RecordValues.Look(...)` hashes:

- field name.
- declared leaf type.
- supported leaf category.
- current leaf value.
- declared default value.

Supported leaf values are:

- `bool`
- `byte`
- `sbyte`
- `short`
- `ushort`
- `int`
- `uint`
- `long`
- `ulong`
- `char`
- `string`
- enums with 1, 2, 4, or 8 byte underlying values.

Unsupported leaf values throw `NotSupportedException` with the field name and
declared type. Floating-point values are intentionally not supported by the
generic hash backend; deterministic fixed-point packages should provide their
own writer extensions.

`RecordDeep`, `RecordDeepStruct`, and `RecordNullableDeep` hash declared type
names, presence where applicable, and nested `RecordData(...)` payloads with
explicit record boundaries.

`RecordLinks` hashes the field name, declared type, normalized slot, resolve
mode, and stable link ID resolved through `ChronicleContext.Links`. Non-null
links without a stable ID throw instead of falling back to object identity.

## Usage

For a standalone recordable graph:

```csharp
ChronicleHash hash = ChronicleHashSerializer.Compute(snapshot);
```

When stable links are involved, use the same context registration model as
transport serialization:

```csharp
ChronicleContext context = new();
context.Links.RegisterInstance("player-42", playerEntity);

ChronicleHash hash = ChronicleHashSerializer.Compute(actorState, context);
```

For larger domain hashes:

```csharp
var writer = new ChronicleHashWriter();
writer.WriteSection("world.replay", 1);
writer.WriteInt32(frameNumber);
ChronicleHashSerializer.Contribute(snapshot, context, ref writer);

ChronicleHash hash = writer.ToHash();
```

Use explicit sections around domain-owned payloads so schema changes are
intentional and reviewable.
