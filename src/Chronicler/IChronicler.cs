//=======================================================================
// IChronicler.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

using System;

namespace Chronicler;

/// <summary>
/// Defines a recording pass for values, owned state, and stable links.
/// </summary>
public interface IChronicler
{
    /// <summary>
    /// Gets the session context for the current chronicler pass.
    /// </summary>
    /// <value>The context that supplies stable link registrations and tracks deferred assignments for this pass.</value>
    ChronicleContext Context { get; }

    /// <summary>Gets the direction of the current pass.</summary>
    /// <value>Saving reads the schema's current state. Loading applies the recorded state.</value>
    SerializationMode Mode { get; }

    /// <summary>Records a leaf value.</summary>
    /// <typeparam name="T">The type handled by the active transport.</typeparam>
    /// <param name="value">The value to write on save. On load, receives the decoded value or the declared default for a missing or null entry.</param>
    /// <param name="name">The stable field name shared by saving and loading.</param>
    /// <param name="defaultValue">The schema default. The built-in transports omit null values and values equal to this default when saving.</param>
    /// <remarks>Leaf encoding is transport-specific. Record hashes include the current and default values for supported leaf types.</remarks>
    void LookValue<T>(ref T value, string name, T? defaultValue = default);

    /// <summary>Records an owned class instance.</summary>
    /// <typeparam name="T">The declared type of the owned object.</typeparam>
    /// <param name="value">The existing instance to write or populate. A missing or null entry leaves the target untouched.</param>
    /// <param name="name">The stable field name shared by saving and loading.</param>
    /// <remarks>A present entry runs the schema on the existing target instance. A failure can leave that instance partly updated.</remarks>
    /// <exception cref="InvalidOperationException">A present entry needs an instance, but the load target is null.</exception>
    void LookDeep<T>(ref T value, string name) where T : class, IRecordable;

    /// <summary>Records an owned struct inline.</summary>
    /// <typeparam name="T">The type of the owned value.</typeparam>
    /// <param name="value">The value to write on save. A present record loads into a fresh value. For missing or null input, the pass runs the schema against an empty record before replacing this value.</param>
    /// <param name="name">The stable field name shared by saving and loading.</param>
    /// <remarks>If a present record throws while applying its schema, the new value can be partly updated. If applying an empty record throws, the prior value is retained.</remarks>
    void LookDeepStruct<T>(ref T value, string name) where T : struct, IRecordable;

    /// <summary>Records an optional owned struct inline.</summary>
    /// <typeparam name="T">The underlying non-nullable struct type.</typeparam>
    /// <param name="value">The value to write on save. A missing or null entry clears it on load. A present record is staged before assignment.</param>
    /// <param name="name">The stable field name shared by saving and loading.</param>
    /// <remarks>An absent value is omitted on save. A failure while loading a present record leaves the existing nullable value in place.</remarks>
    void LookNullableDeep<T>(ref T? value, string name) where T : struct, IRecordable;

    /// <summary>
    /// Processes a named link by stable ID.
    /// </summary>
    /// <typeparam name="T">The declared type used for registry lookup.</typeparam>
    /// <param name="value">
    /// The value to identify on save. On load, receives the resolved value, or the default of
    /// <typeparamref name="T"/> for a missing, null, or queued entry.
    /// An unresolved immediate link leaves this value unchanged.
    /// </param>
    /// <param name="name">The stable field name to use on both save and load.</param>
    /// <param name="slot">An optional identity domain. Null or empty selects the default slot.</param>
    /// <param name="resolveMode">Whether a present ID must resolve when read. Defaults to immediate resolution.</param>
    /// <param name="assignLoadedValue">
    /// Optional load callback for immediate links. Required when reading a present ID in deferred mode.
    /// Receives the resolved value or the default for a missing or null entry.
    /// </param>
    /// <remarks>
    /// Use the callback to assign state when a queued link resolves, because the pass cannot update
    /// <paramref name="value"/> after the call returns. See <see cref="ChronicleContext.ResolveDeferredLinks"/>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// A non-null value has no stable ID on save, a present immediate link cannot resolve,
    /// or a present deferred link is read without an assignment callback.
    /// </exception>
    void LookLink<T>(
        ref T value,
        string name,
        string? slot = null,
        RecordLinkResolveMode resolveMode = RecordLinkResolveMode.Immediate,
        Action<T>? assignLoadedValue = null);
}
