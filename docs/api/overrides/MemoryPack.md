---
uid: MemoryPack
summary: *content
---

This namespace contains the public compatibility attributes supplied by
`Chronicler.MemoryPackShim`. They let Lean libraries retain MemoryPack
annotations in public metadata without referencing the real MemoryPack package.

The shim does not serialize or deserialize data. Use `Chronicler.Core` when you
need Chronicler's built-in `MemoryPackRecordSerializer` transport.
