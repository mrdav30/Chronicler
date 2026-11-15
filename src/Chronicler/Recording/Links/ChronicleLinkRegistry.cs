//=======================================================================
// ChronicleLinkRegistry.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Chronicler.Serialization;

namespace Chronicler;

/// <summary>
/// Maps stable IDs to host-owned values within a recording session.
/// </summary>
/// <remarks>
/// Each lookup uses the declared link type and slot. Slot names are case-sensitive.
/// Null and empty slots select the same default slot.
/// </remarks>
public sealed class ChronicleLinkRegistry
{
    private readonly Dictionary<ChronicleLinkKey, object> _resolvers = new();
    private readonly Dictionary<ChronicleLinkKey, object> _registeredInstances = new();

    /// <summary>
    /// Sets the custom resolver for a link type and slot.
    /// </summary>
    /// <typeparam name="T">The declared type used by the recording schema.</typeparam>
    /// <param name="resolver">The resolver to retain and consult before instance registrations.</param>
    /// <param name="slot">An optional identity domain. Null or empty selects the default slot.</param>
    /// <remarks>Replaces any previous resolver in that type and slot.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="resolver"/> is null.</exception>
    public void RegisterResolver<T>(IRecordLinkResolver<T> resolver, string? slot = null)
    {
        _resolvers[new ChronicleLinkKey(typeof(T), slot)] = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    /// <summary>
    /// Associates a value with a stable ID.
    /// </summary>
    /// <typeparam name="T">The declared type used by the recording schema.</typeparam>
    /// <param name="id">A case-sensitive ID containing at least one non-whitespace character.</param>
    /// <param name="value">The value to retain. For reference types, the registry keeps the instance.</param>
    /// <param name="slot">An optional identity domain. Null or empty selects the default slot.</param>
    /// <remarks>Registering the same ID for the same type and slot replaces its value in place.</remarks>
    /// <exception cref="ArgumentException"><paramref name="id"/> is null, empty, or whitespace.</exception>
    public void RegisterInstance<T>(string id, T value, string? slot = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("A registered link id must not be null or empty.", nameof(id));

        RegisteredLinkTable<T> table = GetOrCreateInstanceTable<T>(slot);
        table.Register(id, value);
    }

    /// <summary>
    /// Removes an instance registration.
    /// </summary>
    /// <typeparam name="T">The declared type used to register the value.</typeparam>
    /// <param name="id">The case-sensitive ID of the registration.</param>
    /// <param name="slot">The registration's identity domain. Null or empty selects the default slot.</param>
    /// <returns>
    /// <see langword="true"/> if the registration was removed. Returns <see langword="false"/> for a blank ID
    /// or when the type and slot contain no registration for it.
    /// </returns>
    /// <remarks>The custom resolver for the same type and slot remains available.</remarks>
    public bool UnregisterInstance<T>(string id, string? slot = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        ChronicleLinkKey key = new(typeof(T), slot);
        if (!_registeredInstances.TryGetValue(key, out object? tableObject))
            return false;

        return ((RegisteredLinkTable<T>)tableObject!).Unregister(id);
    }

    /// <summary>
    /// Looks up the value for a recorded ID.
    /// </summary>
    /// <typeparam name="T">The declared link type to look up.</typeparam>
    /// <param name="id">The non-null recorded ID. Instance registrations use case-sensitive matching.</param>
    /// <param name="value">The resolved value on success. Otherwise, the default value of <typeparamref name="T"/>.</param>
    /// <param name="slot">The recorded link's identity domain. Null or empty selects the default slot.</param>
    /// <returns><see langword="true"/> if the resolver or an instance registration supplied a value.</returns>
    /// <remarks>Consults the custom resolver first and falls back to instance registrations on a miss.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is null when an instance table is consulted.</exception>
    public bool TryResolve<T>(string id, [MaybeNullWhen(false)] out T value, string? slot = null)
    {
        ChronicleLinkKey key = new(typeof(T), slot);
        if (_resolvers.TryGetValue(key, out object? resolverObject)
            && ((IRecordLinkResolver<T>)resolverObject!).TryResolveReference(id, out value))
        {
            return true;
        }

        if (_registeredInstances.TryGetValue(key, out object? tableObject)
            && ((RegisteredLinkTable<T>)tableObject!).TryResolve(id, out value))
        {
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// Looks up the stable ID for a value.
    /// </summary>
    /// <typeparam name="T">The declared link type to look up.</typeparam>
    /// <param name="value">The value to identify in this session.</param>
    /// <param name="id">The stable ID on success. Otherwise, null.</param>
    /// <param name="slot">The link's identity domain. Null or empty selects the default slot.</param>
    /// <returns><see langword="true"/> if the resolver or an instance registration supplied an ID.</returns>
    /// <remarks>
    /// Consults the custom resolver first. Instance lookup uses reference identity for reference types
    /// and default equality for value types. If several registrations match, returns the first in registration order.
    /// </remarks>
    public bool TryGetReferenceId<T>(T value, [NotNullWhen(true)] out string? id, string? slot = null)
    {
        ChronicleLinkKey key = new(typeof(T), slot);
        if (_resolvers.TryGetValue(key, out object? resolverObject)
            && ((IRecordLinkResolver<T>)resolverObject!).TryGetReferenceId(value, out id))
        {
            return true;
        }

        if (_registeredInstances.TryGetValue(key, out object? tableObject)
            && ((RegisteredLinkTable<T>)tableObject!).TryGetReferenceId(value, out id))
        {
            return true;
        }

        id = null;
        return false;
    }

    private RegisteredLinkTable<T> GetOrCreateInstanceTable<T>(string? slot)
    {
        ChronicleLinkKey key = new(typeof(T), slot);
        if (_registeredInstances.TryGetValue(key, out object? tableObject))
            return (RegisteredLinkTable<T>)tableObject!;

        var table = new RegisteredLinkTable<T>();
        _registeredInstances[key] = table;
        return table;
    }

    private readonly struct ChronicleLinkKey : IEquatable<ChronicleLinkKey>
    {
        private readonly Type _type;
        private readonly string _slot;

        public ChronicleLinkKey(Type type, string? slot)
        {
            _type = type ?? throw new ArgumentNullException(nameof(type));
            _slot = slot ?? string.Empty;
        }

        public bool Equals(ChronicleLinkKey other)
        {
            return _type == other._type
                && string.Equals(_slot, other._slot, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj) => obj is ChronicleLinkKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (_type.GetHashCode() * 397) ^ _slot.GetHashCode();
            }
        }
    }

    private sealed class RegisteredLinkTable<T>
    {
        private readonly OrderedStringMap<T> _byId = new(8, StringComparer.Ordinal);

        public void Register(string id, T value)
        {
            _byId[id] = value;
        }

        public bool Unregister(string id)
        {
            return _byId.Remove(id);
        }

        public bool TryResolve(string id, [MaybeNullWhen(false)] out T value)
        {
            return _byId.TryGetValue(id, out value);
        }

        public bool TryGetReferenceId(T value, [NotNullWhen(true)] out string? id)
        {
            foreach (KeyValuePair<string, T> pair in _byId)
            {
                if (!ValuesMatch(pair.Value, value))
                    continue;

                id = pair.Key;
                return true;
            }

            id = null;
            return false;
        }

        private static bool ValuesMatch(T left, T right)
        {
            if (typeof(T).IsValueType)
                return EqualityComparer<T>.Default.Equals(left, right);

            return ReferenceEquals(left, right);
        }
    }
}
