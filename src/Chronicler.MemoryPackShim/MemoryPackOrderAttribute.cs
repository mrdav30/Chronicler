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
    /// <summary>Records the member's declared position.</summary>
    /// <param name="order">The order metadata to store as supplied.</param>
    public MemoryPackOrderAttribute(int order)
    {
        Order = order;
    }

    /// <summary>Gets the member order metadata.</summary>
    /// <value>The integer passed to the attribute constructor.</value>
    public int Order { get; }
}
