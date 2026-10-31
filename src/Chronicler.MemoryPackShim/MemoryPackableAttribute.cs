//=======================================================================
// MemoryPackableAttribute.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

using System;

namespace MemoryPack;

/// <summary>
/// Marks a type as MemoryPack-compatible when the MemoryPack package is disabled.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface,
    AllowMultiple = false,
    Inherited = false)]
public sealed class MemoryPackableAttribute : Attribute
{
    /// <summary>
    /// Initializes the attribute with the requested generation mode.
    /// </summary>
    public MemoryPackableAttribute(GenerateType generateType = GenerateType.Object)
    {
        GenerateType = generateType;
        SerializeLayout = generateType is GenerateType.VersionTolerant or GenerateType.CircularReference
            ? SerializeLayout.Explicit
            : SerializeLayout.Sequential;
    }

    /// <summary>
    /// Initializes the attribute with the requested serialization layout.
    /// </summary>
    public MemoryPackableAttribute(SerializeLayout serializeLayout)
    {
        GenerateType = GenerateType.Object;
        SerializeLayout = serializeLayout;
    }

    /// <summary>
    /// Initializes the attribute with the requested generation mode and serialization layout.
    /// </summary>
    public MemoryPackableAttribute(GenerateType generateType, SerializeLayout serializeLayout)
    {
        GenerateType = generateType;
        SerializeLayout = serializeLayout;
    }

    /// <summary>
    /// Gets the requested generation mode.
    /// </summary>
    public GenerateType GenerateType { get; }

    /// <summary>
    /// Gets the requested serialization layout.
    /// </summary>
    public SerializeLayout SerializeLayout { get; }
}
