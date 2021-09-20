using System;
using System.Collections.Generic;
using NodaTime;
using NodaTime.Extensions;
using NodaTime.Text;

namespace CustomShared
{
    public static class DateUtils
    {
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

        public static ZonedDateTime CreateNyDateTime(this DateTime dateTime)
        {
            return new(dateTime.ToUniversalTime().ToInstant(), NyDateTz);
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
        {
            return new ZonedDateTime(dateTime.ToInstant(), NyDateTz).ToDateTimeUtc();
            // return TimeZoneInfo.ConvertTimeToUtc(dateTime, NyTzInfo);
        }

        public static LocalDate ParseToLocalDate(this string formattedDateStr)
            => LocalDatePattern.Parse(formattedDateStr).Value;

        public static LocalTime ParseToLocalTime(this string formattedTimeStr)
            => LocalTimePattern.Parse(formattedTimeStr).Value;

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
    }
}