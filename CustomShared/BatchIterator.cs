using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomShared;

public static class Iterators
{
    public static IEnumerable<IList<TSource>> Batch<TSource>(
        this IList<TSource> source,
        uint batchSize)
    {
        for (var x = 0; x < Math.Ceiling((decimal)source.Count() / batchSize); x++)
        {
            yield return source.Skip((int)(x * batchSize)).Take((int)batchSize).ToList();
        }
    }

    public static IEnumerable<IList<TSource>> Batch<TSource>(
        this IEnumerable<TSource> source,
        uint batchSize)
    {
        List<TSource> nextList = new();
        using var enumerator = source.GetEnumerator();
        while (enumerator.MoveNext())
        {
            nextList.Add(
                enumerator.Current);

            if (nextList.Count < batchSize)
                continue;

            yield return nextList;
            nextList = new();
        }

        if (nextList.Count > 0)
            yield return nextList;
    }
}
