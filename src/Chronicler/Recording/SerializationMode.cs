//=======================================================================
// SerializationMode.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

namespace Chronicler;

/// <summary>
/// Describes whether a chronicler pass is saving values out or loading values in.
/// </summary>
public enum SerializationMode
{
    /// <summary>
    /// The chronicler is saving values out.
    /// </summary>
    Saving,
    /// <summary>
    /// The chronicler is loading values in.
    /// </summary>
    Loading
}
