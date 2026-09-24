# Chronicler Migration Guide

## Migrating To v1.0.0

Chronicler v1.0.0 groups transports and hash APIs into focused namespaces. Update
imports or fully qualified names for the moved types, then rebuild consuming
libraries and applications together. A namespace change changes a CLR type's
identity; an assembly compiled against the previous name must be rebuilt.

### Upgrade Checklist

- Update Chronicler package references across consuming libraries and applications
  to v1.0.0, keeping the appropriate standard or Lean package.
- Add `using Chronicler.Serialization;` where you use serializers, converters,
  or payload editing.
- Add `using Chronicler.Hashing;` where you use hash values, writers, or record
  traversal, including downstream hash writer extensions.
- Update fully qualified names and stored reflection names for moved types.
- Rebuild consumers together; the old root type names have no compatibility
  wrappers.
- Re-run save/load and replay/conformance tests. Existing hash vectors should
  remain unchanged for unchanged recorded graphs; audit any downstream record
  type renames separately.

### Serialization And Hashing Namespaces

Each type below previously lived directly in `Chronicler`:

| Type | New namespace |
| --- | --- |
| `JsonRecordSerializer` | `Chronicler.Serialization` |
| `MemoryPackRecordSerializer` | `Chronicler.Serialization` |
| `StateJsonConverter<TRecord, TState>` | `Chronicler.Serialization` |
| `StateJsonConverterFactory` | `Chronicler.Serialization` |
| `SerializationPayloadEditor` | `Chronicler.Serialization` |
| `ChronicleHash` | `Chronicler.Hashing` |
| `ChronicleHashWriter` | `Chronicler.Hashing` |
| `ChronicleHashSerializer` | `Chronicler.Hashing` |

Import the domains your code uses:

```csharp
using Chronicler;               // Recording contracts, helpers, context, links.
using Chronicler.Serialization; // Transports, converters, payload editing.
using Chronicler.Hashing;       // Hash values, writers, and record traversal.
using Chronicler.Timing;        // Time values and the host-advanced clock.
```

For example, a file that previously imported only `Chronicler` and called
`JsonRecordSerializer.Serialize(...)` now also imports
`Chronicler.Serialization`. Hash writer extensions and public APIs accepting or
returning hash types need the new `Chronicler.Hashing` identities as well.
Update any explicitly stored reflection type names for moved types.

The old root names have no compatibility wrappers. Method names and behavior
are unchanged; this migration does not redesign the payload-editor API.

### Shared Contracts And Packages

`IRecordable`, `IChronicler`, `IStateBacked<TState>`, all `Record*` helpers,
`SerializationMode`, context, link services, and `DefaultSaver` remain in
`Chronicler`. In particular, `RecordChronicleTime` remains alongside the other
recording helpers. Time values and `ChronicleClock` retain `Chronicler.Timing`.

This organization adds no packages or assemblies. Continue using
`Chronicler.Core` or `Chronicler.Core.Lean`. The assembly name remains
`Chronicler`; target frameworks remain `netstandard2.1` and `net8.0`.
MemoryPack transport APIs remain available only in the standard package.

`Chronicler.MemoryPackShim` remains a separate compatibility package whose
attributes use the `MemoryPack` namespace. It does not provide a serializer.

### Payloads And Deterministic Hashes

The reorganization preserves field names, record order, declared defaults,
link semantics, and the built-in transport encoding. Existing payloads do not
need rewriting because the serializer classes moved namespaces.

The hash implementation also preserves its existing stream for unchanged
recorded graphs. However, recorded type identity is part of that stream:

- the root record contributes its runtime type name;
- fields and deep records contribute their declared type names;
- generic type identities include their argument type names;
- nested recordable carriers contribute their own type identities.

Renaming or moving one of those recorded types can change hashes even when its
fields and values are unchanged. This includes types declared for links.
Moving only a source file, with its namespace and containing type unchanged,
does not change that identity.

`RecordChronicleTime` retains its nested `TimeRecord` identity, and
`ChronicleClock` retains its existing namespace and schema. Their existing hash
golden vectors remain unchanged. Downstream namespace reorganizations should
review their own recorded types separately and version replay/conformance
contracts deliberately when those identities change.

See [Deterministic record hashes](api/guides/record-hashes.md) for the complete hash
contract and [Values, owned state, and links](api/guides/serialization-model.md) for the
shared state model.
