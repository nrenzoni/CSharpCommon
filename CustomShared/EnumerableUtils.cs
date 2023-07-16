using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using NodaTime;

namespace CustomShared;

public static class EnumerableUtils
{
    public static List<T> FilterInRange<T>(
        this List<T> series,
        Func<T, LocalTime> extractTimePredicate,
        LocalTime? startTime = null,
        LocalTime? endTime = null,
        bool startTimeInclusive = true,
        bool endTimeInclusive = false)
    {
        if (startTime is null
            && endTime is null)
            return new List<T>(series);

        var newSeriesInRange = new List<T>();

        foreach (var item in series)
        {
            var nyLocalTime = extractTimePredicate(item);

            if (startTime is not null)
            {
                if (startTimeInclusive)
                {
                    if (nyLocalTime < startTime)
                        continue;
                }
                else if (nyLocalTime <= startTime)
                    continue;
            }

            if (endTime is not null)
            {
                if (endTimeInclusive)
                {
                    if (nyLocalTime > endTime)
                        break;
                }
                else if (nyLocalTime >= endTime)
                    break;
            }

            newSeriesInRange.Add(item);
        }

        return new List<T>(newSeriesInRange);
    }

    public static int FirstIndex<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, bool> predicate)
    {
        foreach (var (item, index) in source.WithIndex())
        {
            if (predicate(item))
                return index;
        }

        return -1;
    }

    public static List<int> SortIndices<T>(
        List<T> input,
        IComparer<T> comparer)
        where T : IComparable<T>
    {
        if (input.Count == 0)
        {
            return new List<int>();
        }

        var items = Enumerable.Range(
                0,
                input.Count)
            .ToList();

        items.Sort(
            Comparer<int>.Create(
                (
                    x,
                    y) => comparer.Compare(
                    input[x],
                    input[y])));

        return items;
    }

    public static KeyValuePair<uint, T> MaxIndex<T>(
        this IEnumerable<T> source)
        => MaxIndex(
            source,
            Comparer<T>.Default);

    public static KeyValuePair<uint, T> MaxIndex<T>(
        this IEnumerable<T> source,
        IComparer<T> comparer)
    {
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
        this IEnumerable<T> source,
        uint pairOffset = 1)
    {
        using var it = source.GetEnumerator();

        var nextFirstElems = new Queue<T>((int)pairOffset);

        // start iterator, enqueue first element
        if (it.MoveNext())
            nextFirstElems.Enqueue(it.Current);

        while (pairOffset > 1)
        {
            if (!it.MoveNext())
                break;
            nextFirstElems.Enqueue(it.Current);
            pairOffset--;
        }

        while (it.MoveNext())
        {
            var first = nextFirstElems.Dequeue();
            var second = it.Current;

            yield return (first, second);

            nextFirstElems.Enqueue(second);
        }
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

    private static readonly RNGCryptoServiceProvider Random = new();

    public static void ShuffleFisherYates<T>(
        this IList<T> list)
    {
        var rnd = ThreadSafeRandom.ThisThreadsRandom;

        for (var i = list.Count - 1; i > 1; i--)
        {
            var j = rnd.Next(
                0,
                i + 1);

            list.Swap(
                i,
                j);
        }
    }

    public static void Swap<T>(
        this IList<T> list,
        int i,
        int j)
    {
        (list[i], list[j]) =
            (list[j], list[i]);
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

    public static (List<T1> first, List<T2> second)
        Unzip<T, T1, T2>(
            this IList<T> source,
            Func<T, T1> firstSelector,
            Func<T, T2> secondSelector)
    {
        List<T1> firstList = new();
        List<T2> secondList = new();

        foreach (var s in source)
        {
            firstList.Add(firstSelector(s));
            secondList.Add(secondSelector(s));
        }

        return (firstList, secondList);
    }

    public static IEnumerable<int> GetIndices<T>(
        this IEnumerable<T> inEnumerable,
        Predicate<T> pred)
    {
        foreach (var (x1, i) in inEnumerable.WithIndex())
        {
            if (pred(x1))
                yield return i;
        }
    }

    public static IEnumerable<int> GetNonNullElementIndices<T>(
        this IEnumerable<T> inEnumerable)
    {
        return inEnumerable
            .GetIndices(x => x is not null);
    }

    public static HashSet<int> GetNonNullElementIndices<T1, T2>(
        IEnumerable<T1> enumerable1,
        IEnumerable<T2> enumerable2)
    {
        return enumerable1.GetNonNullElementIndices()
            .Intersect(enumerable2.GetNonNullElementIndices())
            .ToHashSet();
    }

    public static IEnumerable<T> GetFilteredByIndices<T>(
        IList<T> inArray,
        IEnumerable<int> indices)
    {
        var filteredArr =
            indices
                .Select(index => inArray[index]);

        return filteredArr;
    }

    public static IEnumerable<(T item, int index)> WithIndex<T>(
        this IEnumerable<T> source)
    {
        return source.Select(
            (
                item,
                index) => (item, index));
    }

    public static bool ScrambledEquals<T>(
        this IEnumerable<T> list1,
        IEnumerable<T> list2)
    {
        var cnt = new Dictionary<T, int>();
        foreach (T s in list1)
        {
            if (cnt.ContainsKey(s))
            {
                cnt[s]++;
            }
            else
            {
                cnt.Add(
                    s,
                    1);
            }
        }

        foreach (T s in list2)
        {
            if (cnt.ContainsKey(s))
            {
                cnt[s]--;
            }
            else
            {
                return false;
            }
        }

        return cnt.Values.All(c => c == 0);
    }

    public static int GetHashCodeByItems<T>(
        this IEnumerable<T> enumerable)
    {
        unchecked
        {
            var hash = 19;
            foreach (T item in enumerable)
            {
                hash = hash * 31 + (item != null
                    ? item.GetHashCode()
                    : 1);
            }

            return hash;
        }
    }

    public static bool IsUnique<T>(
        IEnumerable<T> values)
    {
        var set = new HashSet<T>();

        foreach (var item in values)
        {
            if (!set.Add(item))
                return false;
        }

        return true;
    }

    public static IEnumerable<T?> FillForward<T>(
        IEnumerable<T?> vals)
        where T : struct
    {
        using var enumerator = vals.GetEnumerator();

        if (!enumerator.MoveNext())
            yield return null;

        var prior = enumerator.Current;

        yield return prior;
        
        while (enumerator.MoveNext())
        {
            var curr = enumerator.Current;

            if (curr == null)
            {
                yield return prior;
                continue;
            }

            prior = curr;

            yield return curr;
        }
    }
}

public static class ThreadSafeRandom
{
    [ThreadStatic] private static Random Local;

    public static Random ThisThreadsRandom =>
        Local ??= new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId));
}
