using System;
using System.Collections.Generic;
using System.Linq;

namespace CustomShared;

public class FixedRangeBinGrouper<TGrouped>
{
    private readonly decimal _rangeSize;
    private readonly decimal _rangeSizeHalf;
    private readonly decimal? _center;
    private readonly object _syncLock = new();

    private readonly SortedList<decimal, TGrouped> nonCenteredItems;

    // key is lower boundary
    public SortedDictionary<decimal, List<TGrouped>> Groups { get; } = new();

    public FixedRangeBinGrouper(
        decimal rangeSize,
        decimal? center)
    {
        _rangeSize = rangeSize;
        _rangeSizeHalf = rangeSize / 2m;
        _center = center;

        if (_center is null)
            nonCenteredItems = new();
    }

    public IEnumerable<(decimal BinStart, uint Count)>
        GetBinsWithCountsIncludingEmpty()
    {
        if (_center.HasValue)
        {
            if (!Groups.Any())
                yield break;

            foreach (var valueTuple in GetBinsWithCountsIncludingEmptyForCenter())
                yield return valueTuple;
            yield break;
        }

        foreach (var valueTuple in GetBinsWithCountsIncludingEmptyForNonCentered())
            yield return valueTuple;
    }

    private IEnumerable<(decimal BinStart, uint Count)>
        GetBinsWithCountsIncludingEmptyForNonCentered()
    {
        var minKey = nonCenteredItems.Keys.First();

        var currKeyStart = minKey;
        var currKeyEnd = minKey + _rangeSize;
        uint currCount = 0;

        foreach (var (key, value) in nonCenteredItems)
        {
            if (key >= currKeyEnd)
            {
                yield return (currKeyStart, currCount);
                currKeyStart = currKeyEnd;
                currKeyEnd += _rangeSize;
                currCount = 1;
                continue;
            }

            currCount++;
        }

        if (currCount > 0)
        {
            yield return (currKeyStart, currCount);
        }
    }

    private IEnumerable<(decimal BinStart, uint Count)>
        GetBinsWithCountsIncludingEmptyForCenter()
    {
        var keys = Groups.Keys.ToList();
        var firstKey = keys.First();
        var lastKey = keys.Last();

        var iterKey = firstKey;

        while (iterKey <= lastKey)
        {
            Groups.TryGetValue(
                iterKey,
                out var group);

            if (group is null)
                yield return (iterKey, 0);
            else
            {
                yield return (iterKey, (uint)group.Count);
            }

            iterKey += _rangeSize;
        }
    }

    private decimal GetStartRangeForCentered(
        decimal key)
    {
        var potStart = _center.Value;

        var adder = _rangeSizeHalf;
        var lessThan = key < potStart;
        if (lessThan)
        {
            adder *= -1;
        }

        potStart += adder;

        var mod = Math.Abs(potStart - key) / _rangeSize - 0.5m;
        var frac = (int)Math.Round(mod);

        return potStart
               + frac * _rangeSize;
    }

    public void Emplace(
        decimal key,
        TGrouped val)
    {
        lock (_syncLock)
        {
            if (!_center.HasValue)
            {
                nonCenteredItems.Add(
                    key,
                    val);
                return;
            }

            var startRange =
                GetStartRangeForCentered(key);

            List<TGrouped> group;

            if (!Groups.ContainsKey(startRange))
            {
                group = Groups[startRange] = new();
            }
            else
                group = Groups[startRange];

            group.Add(val);
        }
    }
}
