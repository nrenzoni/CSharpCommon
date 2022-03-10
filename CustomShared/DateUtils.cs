using System;
using System.Collections.Generic;
using log4net;
using NodaTime;
using NodaTime.Extensions;
using NodaTime.Text;

namespace CustomShared;

public static class DateUtils
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(DateUtils));

    public static string NyTzStringSpecifier = "America/New_York";

    public static readonly DateTimeZone NyDateTz = DateTimeZoneProviders.Tzdb[NyTzStringSpecifier];

    // public static readonly TimeZoneInfo NyTzInfo = TimeZoneInfo.FindSystemTimeZoneById(NyTzStringSpecifier);
    public static readonly string YYYYMMDDString = "yyyy-MM-dd";
    public static readonly string TimePattern = "HH:mm";

    public static readonly LocalDatePattern LocalDatePattern =
        LocalDatePattern.CreateWithCurrentCulture(YYYYMMDDString);

    public static readonly LocalTimePattern LocalTimePattern =
        LocalTimePattern.CreateWithCurrentCulture(TimePattern);

    public static IList<LocalDate> EachLocalDay(LocalDate from, LocalDate thru)
    {
        List<LocalDate> dates = new();
        for (var day = from; day <= thru; day += Period.FromDays(1))
            dates.Add(day);
        return dates;
    }

    public static TimeSpan ToTimeSpan(this LocalTime time)
        => TimeSpan.FromTicks(time.TickOfDay);

    public static string ToYYYYMMDD(this DateTime dateTime)
        => dateTime.ToString("yyyy-MM-dd");

    public static string ToYYYYMMDD(this LocalDate date)
    {
        return date.ToString(YYYYMMDDString, null);
    }

    public static DateTime ForceDateTimeUtc(this DateTime dateTime)
        => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);

    public static ZonedDateTime CreateNyDateTime(this DateTime dateTime)
    {
        if (dateTime.Kind == DateTimeKind.Local)
        {
            return LocalDateTime.FromDateTime(dateTime).InZoneStrictly(NyDateTz);
        }

        if (dateTime.Kind == DateTimeKind.Unspecified)
        {
            Log.Warn($"datetime kind is unspecified for {dateTime}. Coercing to Utc.");
            dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }

        var instant = Instant.FromDateTimeUtc(dateTime);
        return instant.InZone(NyDateTz);
    }

    public static ZonedDateTime MillisSinceEpochToNyTzDatetime(this long millisSinceEpoch)
    {
        return Instant.FromUnixTimeMilliseconds(millisSinceEpoch).InZone(NyDateTz);
    }

    public static ZonedDateTime GetCurrentNyTzTime()
    {
        return SystemClock.Instance.GetCurrentInstant().InZone(NyDateTz);
    }

    public static Instant GetCurrentInstant()
    {
        return SystemClock.Instance.GetCurrentInstant();
    }

    public static DateTime AtLastMinuteOfDay(this DateTime dateTime)
    {
        return dateTime.Date + new TimeSpan(23, 59, 59);
    }

    public static DateTime ConvertToNyTimeFromUtc(DateTime dateInUtc)
    {
        if (dateInUtc.Kind != DateTimeKind.Utc)
            throw new Exception("Passed datetime must be UTC kind.");

        return CreateNyDateTime(dateInUtc).LocalDateTime.ToDateTimeUnspecified();
        // return TimeZoneInfo.ConvertTimeFromUtc(dateInUtc, NyTzInfo);
    }

    // ignores input dateTime type.
    public static DateTime ConvertToUtcFromNy(this DateTime dateTime)
        => NyDateTz.AtStrictly(LocalDateTime.FromDateTime(dateTime)).ToDateTimeUtc();

    public static LocalDate ParseToLocalDate(this string formattedDateStr)
        => LocalDatePattern.Parse(formattedDateStr).Value;

    public static LocalTime ParseToLocalTime(this string formattedTimeStr)
        => LocalTimePattern.Parse(formattedTimeStr).Value;

    public static string ToTimeString(this LocalTime time)
        => time.ToString("HH:mm:ss", null);

    public static LocalDate GetWorkdayOffset(this LocalDate date, int dayOffset)
    {
        if (dayOffset == 0)
            throw new Exception("dayOffset must be integer != 0");

        for (var i = 1; i <= Math.Abs(dayOffset); i++)
        {
            do
            {
                date = date + Period.FromDays(Math.Sign(dayOffset));
            } while (IsWeekend(date));
        }

        return date;
    }

    public static LocalDate GetPreviousWorkDay(this LocalDate date)
        => date.GetWorkdayOffset(-1);

    public static bool IsWeekend(LocalDate date)
    {
        return date.DayOfWeek == IsoDayOfWeek.Saturday ||
               date.DayOfWeek == IsoDayOfWeek.Sunday;
    }

    public static DateTime RoundToTicks(this DateTime target, long ticks) =>
        new DateTime((target.Ticks + ticks / 2) / ticks * ticks, target.Kind);

    public static DateTime RoundUpToTicks(this DateTime target, long ticks) =>
        new DateTime((target.Ticks + ticks - 1) / ticks * ticks, target.Kind);

    public static DateTime RoundDownToTicks(this DateTime target, long ticks) =>
        new DateTime(target.Ticks / ticks * ticks, target.Kind);

    public static DateTime Round(this DateTime target, TimeSpan round) => RoundToTicks(target, round.Ticks);
    public static DateTime RoundUp(this DateTime target, TimeSpan round) => RoundUpToTicks(target, round.Ticks);
    public static DateTime RoundDown(this DateTime target, TimeSpan round) => RoundDownToTicks(target, round.Ticks);

    public static List<LocalTime> GetTimesInInterval(LocalTime firstTime, LocalTime lastTime, Period period)
    {
        var times = new List<LocalTime>();
        var currentTime = firstTime;
        while (currentTime <= lastTime)
        {
            times.Add(currentTime);
            currentTime += period;
        }

        return times;
    }

    public static LocalDate GetNyDate(this Instant instant) => instant.InZone(NyDateTz).Date;
    
    public static LocalTime GetNyLocalTime(this Instant instant) => instant.InZone(NyDateTz).TimeOfDay;

    public static ZonedDateTime ToNyTime(
        this LocalDateTime localDateTime)
        => localDateTime.InZoneStrictly(NyDateTz);

    public static List<LocalDate> RangeOfDates(LocalDate beginDate, LocalDate endDate)
    {
        List<LocalDate> dates = new();
        var oneDay = Period.FromDays(1);
        var date = beginDate;
        while (date < endDate)
        {
            dates.Add(date);
            date += oneDay;
        }

        return dates;
    }
    
    public static IEnumerable<Instant> GetInstantsInRange(
        Instant startRange, Instant endRange, Duration offset)
    {
        Instant nextInstant = startRange;
        while (nextInstant < endRange)
        {
            yield return nextInstant;
            nextInstant += offset;
        }
    }
}