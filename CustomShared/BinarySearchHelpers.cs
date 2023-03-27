#nullable enable
using System;
using System.Collections.Generic;

namespace CustomShared;

public static class BinarySearchHelpers
{
    public static Int32 BinarySearch<T>(
        this IList<T?> list,
        T? value,
        IComparer<T>? comparer = null)
    {
        if (list == null)
            throw new ArgumentNullException(nameof(list));

        comparer ??= Comparer<T>.Default;

        Int32 lower = 0;
        Int32 upper = list.Count - 1;

        while (lower <= upper)
        {
            Int32 middle = lower + (upper - lower) / 2;
            Int32 comparisonResult = comparer.Compare(
                list[middle],
                value);
            if (comparisonResult == 0)
                return middle;
            if (comparisonResult > 0)
                upper = middle - 1;
            else
                lower = middle + 1;
        }

        return ~lower;
    }

    public static List<T>? FilterInRangeBinarySearch<T>(
        this List<T> list,
        IComparer<T> comparer,
        FilterType filterType)
    {
        var binarySearchResult = CustomBinarySearch(
            list,
            comparer);

        switch (filterType)
        {
            case FilterType.AllAboveOrEqual:
                switch (binarySearchResult.BinarySearchResultType)
                {
                    case BinarySearchResultType.AllAbove:
                        return list;
                    case BinarySearchResultType.AllUnder:
                        return null;
                    case BinarySearchResultType.FirstOver or BinarySearchResultType.Exact:
                        return list.GetRange(
                            (int)binarySearchResult.Index,
                            list.Count - (int)binarySearchResult.Index);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            case FilterType.AllUnder:
                switch (binarySearchResult.BinarySearchResultType)
                {
                    case BinarySearchResultType.AllAbove:
                        return null;
                    case BinarySearchResultType.AllUnder:
                        return list;
                    case BinarySearchResultType.FirstOver or BinarySearchResultType.Exact:
                        return list.GetRange(
                            0,
                            (int)binarySearchResult.Index);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            case FilterType.AllUnderOrEqual:
                switch (binarySearchResult.BinarySearchResultType)
                {
                    case BinarySearchResultType.AllAbove:
                        return null;
                    case BinarySearchResultType.AllUnder:
                        return list;
                    case BinarySearchResultType.FirstOver:
                        return list.GetRange(
                            0,
                            (int)binarySearchResult.Index);
                    case BinarySearchResultType.Exact:
                        return list.GetRange(
                            0,
                            (int)binarySearchResult.Index + 1);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            default:
                throw new NotImplementedException();
        }
    }

    public static BinarySearchResult CustomBinarySearch<T>(
        this List<T> list,
        IComparer<T> comparer)
    {
        var binarySearchIntResult = list.BinarySearch(
            default,
            comparer);

        return GetBinarySearchResultFromIdxResult(
            binarySearchIntResult,
            list.Count);
    }

    public static BinarySearchResult CustomBinarySearch<T>(
        this IList<T> list,
        IComparer<T> comparer)
    {
        var binarySearchIntResult = list.BinarySearch(
            default,
            comparer);

        return GetBinarySearchResultFromIdxResult(
            binarySearchIntResult,
            list.Count);
    }

    public static BinarySearchResult CustomBinarySearch<T>(
        this IList<T?> list,
        T value)
    {
        var binarySearchIntResult = list.BinarySearch(
            value,
            Comparer<T>.Default);

        return GetBinarySearchResultFromIdxResult(
            binarySearchIntResult,
            list.Count);
    }

    public static BinarySearchResult GetBinarySearchResultFromIdxResult(
        int binarySearchResult,
        int collectionSize)
    {
        if (binarySearchResult >= 0)
            return new BinarySearchResult(
                (uint)binarySearchResult,
                BinarySearchResultType.Exact);

        binarySearchResult = ~binarySearchResult;

        if (binarySearchResult == collectionSize)
            return new BinarySearchResult(
                null,
                BinarySearchResultType.AllUnder);

        if (binarySearchResult == 0)
            return new BinarySearchResult(
                null,
                BinarySearchResultType.AllAbove);

        return new BinarySearchResult(
            (uint)binarySearchResult,
            BinarySearchResultType.FirstOver);
    }
}

public record BinarySearchResult(
    uint? Index,
    BinarySearchResultType BinarySearchResultType);

public enum BinarySearchResultType
{
    AllAbove,
    AllUnder,
    FirstOver,
    Exact,
}

public enum FilterType
{
    AllAbove,
    AllAboveOrEqual,
    AllUnder,
    AllUnderOrEqual
}

public class SingleValueCollectionComparer<TCollection, T> : IComparer<TCollection>
    where T : IComparable<T>
{
    private readonly T _val;
    private readonly Func<TCollection?, T> _collectionValueExtractor;

    public SingleValueCollectionComparer(T val, Func<TCollection?, T> collectionValueExtractor)
    {
        _val = val;
        _collectionValueExtractor = collectionValueExtractor;
    }

    public int Compare(TCollection? x, TCollection? y)
        => _collectionValueExtractor(x).CompareTo(_val);
}