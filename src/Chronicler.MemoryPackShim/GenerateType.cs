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
    /// <summary>Selects ordinary object encoding.</summary>
    Object,

    /// <summary>Selects a version-tolerant object layout.</summary>
    VersionTolerant,

    /// <summary>Selects the circular-reference mode.</summary>
    CircularReference,

    /// <summary>Selects collection encoding.</summary>
    Collection,

    /// <summary>Marks a type for which generation is suppressed.</summary>
    NoGenerate
}
