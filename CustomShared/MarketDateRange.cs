using NodaTime;

namespace CustomShared;

public class MarketDateRange
{
    public LocalDate StartDate { get; set; }

    public LocalDate EndDate { get; set; }

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
}
