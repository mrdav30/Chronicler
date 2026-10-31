//=======================================================================
// SerializeLayout.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

namespace MemoryPack;

/// <summary>
/// Selects the member layout represented by MemoryPack-compatible metadata.
/// </summary>
public enum SerializeLayout
{
    /// <summary>
    /// Uses sequential member layout.
    /// </summary>
    Sequential,

    /// <summary>
    /// Uses explicit member layout.
    /// </summary>
    Explicit
}
