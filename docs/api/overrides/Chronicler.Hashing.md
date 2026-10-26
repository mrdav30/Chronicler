---
uid: Chronicler.Hashing
summary: *content
---

Deterministic record hashes for replay and conformance checks.
<xref:Chronicler.Hashing.ChronicleHashSerializer> traverses the shared
<xref:Chronicler.IRecordable> schema directly, preserving field order,
declared types and defaults, nested records, and stable link identities.

<xref:Chronicler.Hashing.ChronicleHashWriter> lets callers compose explicit
primitive values and record contributions into a
<xref:Chronicler.Hashing.ChronicleHash>. These are non-cryptographic comparison
signals; they are not hashes of a transport payload. JSON and MemoryPack
transports live in <xref:Chronicler.Serialization>.
