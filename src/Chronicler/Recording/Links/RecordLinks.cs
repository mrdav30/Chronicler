//=======================================================================
// RecordLinks.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

using System;

namespace Chronicler;

/// <summary>
/// Records stable IDs for links to host-owned values.
/// </summary>
public static class RecordLinks
{
    /// <summary>
    /// Records a link that must resolve as soon as it is read.
    /// </summary>
    /// <typeparam name="T">The declared type used for registry lookup.</typeparam>
    /// <param name="chronicler">The active pass supplied to <see cref="IRecordable.RecordData"/>.</param>
    /// <param name="value">
    /// The value to identify on save. On load, receives the resolved value or the default for a missing or null entry.
    /// An unresolved ID leaves this value unchanged.
    /// </param>
    /// <param name="name">The stable field name to use on both save and load.</param>
    /// <param name="slot">An optional identity domain. Null or empty selects the default slot.</param>
    /// <exception cref="InvalidOperationException">
    /// A non-null value has no stable ID on save, or a present link ID cannot resolve on load.
    /// </exception>
    public static void Look<T>(IChronicler chronicler, ref T value, string name, string? slot = null)
    {
        chronicler.LookLink(ref value, name, slot);
    }

    /// <summary>
    /// Records a link whose assignment may wait until the graph has loaded.
    /// </summary>
    /// <typeparam name="T">The declared type used for registry lookup.</typeparam>
    /// <param name="chronicler">The active pass supplied to <see cref="IRecordable.RecordData"/>.</param>
    /// <param name="value">The value to identify on save. The callback applies any loaded value.</param>
    /// <param name="name">The stable field name to use on both save and load.</param>
    /// <param name="assignLoadedValue">
    /// Applies the resolved value, or the default of <typeparamref name="T"/> for a missing or null entry.
    /// The load context retains this callback while the link is queued.
    /// </param>
    /// <param name="slot">An optional identity domain. Null or empty selects the default slot.</param>
    /// <remarks>
    /// On load, the callback runs immediately if the target is available or the entry is missing or null.
    /// Otherwise, <see cref="ChronicleContext.ResolveDeferredLinks"/> invokes it when the target resolves.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="assignLoadedValue"/> is null.</exception>
    /// <exception cref="InvalidOperationException">A non-null value has no stable ID on save.</exception>
    public static void LookDeferred<T>(
        IChronicler chronicler,
        T value,
        string name,
        Action<T> assignLoadedValue,
        string? slot = null)
    {
        if (assignLoadedValue == null)
            throw new ArgumentNullException(nameof(assignLoadedValue));

        chronicler.LookLink(
            ref value,
            name,
            slot,
            RecordLinkResolveMode.Deferred,
            assignLoadedValue);
    }
}
