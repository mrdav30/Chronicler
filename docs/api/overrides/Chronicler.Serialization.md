---
uid: Chronicler.Serialization
summary: *content
---

Transports and adapters for Chronicler's explicit state model.
<xref:Chronicler.Serialization.JsonRecordSerializer> and, in the standard
package, <xref:Chronicler.Serialization.MemoryPackRecordSerializer> apply the
shared <xref:Chronicler.IRecordable> schema to existing runtime shells.

State-backed JSON converters use <xref:Chronicler.IStateBacked`1> when
reconstruction from canonical state is the intended contract.
<xref:Chronicler.Serialization.SerializationPayloadEditor> supports controlled
payload editing. Recording contracts, context, and links remain in
<xref:Chronicler>; hash APIs live in <xref:Chronicler.Hashing>.
