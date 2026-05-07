using System;
using System.Collections;
using System.Collections.Generic;

namespace Chronicler;

internal sealed class OrderedStringMap<TValue> : IEnumerable<KeyValuePair<string, TValue>>
{
    private readonly List<KeyValuePair<string, TValue>> _entries;
    private readonly Dictionary<string, int> _indexes;

    public OrderedStringMap(int capacity, IEqualityComparer<string> comparer)
    {
        _entries = new List<KeyValuePair<string, TValue>>(capacity);
        _indexes = new Dictionary<string, int>(capacity, comparer);
    }

    public int Count => _entries.Count;

    public TValue this[string key]
    {
        set => Set(key, value);
    }

    public bool TryGetValue(string key, out TValue value)
    {
        if (!_indexes.TryGetValue(key, out int index))
        {
            value = default!;
            return false;
        }

        value = _entries[index].Value;
        return true;
    }

    public bool Remove(string key)
    {
        if (!_indexes.TryGetValue(key, out int index))
            return false;

        _entries.RemoveAt(index);
        _indexes.Remove(key);

        for (int i = index; i < _entries.Count; i++)
            _indexes[_entries[i].Key] = i;

        return true;
    }

    public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator()
    {
        return _entries.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private void Set(string key, TValue value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        if (_indexes.TryGetValue(key, out int index))
        {
            string existingKey = _entries[index].Key;
            _entries[index] = new KeyValuePair<string, TValue>(existingKey, value);
            return;
        }

        _indexes.Add(key, _entries.Count);
        _entries.Add(new KeyValuePair<string, TValue>(key, value));
    }
}
