using System;
using System.Collections.Generic;
using System.Linq;

namespace CustomShared;

public static class DictionaryExtensions
{
    public static string StringifyDictionary<K, V>(
        this IDictionary<K, V> dictionary)
    {
        var lines = dictionary.Select(
            kvp => kvp.Key + ": " + kvp.Value);
        return string.Join(
            ", ",
            lines);
    }
}
