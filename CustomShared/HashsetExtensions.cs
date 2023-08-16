using System.Collections.Generic;

namespace CustomShared;

public static class HashsetExtensions
{
    public static bool AddRange<T>(
        this HashSet<T> hashset,
        IEnumerable<T> items)
    {
        bool allAdded = true;   
        
        foreach (var item in items)
        {
            var added = hashset.Add(item);
            if (!added)
                allAdded = false;
        }

        return allAdded;
    }
}