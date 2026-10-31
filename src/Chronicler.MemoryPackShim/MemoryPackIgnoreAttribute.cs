//=======================================================================
// MemoryPackIgnoreAttribute.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

using System;

namespace MemoryPack;

/// <summary>
/// Excludes a member from MemoryPack-compatible metadata when the MemoryPack package is disabled.
/// </summary>
[AttributeUsage(
    AttributeTargets.Field | AttributeTargets.Property,
    AllowMultiple = false,
    Inherited = false)]
public sealed class MemoryPackIgnoreAttribute : Attribute
{
}
