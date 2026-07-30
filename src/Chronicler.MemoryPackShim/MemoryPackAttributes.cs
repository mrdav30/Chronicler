using System;

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

/// <summary>
/// Includes a member in MemoryPack-compatible metadata when the MemoryPack package is disabled.
/// </summary>
[AttributeUsage(
    AttributeTargets.Field | AttributeTargets.Property,
    AllowMultiple = false,
    Inherited = false)]
public sealed class MemoryPackIncludeAttribute : Attribute
{
}

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

/// <summary>
/// Marks the constructor used by MemoryPack-compatible metadata when the MemoryPack package is disabled.
/// </summary>
[AttributeUsage(
    AttributeTargets.Constructor,
    AllowMultiple = false,
    Inherited = false)]
public sealed class MemoryPackConstructorAttribute : Attribute
{
}

/// <summary>
/// Allows MemoryPack-compatible metadata on a type or member when the MemoryPack package is disabled.
/// </summary>
[AttributeUsage(
    AttributeTargets.Field | AttributeTargets.Property,
    AllowMultiple = false,
    Inherited = false)]
public sealed class MemoryPackAllowSerializeAttribute : Attribute
{
}

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
