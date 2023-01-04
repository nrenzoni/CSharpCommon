using LocalDate = NodaTime.LocalDate;

namespace GrpcShared;

public static class GrpcCommonConverters
{
    public static LocalDate Convert(
        CommonProto.LocalDate inLocalDate)
    {
        return new LocalDate(
            (int)inLocalDate.Year,
            (int)inLocalDate.Month,
            (int)inLocalDate.Date);
    }

    public static CommonProto.LocalDate Convert(
        LocalDate inLocalDate)
    {
        return new CommonProto.LocalDate
        {
            Year = (uint)inLocalDate.Year,
            Month = (uint)inLocalDate.Month,
            Date = (uint)inLocalDate.Day
        };
    }
}
