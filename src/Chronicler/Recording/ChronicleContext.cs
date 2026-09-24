using System;
using System.Collections.Generic;
using System.Text;

namespace Chronicler;

/// <summary>
/// Carries session-scoped serialization services such as stable link resolution.
/// </summary>
public sealed class ChronicleContext
{
    private readonly List<IDeferredRecordLink> _deferredLinks = new();

    /// <summary>
    /// Creates a new context with an empty link registry.
    /// </summary>
    public ChronicleContext()
    {
        Links = new ChronicleLinkRegistry();
    }

    /// <summary>
    /// Gets the registry used to save and load stable links to external or runtime-owned objects.
    /// </summary>
    public ChronicleLinkRegistry Links { get; }

    internal void QueueDeferredLink<T>(string name, string id, string? slot, Action<T> assignLoadedValue)
    {
        _deferredLinks.Add(new DeferredRecordLink<T>(name, id, slot, assignLoadedValue));
    }

    /// <summary>
    /// Attempts to resolve all deferred links queued during loading.
    /// </summary>
    public void ResolveDeferredLinks()
    {
        if (_deferredLinks.Count == 0)
            return;

        ResolveQueuedLinks();

        if (_deferredLinks.Count > 0)
            ThrowUnresolvedDeferredLinks();
    }

    private void ResolveQueuedLinks()
    {
        while (_deferredLinks.Count > 0)
        {
            if (ResolveQueuedLinkPass() == 0)
                return;
        }
    }

    private int ResolveQueuedLinkPass()
    {
        int resolvedCount = 0;
        for (int i = _deferredLinks.Count - 1; i >= 0; i--)
        {
            if (!_deferredLinks[i].TryResolve(Links))
                continue;

            _deferredLinks.RemoveAt(i);
            resolvedCount++;
        }

        return resolvedCount;
    }

    private void ThrowUnresolvedDeferredLinks()
    {
        var builder = new StringBuilder();
        builder.Append("Unable to resolve deferred links:");
        foreach (IDeferredRecordLink deferredLink in _deferredLinks)
        {
            builder.Append(' ');
            builder.Append('[');
            builder.Append(deferredLink.Describe());
            builder.Append(']');
        }

        throw new InvalidOperationException(builder.ToString());
    }

    private interface IDeferredRecordLink
    {
        bool TryResolve(ChronicleLinkRegistry registry);

        string Describe();
    }

    private sealed class DeferredRecordLink<T> : IDeferredRecordLink
    {
        private readonly string _name;
        private readonly string _id;
        private readonly string? _slot;
        private readonly Action<T> _assignLoadedValue;

        public DeferredRecordLink(string name, string id, string? slot, Action<T> assignLoadedValue)
        {
            _name = name;
            _id = id;
            _slot = slot;
            _assignLoadedValue = assignLoadedValue ?? throw new ArgumentNullException(nameof(assignLoadedValue));
        }

        public bool TryResolve(ChronicleLinkRegistry registry)
        {
            T? value;
            if (!registry.TryResolve(_id, out value, _slot))
                return false;

            _assignLoadedValue(value!);
            return true;
        }

        public string Describe()
        {
            string typeName = typeof(T).Name;
            if (string.IsNullOrEmpty(_slot))
                return $"{_name}:{typeName}:{_id}";

            return $"{_name}:{typeName}:{_id}@{_slot}";
        }
    }
}
