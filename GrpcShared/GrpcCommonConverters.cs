using CommonProto;
using CustomShared;
using DecimalWithInf = CustomShared.DecimalWithInf;
using LocalDate = NodaTime.LocalDate;

namespace GrpcShared;

public static class GrpcCommonConverters
{
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

    public static KeyValueDecimal Convert(
        KeyValuePair<string, decimal> inKeyValueDecimal)
    {
        return new KeyValueDecimal
        {
            Key = inKeyValueDecimal.Key,
            Value = Convert(inKeyValueDecimal.Value)
        };
    }
}
