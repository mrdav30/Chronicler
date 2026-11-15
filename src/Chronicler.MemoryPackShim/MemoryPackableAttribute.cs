//=======================================================================
// MemoryPackableAttribute.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

using System;

namespace MemoryPack;

/// <summary>Carries MemoryPack-compatible generation and layout metadata.</summary>
/// <remarks>The shim stores attribute metadata for Lean builds. Serialization requires the full MemoryPack implementation.</remarks>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface,
    AllowMultiple = false,
    Inherited = false)]
public sealed class MemoryPackableAttribute : Attribute
{
    /// <summary>Records a generation mode and selects its default layout.</summary>
    /// <param name="generateType">Defaults to ordinary object mode. Version-tolerant and circular-reference modes select explicit layout.</param>
    /// <remarks>Other modes select sequential layout.</remarks>
    public MemoryPackableAttribute(GenerateType generateType = GenerateType.Object)
    {
        GenerateType = generateType;
        SerializeLayout = generateType is GenerateType.VersionTolerant or GenerateType.CircularReference
            ? SerializeLayout.Explicit
            : SerializeLayout.Sequential;
    }

    /// <summary>Records a layout with ordinary object generation metadata.</summary>
    /// <param name="serializeLayout">The layout value to preserve on the attribute.</param>
    public MemoryPackableAttribute(SerializeLayout serializeLayout)
    {
        GenerateType = GenerateType.Object;
        SerializeLayout = serializeLayout;
    }

    /// <summary>Records an explicit generation mode and layout pair.</summary>
    /// <param name="generateType">The mode metadata to preserve.</param>
    /// <param name="serializeLayout">The layout metadata to preserve independently of the generation mode.</param>
    public MemoryPackableAttribute(GenerateType generateType, SerializeLayout serializeLayout)
    {
        GenerateType = generateType;
        SerializeLayout = serializeLayout;
    }

    /// <summary>Gets the recorded generation mode.</summary>
    /// <value>The supplied value, or ordinary object mode when omitted.</value>
    public GenerateType GenerateType { get; }

    /// <summary>Gets the recorded member layout.</summary>
    /// <value>The supplied layout or the default associated with the selected generation mode.</value>
    public SerializeLayout SerializeLayout { get; }
}
