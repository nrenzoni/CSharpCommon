using NodaTime;

namespace CustomShared;

public record TimeValuePoint<T>(
    T Value,
    Instant Time);