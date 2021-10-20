using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomShared
{
    public static class Iterators
    {
        public static IEnumerable<IList<TSource>> Batch<TSource>(
            this IList<TSource> source,
            uint batchSize)
        {
            for (var x = 0; x < Math.Ceiling((decimal)source.Count() / batchSize); x++)
            {
                yield return source.Skip((int) (x * batchSize)).Take((int)batchSize).ToList();
            }
        }
        
        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source)
        {
            return source.Select((item, index) => (item, index));
        }
    }
}
