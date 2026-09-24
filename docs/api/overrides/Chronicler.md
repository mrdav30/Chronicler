---
uid: Chronicler
summary: *content
---

Chronicler provides deterministic, transport-neutral state transfer for .NET
runtimes that own their object graphs. Types declare names, defaults, order, and
ownership through `IRecordable.RecordData(...)`; the built-in transports apply
that schema to JSON or MemoryPack and populate existing runtime shells.

Use `RecordValues` for leaf data, the deep helpers for owned nested state, and
`RecordLinks` for external or runtime-owned identity. The context and link
registry are shared by transports and hashes. `IStateBacked<TState>` exposes
canonical helper state independently of a transport.

Use <xref:Chronicler.Serialization> for JSON and MemoryPack transports,
state-backed JSON converters, and payload editing. Use <xref:Chronicler.Hashing>
for hash values, primitive writing, and `ChronicleHashSerializer`, which walks
the same recording schema to produce replay and conformance signals.

<xref:Chronicler.DefaultSaver> provides explicit save/apply lifecycle hooks for
host integrations, such as settings and configuration edited in a game engine.
The host invokes each phase; Chronicler takes no engine dependency.

The <xref:Chronicler.Timing> namespace provides immutable timestamps and durations
with 64-bit whole seconds and 32 binary fractional bits, plus a host-advanced
`ChronicleClock`. `RecordChronicleTime` records the values explicitly; clock
population validates all state before mutation. Hosts retain simulation-loop
and reset/restore lifetime ownership.

Chronicler is designed for the deterministic
[Lockstep Simulation Framework](https://github.com/mrdav30/FixedMathSharp)
ecosystem, including
[SwiftCollections](https://github.com/mrdav30/SwiftCollections),
[GridForge](https://github.com/mrdav30/GridForge), and
[Gravitas](https://github.com/mrdav30/Gravitas).
