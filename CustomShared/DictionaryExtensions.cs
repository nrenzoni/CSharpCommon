using System;
using System.Collections.Generic;
using System.Linq;

namespace CustomShared;

public static class DictionaryExtensions
{
    public static IEnumerable<string> StringifyDictionaryToLines<K, V>(
        this IDictionary<K, V> dictionary,
        IList<K> orderBy = null)
    {
        var dictSorted =
            orderBy is not null
                ? orderBy.Select(x => (x, dictionary[x]))
                : dictionary.Select(kv => (kv.Key, kv.Value));

        var stringifyDictionaryToLines =
            dictSorted
                .Select(
                    kvp => kvp.Item1 + ": " + kvp.Item2);

        return stringifyDictionaryToLines;
    }

    public static string StringifyDictionary<K, V>(
        this IDictionary<K, V> dictionary,
        bool newLinePerKv = false)
    {
        var lines =
            StringifyDictionaryToLines(dictionary);

        var separator =
            newLinePerKv
                ? "\n"
                : ", ";

        return string.Join(
            separator,
            lines);
    }

    public static Dictionary<TKey, object> WithValueAsObj<TKey, TValue>(this Dictionary<TKey, TValue> inDic)
    {
        return inDic.ToDictionary(kv => kv.Key,
            kv => (object)kv.Value);
    }
}