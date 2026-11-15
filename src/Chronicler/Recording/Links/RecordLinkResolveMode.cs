//=======================================================================
// RecordLinkResolveMode.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

namespace Chronicler;

/// <summary>
/// Controls when a recorded link must resolve during population.
/// </summary>
public enum RecordLinkResolveMode
{
    /// <summary>
    /// Resolve the link while reading its field. An unresolved ID fails the load at that field.
    /// </summary>
    Immediate,

    /// <summary>
    /// Resolve the link when its field is read if possible. Otherwise queue it for resolution after the graph loads.
    /// </summary>
    Deferred
}
