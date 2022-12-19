using System;
using System.Globalization;
using System.Linq;

namespace CustomShared;

public static class MathUtils
{
    public static decimal Max(
        decimal[] vals)
        => vals.Max();

    public static UInt64 WholePart(
        this decimal value)
    {
        return Convert.ToUInt64(Math.Truncate(value));
    }

    public static (UInt64, UInt32 shiftCount) FractionalPart(
        this decimal value)
    {
        if (value == 0M)
            return (0, 0);

        var fractionalPart = Math.Abs(value - Decimal.Truncate(value));
        var shiftedRight = fractionalPart * 10e19M;
        var shiftedDigitCountWithTrailingZeros =
            (uint)Math.Log10(Convert.ToDouble(shiftedRight)) + 1;
        
        uint significantTrailingZeros = 0;
        while (true)
        {
            if (shiftedRight % 1e5M != 0)
            {
                shiftedRight /= 1e5M;
                significantTrailingZeros += 5;
                continue;
            }

            if (shiftedRight % 10 != 0)
            {
                shiftedRight /= 10M;
                significantTrailingZeros += 1;
                continue;
            }

            break;
        }

        var shiftCount =
            shiftedDigitCountWithTrailingZeros - significantTrailingZeros;

        return (Convert.ToUInt64(fractionalPart), shiftCount);
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
        ulong wholePart,
        ulong fractional,
        uint? shiftRightCount)
    {
        var shiftCount = shiftRightCount ?? fractional.NumberOfDigits();

        return new decimal(
            wholePart + fractional / Math.Pow(
                10,
                shiftCount));
    }

    public static uint NumberOfDigits(
        this ulong n)
        => (uint)Math.Floor(Math.Log10(n) + 1);
}
