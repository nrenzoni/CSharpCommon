using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NodaTime;

namespace CustomShared;

public class MarketDateRangeUtils
{
    private readonly MarketDayChecker _marketDayChecker;

    public MarketDateRangeUtils(
        MarketDayChecker marketDayChecker)
    {
        _marketDayChecker = marketDayChecker;
    }

    public List<MarketDateRange> BuildMarketDateRangeContiguousDates(
        ImmutableSortedSet<LocalDate> dates)
    {
        if (dates.Count == 0)
            return new List<MarketDateRange>();

        List<MarketDateRange> uncontainedMarketDateRanges = new();

        LocalDate currentMinDate = dates.First();
        LocalDate currentEndDate = currentMinDate;
        foreach (var date in dates.Skip(1))
        {
            if (_marketDayChecker.GetNextOpenDay(currentEndDate) != date)
            {
                uncontainedMarketDateRanges.Add(
                    new(currentMinDate, currentEndDate));

                currentMinDate = date;
            }

            currentEndDate = date;
        }

        if (uncontainedMarketDateRanges.Count == 0
            || !uncontainedMarketDateRanges.Last().ContainsDate(currentEndDate))
        {
            uncontainedMarketDateRanges.Add(
                new(currentMinDate, currentEndDate));
        }

        return uncontainedMarketDateRanges;
    }
    
    public IEnumerable<LocalDate> IterateDays(
        MarketDateRange marketDateRange,
        bool marketDayOffset = true)
    {
        Func<LocalDate, LocalDate, IEnumerable<LocalDate>> getDaysInRangeFunc;
        if (marketDayOffset)
            getDaysInRangeFunc
                = (
                    startDate,
                    endDate) => _marketDayChecker.GetMarketOpenDaysInRangeInclLast(
                    startDate, endDate);
        else
            getDaysInRangeFunc
                = DateUtils.RangeOfDatesInclLast;

        return getDaysInRangeFunc(
            marketDateRange.StartDate, marketDateRange.EndDate);
    }
}
