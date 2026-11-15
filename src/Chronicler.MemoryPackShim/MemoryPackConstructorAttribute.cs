//=======================================================================
// MemoryPackConstructorAttribute.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

using System;

namespace MemoryPack;

/// <summary>Carries the marker identifying the intended constructor.</summary>
/// <remarks>This compatibility attribute preserves metadata in Lean builds.</remarks>
[AttributeUsage(
    AttributeTargets.Constructor,
    AllowMultiple = false,
    Inherited = false)]
public sealed class MemoryPackConstructorAttribute : Attribute
{
}
