using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
}