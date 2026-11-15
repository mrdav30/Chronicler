//=======================================================================
// IRecordLinkResolver.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

using System.Diagnostics.CodeAnalysis;

namespace Chronicler;

/// <summary>
/// Maps host-owned link values to stable IDs and resolves those IDs during loading.
/// </summary>
/// <typeparam name="T">The declared link type for which the resolver is registered.</typeparam>
/// <remarks>
/// Use IDs that identify corresponding values across save and load sessions.
/// A false result allows <see cref="ChronicleLinkRegistry"/> to consult instance registrations.
/// </remarks>
public interface IRecordLinkResolver<T>
{
    /// <summary>
    /// Looks up an ID for a value in the current session.
    /// </summary>
    /// <param name="value">The host-owned value to identify.</param>
    /// <param name="id">A non-null stable ID on success. The registry ignores this output on failure.</param>
    /// <returns><see langword="true"/> if the resolver supplied an ID.</returns>
    bool TryGetReferenceId(T value, [NotNullWhen(true)] out string? id);

    /// <summary>
    /// Looks up a recorded ID in the destination session.
    /// </summary>
    /// <param name="id">The stable ID read from the record.</param>
    /// <param name="value">The destination value on success. The registry ignores this output on failure.</param>
    /// <returns><see langword="true"/> if the resolver supplied a value.</returns>
    bool TryResolveReference(string id, [MaybeNullWhen(false)] out T value);
}
