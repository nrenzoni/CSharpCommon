namespace CustomShared;

using System;

public class DecimalWithInf
{
    public decimal? Value { get; }

    public bool PositiveInfinity { get; }

    public bool NegativeInfinity { get; }

    public static DecimalWithInf FromDivision(
        decimal numerator,
        decimal denominator)
    {
        if (denominator != 0M)
        {
            return new DecimalWithInf(numerator / denominator);
        }

        if (numerator > 0M)
            return new DecimalWithInf(positiveInfinity: true);

        return new DecimalWithInf(negativeInfinity: true);
    }

    public DecimalWithInf(
        decimal value)
    {
        if (value == decimal.MaxValue)
        {
            PositiveInfinity = true;
            return;
        }

        if (value == decimal.MinValue)
        {
            NegativeInfinity = true;
            return;
        }

        Value = value;
    }

    public DecimalWithInf(
        decimal? value = null,
        bool positiveInfinity = false,
        bool negativeInfinity = false)
    {
        if (value is null
            && !positiveInfinity
            && !negativeInfinity)
            throw new Exception("Must have value or one infinity");

        if (value is not null
            && (positiveInfinity || negativeInfinity))
            throw new Exception("Cannot have value set and an infinity set.");

        if (positiveInfinity && negativeInfinity)
            throw new Exception("At most one infinity can be set");

        Value = value;
        PositiveInfinity = positiveInfinity;
        NegativeInfinity = negativeInfinity;
    }

    public static bool operator >(
        DecimalWithInf lhs,
        DecimalWithInf rhs)
    {
        if (lhs.Value is null
            && rhs.Value is null)
        {
            if (lhs.NegativeInfinity
                && rhs.NegativeInfinity)
                return false;
            if (lhs.PositiveInfinity
                && rhs.PositiveInfinity)
                return false;
        }

        if (lhs.Value is null)
        {
            if (lhs.PositiveInfinity)
                return true;

            return false;
        }

        if (rhs.Value is null)
        {
            if (rhs.PositiveInfinity)
                return false;

            return true;
        }

        return lhs.Value.Value > rhs.Value.Value;
    }

    public static bool operator <=(
        DecimalWithInf lhs,
        DecimalWithInf rhs)
    {
        return !(lhs > rhs);
    }

    public static bool operator >=(
        DecimalWithInf lhs,
        DecimalWithInf rhs)
    {
        return !(lhs < rhs);
    }

    public static bool operator <(
        DecimalWithInf lhs,
        DecimalWithInf rhs)
    {
        return !(rhs <= lhs);
    }

    public static DecimalWithInf Max(
        DecimalWithInf lhs,
        DecimalWithInf rhs)
    {
        return lhs > rhs
            ? lhs
            : rhs;
    }
}
