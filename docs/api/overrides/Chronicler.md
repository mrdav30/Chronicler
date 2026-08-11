---
uid: Chronicler
summary: *content
---

Chronicler provides deterministic, transport-neutral state transfer for .NET
runtimes that own their object graphs. Types declare names, defaults, order, and
ownership through `IRecordable.RecordData(...)`; the built-in transports apply
that schema to JSON or MemoryPack and populate existing runtime shells.

Use `RecordValues` for leaf data, the deep helpers for owned nested state, and
`RecordLinks` for external or runtime-owned identity. `ChronicleHashSerializer`
walks the same schema to produce replay and conformance signals without hashing
a transport payload.

Chronicler is designed for the deterministic
[Lockstep Simulation Framework](https://github.com/mrdav30/FixedMathSharp)
ecosystem, including
[SwiftCollections](https://github.com/mrdav30/SwiftCollections),
[GridForge](https://github.com/mrdav30/GridForge), and
[Gravitas](https://github.com/mrdav30/Gravitas).
