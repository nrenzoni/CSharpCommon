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
        var fractionalPart = Math.Abs(value - Decimal.Truncate(value));

        var shiftCount = 19u;
        var shiftMult = Convert.ToDecimal(
            Math.Pow(
                10,
                19));
        var fractionAsInt = fractionalPart * shiftMult;
        return (Convert.ToUInt64(fractionAsInt), shiftCount);
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
