using CommonProto;
using CustomShared;
using NodaTime;
using DecimalWithInf = CustomShared.DecimalWithInf;
using LocalDate = NodaTime.LocalDate;

namespace GrpcShared;

public static class GrpcCommonConverters
{
    public static TTo? ConvertIfNotNull<TFrom, TTo>(
        Func<TFrom, TTo> convertFunc,
        TFrom? input)
        where TTo : struct
    {
        return input == null
            ? null
            : convertFunc(input);
    }

    public static TTo? ConvertIfNotNull<TFrom, TTo>(
        Func<TFrom, TTo> convertFunc,
        TFrom? input)
        where TFrom : struct
        where TTo : class?
    {
        return input == null
            ? null
            : convertFunc(input.Value);
    }

    public static TTo? ConvertIfNotNull2<TFrom, TTo>(
        Func<TFrom, TTo> convertFunc,
        TFrom? input)
        where TTo : class?
    {
        return input == null
            ? null
            : convertFunc(input);
    }

    public static CommonProto.Decimal Convert(
        decimal inDecimal)
    {
        var decimalRet = new CommonProto.Decimal();
        decimalRet.Whole = inDecimal.WholePositiveOnly();
        var (decimalRetFraction, shiftCount) = inDecimal.FractionalPart();
        decimalRet.Fraction = decimalRetFraction;
        decimalRet.FractionShiftLeft = shiftCount;
        decimalRet.IsNegative = inDecimal < 0;
        return decimalRet;
    }

    public static decimal Convert(
        CommonProto.Decimal inDecimal)
    {
        return MathUtils.DecimalFromParts(
            inDecimal.Whole,
            inDecimal.Fraction,
            inDecimal.FractionShiftLeft,
            inDecimal.IsNegative);
    }

    public static CommonProto.DecimalWithInf Convert(
        DecimalWithInf inDec)
    {
        var decRet = new CommonProto.DecimalWithInf();
        if (inDec.Value is null)
        {
            var inf = new CommonProto.Inf
            {
                NegativeInf = inDec.NegativeInfinity
            };
            decRet.Inf = inf;
        }
        else
        {
            decRet.Value = Convert(inDec.Value.Value);
        }

        return decRet;
    }

    public static DecimalWithInf Convert(
        CommonProto.DecimalWithInf inDecimalWithInf)
    {
        switch (inDecimalWithInf.KindCase)
        {
            case CommonProto.DecimalWithInf.KindOneofCase.Value:
                return new DecimalWithInf(Convert(inDecimalWithInf.Value));
            case CommonProto.DecimalWithInf.KindOneofCase.Inf:
                var negInf = inDecimalWithInf.Inf.NegativeInf;
                return new DecimalWithInf(
                    !negInf,
                    negInf);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

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

    public static (string, decimal) Convert(
        KeyValueDecimal inKeyValueDecimal)
        => (inKeyValueDecimal.Key, Convert(inKeyValueDecimal.Value));

    public static List<KeyValueDecimal> Convert(
        IDictionary<string, decimal> inDictionary)
    {
        return inDictionary.Select(Convert).ToList();
    }

    public static KeyValueDecimal Convert(
        KeyValuePair<string, decimal> inKeyValueDecimal)
    {
        return new KeyValueDecimal
        {
            Key = inKeyValueDecimal.Key,
            Value = Convert(inKeyValueDecimal.Value)
        };
    }

    public static UnixTicks Convert(
        Instant time)
    {
        return new UnixTicks
        {
            Ticks = time.ToUnixTimeTicks()
        };
    }

    public static Instant Convert(
        UnixTicks unixTicks)
    {
        return Instant.FromUnixTimeTicks(unixTicks.Ticks);
    }

    public static StringsPerDayMap Convert(
        Dictionary<LocalDate, List<string>> inStringsPerDay)
    {
        var outStringsPerDayMap = new StringsPerDayMap();

        foreach (var (day, strings) in inStringsPerDay)
        {
            var stringsPerDay = new StringsPerDay()
            {
                Date = Convert(day)
            };
            var newListOfStrings = new ListOfStrings();
            newListOfStrings.Values.AddRange(strings);

            stringsPerDay.Strings = newListOfStrings;

            outStringsPerDayMap.StringsPerDay.Add(stringsPerDay);
        }

        return outStringsPerDayMap;
    }

    public static Dictionary<LocalDate, List<string>> Convert(
        StringsPerDayMap inStringsPerDay)
    {
        var outDic = new Dictionary<LocalDate, List<string>>();

        foreach (var stringsPerDay in inStringsPerDay.StringsPerDay)
        {
            var localDate = Convert(stringsPerDay.Date);
            outDic[localDate] = stringsPerDay.Strings.Values.ToList();
        }

        return outDic;
    }

    public static Dictionary<string, object> Convert(
        NestedObjectDictionary inNestedDictionary)
    {
        return new Dictionary<string, object>();
    }

    public static NestedObjectDictionary Convert(
        Dictionary<string, object> inNestedDictionary)
    {
        return new NestedObjectDictionary();
    }

    public static DecimalIndexedValuePoint Convert(
        (decimal index, decimal value) inIndexValue)
    {
        return new DecimalIndexedValuePoint
        {
            Index = Convert(inIndexValue.index),
            Value = Convert(inIndexValue.value)
        };
    }

    public static (decimal index, decimal value) Convert(
        DecimalIndexedValuePoint inDecimalIndexedValuePoint)
    {
        return (
            Convert(inDecimalIndexedValuePoint.Index),
            Convert(inDecimalIndexedValuePoint.Value));
    }

    public static MarketDateRange Convert(
        DateRangeIncl inDateRange)
    {
        var startDate = Convert(inDateRange.StartDate);
        var endDate = Convert(inDateRange.EndDateIncl);

        return new MarketDateRange(
            startDate,
            endDate);
    }

    public static DateRangeIncl Convert(
        MarketDateRange inDateRange)
    {
        var startDate = Convert(inDateRange.StartDate);
        var endDateIncl = Convert(inDateRange.EndDate);

        return new DateRangeIncl
        {
            StartDate = startDate,
            EndDateIncl = endDateIncl
        };
    }

    public static StringDayPair Convert(
        (string, LocalDate) inStringDayPair)
    {
        var date = Convert(inStringDayPair.Item2);

        return new StringDayPair
        {
            String = inStringDayPair.Item1,
            Date = date
        };
    }

    public static (string, LocalDate) Convert(
        StringDayPair inStringDayPair)
    {
        return (inStringDayPair.String, GrpcCommonConverters.Convert(inStringDayPair.Date));
    }
}