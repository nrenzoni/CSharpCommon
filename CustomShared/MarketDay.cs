using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using NodaTime;

namespace CustomShared
{
    public class MarketDay
    {
        public LocalDate Date { get; }

        public MarketDay(LocalDate date)
        {
            Date = date;
        }

        public bool IsOpen => !IsNonWeekendClosedAllDay && !IsWeekend;

        public bool IsNonWeekendClosedAllDay => YearNonWeekendClosedDayChecker.IsNonWeekendClosedAllDay(Date);

        public bool IsWeekend => DateUtils.IsWeekend(Date);

        public MarketDay GetNextOpenMarketDay(int offset = 1)
        {
            return GetNextOpenDay(Date, offset);
        }

        public MarketDay GetNextNonWeekendDay(int offset = 1)
        {
            return GetNextOpenDay(Date, offset, true);
        }

        public static MarketDay GetNextOpenDay(LocalDate date, int offset = 1, bool onlySkipWeekend = false)
        {
            if (offset == 0)
                throw new Exception("offset must not be 0");

            var sign = Math.Sign(offset);
            int counter = sign;
            var oneDayPeriod = Period.FromDays(sign);

            MarketDay nextMarketDay = new MarketDay(date + oneDayPeriod);

            while (Math.Abs(offset - counter) > 0)
            {
                if (onlySkipWeekend)
                {
                    if (nextMarketDay.IsWeekend)
                        counter += sign;
                }
                else
                {
                    if (nextMarketDay.IsOpen)
                    {
                        counter += sign;
                    }
                }

                nextMarketDay = new MarketDay(nextMarketDay.Date + oneDayPeriod);
            }

            while (true)
            {
                if (onlySkipWeekend)
                {
                    if (!nextMarketDay.IsWeekend)
                        break;
                }
                else if (nextMarketDay.IsOpen)
                    break;

                nextMarketDay = new MarketDay(nextMarketDay.Date + oneDayPeriod);
            }

            return nextMarketDay;
        }

        public static List<LocalDate> GetMarketOpenDaysInRange(LocalDate startDate, LocalDate endDate)
        {
            if (startDate >= endDate)
                throw new Exception($"param {nameof(startDate)} must have value below {nameof(endDate)}");

            var marketOpenDaysInRange = new List<LocalDate>();

            var currentDay = new MarketDay(startDate);
            while (currentDay.Date <= endDate)
            {
                if (currentDay.IsOpen)
                    marketOpenDaysInRange.Add(currentDay.Date);
                currentDay = currentDay.GetNextOpenMarketDay();
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

        public MarketClosedDay(LocalDate date, LocalTime? closeTime)
        {
            Date = date;
            CloseTime = closeTime;
        }
    }

    public static class YearNonWeekendClosedDayChecker
    {
        static readonly Dictionary<uint, YearNonWeekendClosedDays> Instances = new();

        public static bool IsNonWeekendClosedAllDay(LocalDate date)
        {
            var dateYear = (uint)date.Year;

            // lazy load all closed day data for year
            if (!Instances.ContainsKey(dateYear))
            {
                var marketClosedDays = new YearNonWeekendClosedDays(dateYear);
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
        private readonly Dictionary<LocalDate, MarketClosedDay> _closedDays;

        public bool Contains(LocalDate date) => _closedDays.ContainsKey(date);

        public MarketClosedDay GetMarketClosedData(LocalDate date) => _closedDays[date];

        public uint Year { get; }

        public YearNonWeekendClosedDays(uint year)
        {
            Year = year;
            _closedDays = LoadFromFile(year).ToDictionary(x => x.Date, x => x);
        }

        public static List<MarketClosedDay> LoadFromFile(uint year)
        {
            var marketDayDir = ConfigVariables.Instance.MarketDayClosedListDir;
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

        private static List<MarketClosedDay> LoadCsv(string filename)
        {
            using var reader = new StreamReader(filename);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            var records = csv.GetRecords<RawCsvClosedTradingDate>();

            return records.Select(x => x.ToMarketClosedDay()).ToList();
        }
    }
}