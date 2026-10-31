//=======================================================================
// MemoryPackOrderAttribute.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

using System;

namespace MemoryPack;

/// <summary>
/// Declares a member's MemoryPack-compatible order when the MemoryPack package is disabled.
/// </summary>
[AttributeUsage(
    AttributeTargets.Field | AttributeTargets.Property,
    AllowMultiple = false,
    Inherited = false)]
public sealed class MemoryPackOrderAttribute : Attribute
{
    /// <summary>
    /// Initializes the attribute with the requested member order.
    /// </summary>
    public MemoryPackOrderAttribute(int order)
    {
        Order = order;
    }

    /// <summary>
    /// Gets the requested member order.
    /// </summary>
    public int Order { get; }
}
