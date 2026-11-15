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

    /// <summary>
    /// Gets the active serialization mode.
    /// </summary>
    SerializationMode Mode { get; }

    /// <summary>
    /// Reads or writes a value by name.
    /// </summary>
    void LookValue<T>(ref T value, string name, T? defaultValue = default);

    /// <summary>
    /// Reads or writes a nested recordable instance by name.
    /// </summary>
    void LookDeep<T>(ref T value, string name) where T : class, IRecordable;

    /// <summary>
    /// Reads or writes a nested recordable struct by name.
    /// </summary>
    void LookDeepStruct<T>(ref T value, string name) where T : struct, IRecordable;

    /// <summary>
    /// Reads or writes an optional nested recordable struct by name.
    /// </summary>
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
