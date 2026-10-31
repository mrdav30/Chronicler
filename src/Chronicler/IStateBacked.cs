//=======================================================================
// IStateBacked.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

namespace Chronicler;

/// <summary>
/// Exposes a canonical serializable state value for an object.
/// </summary>
/// <typeparam name="TState">The type that represents the object's serializable state.</typeparam>
public interface IStateBacked<out TState>
{
    /// <summary>
    /// Gets the canonical serializable state for this object.
    /// </summary>
    TState State { get; }
}
