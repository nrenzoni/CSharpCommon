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

    public static UInt64 FractionalPart(
        this decimal value)
    {
        var fractionalPart = Math.Abs(value - Decimal.Truncate(value));
        return Convert.ToUInt64(fractionalPart);
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

    public static decimal DecimalFromParts(ulong integer, ulong fractional)
    {
        return new decimal(integer + fractional / Math.Pow(10, fractional.NumberOfDigits()));
    }

    public static uint NumberOfDigits(this ulong n)
        => (uint)Math.Floor(Math.Log10(n) + 1);
}