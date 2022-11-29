using System;
using System.Collections;
using System.Collections.Generic;

namespace CustomShared;

public class DefaultableDictionary<TKey, TValue> : IDictionary<TKey, TValue>
{
    private readonly IDictionary<TKey, TValue> _dictionary;
    private readonly Func<TValue> _defaultValueFactory;

    public DefaultableDictionary(IDictionary<TKey, TValue> dictionary, Func<TValue> defaultValueFactoryFactory)
    {
        _dictionary = dictionary;
        _defaultValueFactory = defaultValueFactoryFactory;
    }

    public DefaultableDictionary(Func<TValue> defaultValueFactoryFactory)
    {
        _dictionary = new Dictionary<TKey, TValue>();
        _defaultValueFactory = defaultValueFactoryFactory;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return _dictionary.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(KeyValuePair<TKey, TValue> item)
    {
        _dictionary.Add(item);
    }

    public void Clear()
    {
        _dictionary.Clear();
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        return _dictionary.Contains(item);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        _dictionary.CopyTo(array, arrayIndex);
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        return _dictionary.Remove(item);
    }

    public int Count => _dictionary.Count;

    public bool IsReadOnly => _dictionary.IsReadOnly;

    public bool ContainsKey(TKey key)
    {
        return _dictionary.ContainsKey(key);
    }

    public void Add(TKey key, TValue value)
    {
        _dictionary.Add(key, value);
    }

    public bool Remove(TKey key)
    {
        return _dictionary.Remove(key);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        if (!_dictionary.TryGetValue(key, out value))
        {
            value = _defaultValueFactory();
        }

        return true;
    }

    public TValue this[TKey key]
    {
        get
        {
            try
            {
                return _dictionary[key];
            }
            catch (KeyNotFoundException)
            {
                _dictionary[key] = _defaultValueFactory();
                return _dictionary[key];
            }
        }

        set => _dictionary[key] = value;
    }

    public ICollection<TKey> Keys
    {
        get { return _dictionary.Keys; }
    }

    public ICollection<TValue> Values
    {
        get
        {
            var values = _dictionary.Values;
            return values;
        }
    }
}

public static class DefaultableDictionaryExtensions
{
    public static DefaultableDictionary<TKey, TValue> WithDefaultValue<TValue, TKey>(
        this IDictionary<TKey, TValue> dictionary,
        Func<TValue> defaultValueFactoryFactory)
    {
        return new DefaultableDictionary<TKey, TValue>(dictionary, defaultValueFactoryFactory);
    }
}