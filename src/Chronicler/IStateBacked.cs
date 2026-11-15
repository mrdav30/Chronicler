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
    /// <summary>Gets the object's canonical state.</summary>
    /// <value>The state value a consumer can encode and use to reconstruct the object. Ownership follows the implementing type's contract.</value>
    TState State { get; }
}
