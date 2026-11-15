//=======================================================================
// MemoryPackIgnoreAttribute.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

using System;

namespace MemoryPack;

/// <summary>Carries the field or property exclusion marker.</summary>
/// <remarks>This compatibility attribute preserves metadata in Lean builds.</remarks>
[AttributeUsage(
    AttributeTargets.Field | AttributeTargets.Property,
    AllowMultiple = false,
    Inherited = false)]
public sealed class MemoryPackIgnoreAttribute : Attribute
{
}
