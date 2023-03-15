namespace CustomShared;

public record IndexedValuePoints<TIndex>(
    decimal Value,
    TIndex Index);
