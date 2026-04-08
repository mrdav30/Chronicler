using MemoryPack;
using SwiftCollections;
using System;

namespace Chronicler;

/// <summary>
/// Stores named field payloads for the MemoryPack chronicler transport.
/// </summary>
[MemoryPackable]
internal sealed partial class MemoryPackRecordEnvelope
{
    /// <summary>
    /// Serialized payloads keyed by record name.
    /// </summary>
    public SwiftDictionary<string, byte[]> Entries { get; set; } = new(8, StringComparer.Ordinal);
}
