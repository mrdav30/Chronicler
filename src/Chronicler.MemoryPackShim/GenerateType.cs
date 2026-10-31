//=======================================================================
// GenerateType.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

namespace MemoryPack;

/// <summary>
/// Selects the source-generation mode represented by MemoryPack-compatible metadata.
/// </summary>
public enum GenerateType
{
    /// <summary>
    /// Generates ordinary object serialization metadata.
    /// </summary>
    Object,

    /// <summary>
    /// Generates version-tolerant serialization metadata.
    /// </summary>
    VersionTolerant,

    /// <summary>
    /// Generates circular-reference serialization metadata.
    /// </summary>
    CircularReference,

    /// <summary>
    /// Generates collection serialization metadata.
    /// </summary>
    Collection,

    /// <summary>
    /// Suppresses source generation.
    /// </summary>
    NoGenerate
}
