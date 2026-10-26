using System;

namespace Chronicler.Timing;

/// <summary>
/// An exact nonnegative instant relative to a simulation origin, with 2^-32-second precision.
/// </summary>
/// <remarks>
/// Values do not carry a world or lifetime identity. The owner must ensure that
/// compared or subtracted timestamps share the same origin and simulation lifetime.
/// </remarks>
public readonly struct ChronicleTimestamp : IEquatable<ChronicleTimestamp>, IComparable<ChronicleTimestamp>
{
    private readonly ChronicleDuration _value;

    /// <summary>Creates an instant from nonnegative whole seconds and an unsigned fraction.</summary>
    /// <param name="wholeSeconds">The mathematical floor of elapsed seconds since the origin.</param>
    /// <param name="fractionalSecond">The nonnegative remainder in units of 2^-32 seconds.</param>
    /// <exception cref="ArgumentOutOfRangeException">Whole seconds are negative.</exception>
    public ChronicleTimestamp(long wholeSeconds, uint fractionalSecond)
    {
        if (wholeSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(wholeSeconds), "A timestamp cannot precede its origin.");
        _value = new ChronicleDuration(wholeSeconds, fractionalSecond);
    }

    /// <summary>Gets the simulation origin, also represented by the default value.</summary>
    public static ChronicleTimestamp Zero => default;

    /// <summary>Gets the mathematical floor of elapsed seconds since the origin.</summary>
    public long WholeSeconds => _value.WholeSeconds;

    /// <summary>Gets the nonnegative remainder in units of 2^-32 seconds.</summary>
    public uint FractionalSecond => _value.FractionalSecond;

    /// <inheritdoc />
    public int CompareTo(ChronicleTimestamp other) => _value.CompareTo(other._value);

    /// <inheritdoc />
    public bool Equals(ChronicleTimestamp other) => _value.Equals(other._value);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ChronicleTimestamp other && Equals(other);

    /// <summary>Returns a deterministic collection hash of the value's components.</summary>
    public override int GetHashCode() => _value.GetHashCode();

    /// <summary>Offsets an instant by a signed interval.</summary>
    /// <exception cref="OverflowException">The result precedes the origin or exceeds the timestamp range.</exception>
    public static ChronicleTimestamp operator +(ChronicleTimestamp time, ChronicleDuration delta)
    {
        ChronicleDuration result = time._value + delta;
        if (result.WholeSeconds < 0)
            throw new OverflowException("The timestamp would precede its origin.");
        return new ChronicleTimestamp(result.WholeSeconds, result.FractionalSecond);
    }

    /// <summary>Gets the signed interval from the right instant to the left instant.</summary>
    /// <remarks>Either operand order is valid, including across the complete timestamp range.</remarks>
    public static ChronicleDuration operator -(ChronicleTimestamp left, ChronicleTimestamp right) =>
        left._value - right._value;

    /// <summary>Tests exact equality.</summary>
    public static bool operator ==(ChronicleTimestamp left, ChronicleTimestamp right) => left.Equals(right);
    /// <summary>Tests exact inequality.</summary>
    public static bool operator !=(ChronicleTimestamp left, ChronicleTimestamp right) => !left.Equals(right);
    /// <summary>Tests whether the left instant is earlier.</summary>
    public static bool operator <(ChronicleTimestamp left, ChronicleTimestamp right) => left.CompareTo(right) < 0;
    /// <summary>Tests whether the left instant is earlier or equal.</summary>
    public static bool operator <=(ChronicleTimestamp left, ChronicleTimestamp right) => left.CompareTo(right) <= 0;
    /// <summary>Tests whether the left instant is later.</summary>
    public static bool operator >(ChronicleTimestamp left, ChronicleTimestamp right) => left.CompareTo(right) > 0;
    /// <summary>Tests whether the left instant is later or equal.</summary>
    public static bool operator >=(ChronicleTimestamp left, ChronicleTimestamp right) => left.CompareTo(right) >= 0;
}
