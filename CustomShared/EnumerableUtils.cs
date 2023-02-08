using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using CsvHelper;

namespace CustomShared;

public static class EnumerableUtils
{
    public static KeyValuePair<uint, T> MaxIndex<T>(
        this IEnumerable<T> source)
    {
        IComparer<T> comparer = Comparer<T>.Default;
        using var iterator = source.GetEnumerator();

        if (!iterator.MoveNext())
        {
            throw new InvalidOperationException("Empty sequence");
        }

        uint maxIndex = 0;
        T maxElement = iterator.Current;
        uint index = 0;
        while (iterator.MoveNext())
        {
            index++;
            T element = iterator.Current;
            if (comparer.Compare(
                    element,
                    maxElement) > 0)
            {
                maxElement = element;
                maxIndex = index;
            }
        }

        return new KeyValuePair<uint, T>(
            maxIndex,
            maxElement);
    }

    public static KeyValuePair<uint, T> MinIndex<T>(
        this IEnumerable<T> source)
    {
        IComparer<T> comparer = Comparer<T>.Default;
        using var iterator = source.GetEnumerator();

        if (!iterator.MoveNext())
        {
            throw new InvalidOperationException("Empty sequence");
        }

        uint minIndex = 0;
        T minElement = iterator.Current;
        uint index = 0;
        while (iterator.MoveNext())
        {
            index++;
            T element = iterator.Current;
            if (comparer.Compare(
                    element,
                    minElement) < 0)
            {
                minElement = element;
                minIndex = index;
            }
        }

        return new KeyValuePair<uint, T>(
            minIndex,
            minElement);
    }

    public static IEnumerable<T> FastReverse<T>(
        this IList<T> items)
    {
        for (int i = items.Count - 1; i >= 0; i--)
        {
            yield return items[i];
        }
    }

    public static IEnumerable<decimal> RangeIterator(
        decimal start,
        decimal stop,
        decimal step)
    {
        if (step == 0)
            throw new ArgumentException(nameof(step));

        var x = start;

        if (step > 0)
            while (x <= stop)
            {
                yield return x;
                x += step;
            }
        else
            while (x >= stop)
            {
                yield return x;
                x += step;
            }
    }

    // https://codereview.stackexchange.com/a/140428
    public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(
        this IEnumerable<IEnumerable<T>> items)
    {
        var slots = items
            // initialize enumerators
            .Select(x => x.GetEnumerator())
            // get only those that could start in case there is an empty collection
            .Where(x => x.MoveNext())
            .ToArray();

        if (slots.Length == 0)
            yield break;

        while (true)
        {
            // yield current values
            yield return slots.Select(x => x.Current);

            // increase enumerators
            foreach (var slot in slots)
            {
                // reset the slot if it couldn't move next
                if (!slot.MoveNext())
                {
                    // stop when the last enumerator resets
                    if (slot == slots.Last())
                        yield break;

                    slot.Reset();
                    slot.MoveNext();
                    // move to the next enumerator if this reset
                    continue;
                }

                // we could increase the current enumerator without reset so stop here
                break;
            }
        }
    }

    // https://stackoverflow.com/a/1581482/3262950
    public static IEnumerable<(T, T)> Pairwise<T>(
        this IEnumerable<T> source)
    {
        var previous = default(T);

        using var it = source.GetEnumerator();

        if (it.MoveNext())
            previous = it.Current;

        while (it.MoveNext())
            yield return (previous, previous = it.Current);
    }

    // https://stackoverflow.com/a/4831908/3262950
    public static IEnumerable<decimal> CumulativeSum(
        this IEnumerable<decimal> sequence)
    {
        decimal sum = 0;
        foreach (var item in sequence)
        {
            sum += item;
            yield return sum;
        }
    }

    public static bool IsOrdered<T>(
        this IList<T> list,
        IComparer<T> comparer = null)
    {
        if (comparer == null)
        {
            comparer = Comparer<T>.Default;
        }

        if (list.Count > 1)
        {
            for (int i = 1; i < list.Count; i++)
            {
                if (comparer.Compare(
                        list[i - 1],
                        list[i]) > 0)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static readonly RNGCryptoServiceProvider  Random = new();

    public static void Shuffle<T>(
        this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            byte[] box = new byte[1];
            do Random.GetBytes(box);
            while (!(box[0] < n * (Byte.MaxValue / n)));
            int k = (box[0] % n);
            n--;
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
    
    public static IEnumerable<T> Unroll<T>(
        this IEnumerable<IEnumerable<T>> enumerableOfEnumerables)
        => enumerableOfEnumerables.SelectMany(enumerable => enumerable);

    public static void AssertListType<T>(
        this object list)
    {
        var type = list.GetType();
        if (type.IsGenericType
            && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            var listType = type.GetGenericArguments()[0];

            if (listType is not T)
            {
                throw new Exception($"Item of type {listType} is not a list of type {typeof(T)}.");
            }
        }
        else throw new Exception("Item is not a list.");
    }

    public static Dictionary<string, T> ReadDictionaryKeyTypeOnly<T>(
        this Dictionary<string, object> inDictionary)
    {
        var outDictionary = new Dictionary<string, T>();

        foreach (var (key, value) in inDictionary)
        {
            try
            {
                var converted = (T)Convert.ChangeType(
                    value,
                    typeof(T));
                outDictionary[key] = converted;
            }
            catch (Exception e)
            {
                
            }
        }

        return outDictionary;
    }
}
