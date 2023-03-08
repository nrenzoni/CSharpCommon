using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CustomShared;

public static class MathUtils
{
    public static decimal Max(
        decimal[] vals)
        => vals.Max();

    public static Int64 WholePart(
        this decimal value)
    {
        return Convert.ToInt64(Math.Truncate(value));
    }

    public static UInt64 WholePositiveOnly(
        this decimal value)
    {
        return Convert.ToUInt64(Math.Abs(Math.Truncate(value)));
    }

    // max fractionalPart of 19 places
    public static (UInt64 fractionalPart, UInt32 shiftCount) FractionalPart(
        this decimal value)
    {
        if (value == 0M)
            return (0, 0);

        var fractionalPart = Math.Abs(
            Math.Round(
                value,
                19)
            - Decimal.Truncate(value));

        if (fractionalPart == 0M)
            return (0, 0);

        var shiftedRight = fractionalPart * 10e19M;

        uint significantTrailingZeros = 0;
        while (true)
        {
            if (shiftedRight % 1e10M == 0)
            {
                shiftedRight /= 1e10M;
                significantTrailingZeros += 10;
                continue;
            }

            if (shiftedRight % 1e5M == 0)
            {
                shiftedRight /= 1e5M;
                significantTrailingZeros += 5;
                continue;
            }

            if (shiftedRight % 10 == 0)
            {
                shiftedRight /= 10M;
                significantTrailingZeros += 1;
                continue;
            }

            break;
        }

        var shiftCount =
            19u - significantTrailingZeros + 1;

        return (Convert.ToUInt64(shiftedRight), shiftCount);
    }

    public static String FmtOnlyFractional(
        this Decimal value)
    {
        Decimal fractionalPart = Math.Abs(value - Decimal.Truncate(value));
        return (fractionalPart == 0)
            ? String.Empty
            : fractionalPart.ToString(
                    format: ".############################",
                    provider: CultureInfo.InvariantCulture)
                .Substring(startIndex: 1);
    }

    public static int BoolToInt(
        this bool inBool)
        => inBool
            ? 1
            : 0;

    public static decimal DecimalFromParts(
        long wholePart,
        ulong fractional,
        uint? shiftRightCount)
    {
        var shiftCount = shiftRightCount ?? fractional.NumberOfDigits();

        var frac = fractional / Math.Pow(
            10,
            shiftCount);

        return new decimal(
            wholePart + frac);
    }

    public static decimal DecimalFromParts(
        UInt64 wholePart,
        ulong fractional,
        uint? shiftRightCount,
        bool isNegative)
    {
        var shiftCount = shiftRightCount ?? fractional.NumberOfDigits();

        var frac = fractional / Math.Pow(
            10,
            shiftCount);

        var posDec = new decimal(
            wholePart + frac);

        if (isNegative)
            return -1 * posDec;
        return posDec;
    }

    public static uint NumberOfDigits(
        this ulong n)
        => (uint)Math.Floor(Math.Log10(n) + 1);

    public static decimal TruncateDecimalPlaces(
        this decimal d,
        byte decimalPlacesToKeep)
    {
        decimal r = Math.Round(
            d,
            decimalPlacesToKeep);

        return d switch
        {
            > 0 when r > d => r - new decimal(
                1,
                0,
                0,
                false,
                decimalPlacesToKeep),
            < 0 when r < d => r + new decimal(
                1,
                0,
                0,
                false,
                decimalPlacesToKeep),
            _ => r
        };
    }

    public static DecimalWithInf TruncateDecimalPlaces(
        this DecimalWithInf d,
        byte decimalPlacesToKeep)
    {
        if (d.Value.HasValue)
            return new DecimalWithInf(
                d.Value.Value.TruncateDecimalPlaces(
                    decimalPlacesToKeep));

        return d;
    }

    public static IEnumerable<decimal> UnrollRange(
        decimal begin,
        decimal end,
        decimal step,
        bool beginInclusive,
        bool endInclusive)
    {
        var curr = begin;

        if (!beginInclusive)
            curr += step;

        if (endInclusive)
        {
            while (true)
            {
                if (curr > end)
                    yield break;

                yield return curr;

                curr += step;
            }
        }

        // !endInclusive
        while (true)
        {
            if (curr >= end)
                yield break;

            yield return curr;

            curr += step;
        }
    }
}