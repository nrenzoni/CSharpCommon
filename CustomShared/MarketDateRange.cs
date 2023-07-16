using System;
using NodaTime;

namespace CustomShared;

public class MarketDateRange : IComparable<MarketDateRange>
{
    public LocalDate StartDate { get; }

    public LocalDate EndDate { get; }

    public bool IsSingleDay => StartDate == EndDate;

    public MarketDateRange(
        LocalDate date)
    {
        StartDate = date;
        EndDate = date;
    }

    public MarketDateRange(
        LocalDate startDate,
        LocalDate endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }

    public override string ToString()
    {
        var innerText = $"{StartDate.ToYYYYMMDD()}";
        if (!IsSingleDay)
            innerText += $", {EndDate.ToYYYYMMDD()}";
        return $"[{innerText}]";
    }

    public bool ContainsDate(
        LocalDate date)
    {
        return StartDate <= date && date <= EndDate;
    }

    public bool Equals(
        MarketDateRange other)
    {
        if (ReferenceEquals(
                null,
                other))
            return false;
        if (ReferenceEquals(
                this,
                other))
            return true;

        return StartDate.Equals(other.StartDate)
               && EndDate.Equals(other.EndDate);
    }

    public override bool Equals(
        object obj)
    {
        if (ReferenceEquals(
                null,
                obj))
            return false;
        if (ReferenceEquals(
                this,
                obj))
            return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((MarketDateRange)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            StartDate,
            EndDate);
    }

    public int CompareTo(
        MarketDateRange other)
    {
        if (ReferenceEquals(
                this,
                other))
            return 0;
        if (ReferenceEquals(
                null,
                other))
            return 1;
        var startDateComparison
            = StartDate.CompareTo(other.StartDate);

        if (startDateComparison != 0)
            return startDateComparison;

        return EndDate.CompareTo(other.EndDate);
    }
}
