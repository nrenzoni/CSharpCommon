using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using NodaTime;

namespace CustomShared;

public class MarketDayChecker
{
    private readonly IYearNonWeekendClosedDayChecker _yearNonWeekendClosedDayChecker;

    public MarketDayChecker(
        IYearNonWeekendClosedDayChecker yearNonWeekendClosedDayChecker)
    {
        _yearNonWeekendClosedDayChecker = yearNonWeekendClosedDayChecker;
    }

    public bool IsOpen(
        LocalDate date) => !IsNonWeekendClosedAllDay(date) && !IsWeekend(date);

    public bool IsNonWeekendClosedAllDay(
        LocalDate date) =>
        _yearNonWeekendClosedDayChecker.IsNonWeekendClosedAllDay(date);

    public bool IsWeekend(
        LocalDate date) => DateUtils.IsWeekend(date);

    public LocalDate GetNextOpenMarketDay(
        LocalDate date,
        int offset = 1)
    {
        return GetNextOpenDay(date, offset);
    }

    public LocalDate GetNextNonWeekendDay(
        LocalDate date,
        int offset = 1)
    {
        return GetNextOpenDay(date, offset, true);
    }

    public LocalDate GetNextOpenDay(
        LocalDate date,
        int offset = 1,
        bool onlySkipWeekend = false)
    {
        if (offset == 0)
            throw new Exception("offset must not be 0");

        var sign = Math.Sign(offset);
        int counter = sign;
        var oneDayPeriod = Period.FromDays(sign);

        var nextDate = date + oneDayPeriod;

        while (Math.Abs(offset - counter) > 0)
        {
            if (onlySkipWeekend)
            {
                if (IsWeekend(nextDate))
                    counter += sign;
            }
            else
            {
                if (IsOpen(nextDate))
                {
                    counter += sign;
                }
            }

            nextDate = nextDate + oneDayPeriod;
        }

        while (true)
        {
            if (onlySkipWeekend)
            {
                if (!IsWeekend(nextDate))
                    break;
            }
            else if (IsOpen(nextDate))
                break;

            nextDate = nextDate + oneDayPeriod;
        }

        return nextDate;
    }

    public List<LocalDate> GetMarketOpenDaysInRangeInclLast(
        LocalDate startDate,
        LocalDate endDateIncl)
    {
        if (startDate > endDateIncl)
            throw new Exception($"param {nameof(startDate)} must have value below or equal to {nameof(endDateIncl)}");

        var marketOpenDaysInRange = new List<LocalDate>();

        var currentDay = startDate;
        while (currentDay <= endDateIncl)
        {
            if (IsOpen(currentDay))
                marketOpenDaysInRange.Add(currentDay);
            currentDay = GetNextOpenMarketDay(currentDay);
        }

        return marketOpenDaysInRange;
    }
}

public class MarketClosedDay
{
    public LocalDate Date { get; }

    public LocalTime? CloseTime { get; }

    public bool ClosesEarly => CloseTime.HasValue;

    public bool ClosedAllDay => !ClosesEarly;

    public MarketClosedDay(
        LocalDate date,
        LocalTime? closeTime)
    {
        Date = date;
        CloseTime = closeTime;
    }
}

public interface IYearNonWeekendClosedDayChecker
{
    public bool IsNonWeekendClosedAllDay(
        LocalDate date);
}

public class YearNonWeekendClosedDayChecker : IYearNonWeekendClosedDayChecker
{
    private readonly string _marketDayClosedListDir;
    static readonly Dictionary<uint, YearNonWeekendClosedDays> Instances = new();

    public YearNonWeekendClosedDayChecker(
        string marketDayClosedListDir)
    {
        _marketDayClosedListDir = marketDayClosedListDir;
    }

    public bool IsNonWeekendClosedAllDay(
        LocalDate date)
    {
        var dateYear = (uint)date.Year;

        // lazy load all closed day data for year
        if (!Instances.ContainsKey(dateYear))
        {
            var marketClosedDays = new YearNonWeekendClosedDays(dateYear, _marketDayClosedListDir);
            Instances.Add(dateYear, marketClosedDays);
        }

        var nonWeekendClosedDaysForYear = Instances[dateYear];

        if (!nonWeekendClosedDaysForYear.Contains(date))
            return false;

        return nonWeekendClosedDaysForYear.GetMarketClosedData(date).ClosedAllDay;
    }
}

public class YearNonWeekendClosedDays
{
    private readonly string _marketDayClosedListDir;

    private readonly Dictionary<LocalDate, MarketClosedDay> _closedDays;

    public YearNonWeekendClosedDays(
        uint year,
        string marketDayClosedListDir)
    {
        Year = year;
        _marketDayClosedListDir = marketDayClosedListDir;

        _closedDays = LoadFromFile(year).ToDictionary(x => x.Date, x => x);
    }

    public bool Contains(
        LocalDate date) => _closedDays.ContainsKey(date);

    public MarketClosedDay GetMarketClosedData(
        LocalDate date) => _closedDays[date];

    public uint Year { get; }


    public List<MarketClosedDay> LoadFromFile(
        uint year)
    {
        var marketDayDir = _marketDayClosedListDir;
        if (!Directory.Exists(marketDayDir))
            throw new DirectoryNotFoundException($"Directory doesn't exist: [{marketDayDir}].");

        var files = Directory.GetFiles(marketDayDir, $"*{year.ToString()}*");

        if (files.Length == 0)
            throw new Exception($"No market closed file for year: [{year}]");

        if (files.Length > 1)
            throw new Exception($"More than 1 market closed file found for year [{year}].");

        var marketClosedFile = files[0];

        return LoadCsv(marketClosedFile);
    }

    // ReSharper disable once ClassNeverInstantiated.Global ClassNeverInstantiated.Local
    private class RawCsvClosedTradingDate
    {
        [Name("date")] public string Date { get; set; }

        [Name("closes_early")] public string ClosesEarly { get; set; }

        [Name("closes_early_time")] public string ClosesEarlyTime { get; set; }

        public MarketClosedDay ToMarketClosedDay()
        {
            var localDate = Date.ParseToLocalDate();
            LocalTime? closingTime = null;
            if (int.Parse(ClosesEarly) == 1)
            {
                if (string.IsNullOrEmpty(ClosesEarlyTime))
                    throw new Exception("ClosesEarlyTime is empty but ClosesEarly flag is true.");

                closingTime = ClosesEarlyTime.ParseToLocalTime();
            }

            return new MarketClosedDay(localDate, closingTime);
        }
    }

    private static List<MarketClosedDay> LoadCsv(
        string filename)
    {
        using var reader = new StreamReader(filename);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var records = csv.GetRecords<RawCsvClosedTradingDate>();

        return records.Select(x => x.ToMarketClosedDay()).ToList();
    }
}
