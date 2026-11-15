//=======================================================================
// ChronicleTimestamp.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

using System;

namespace Chronicler.Timing;

/// <summary>
/// An exact nonnegative instant relative to a simulation origin, with 2^-32-second precision.
/// </summary>
/// <remarks>
/// The range is <c>(0, 0)</c> through <c>(long.MaxValue, uint.MaxValue)</c>.
/// The owner matches origins and lifetimes before comparing or subtracting timestamps.
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

    /// <summary>Gets the simulation origin.</summary>
    /// <value>Both components are zero, which is also the default value.</value>
    public static ChronicleTimestamp Zero => default;

    /// <summary>Gets floor seconds for this instant.</summary>
    /// <value>Nonnegative whole seconds since the simulation origin.</value>
    public long WholeSeconds => _value.WholeSeconds;

    /// <summary>Gets the remainder after floor seconds.</summary>
    /// <value>A nonnegative count of 2^-32-second units, from zero through 2^32 − 1.</value>
    public uint FractionalSecond => _value.FractionalSecond;

    /// <summary>Orders this instant relative to another.</summary>
    /// <param name="other">An instant from the same origin and lifetime.</param>
    /// <returns>A negative value for less, zero for equal, and a positive value for greater.</returns>
    public int CompareTo(ChronicleTimestamp other) => _value.CompareTo(other._value);

    /// <summary>Tests exact instant equality.</summary>
    /// <param name="other">An instant from the same origin and lifetime.</param>
    /// <returns>True when both components match.</returns>
    public bool Equals(ChronicleTimestamp other) => _value.Equals(other._value);

    /// <summary>Tests equality with a boxed instant.</summary>
    /// <param name="obj">The object to compare. Null or another type compares unequal.</param>
    /// <returns>True when the object has this value type and both components match.</returns>
    public override bool Equals(object? obj) => obj is ChronicleTimestamp other && Equals(other);

    /// <summary>Hashes the instant for use in collections.</summary>
    /// <returns>A deterministic 32-bit mix of both components.</returns>
    public override int GetHashCode() => _value.GetHashCode();

    /// <summary>Offsets an instant by a signed interval.</summary>
    /// <param name="time">The starting instant in the current timeline.</param>
    /// <param name="delta">The signed displacement in simulation seconds.</param>
    /// <returns>The resulting instant relative to the same origin.</returns>
    /// <exception cref="OverflowException">The result precedes the origin or exceeds the timestamp range.</exception>
    public static ChronicleTimestamp operator +(ChronicleTimestamp time, ChronicleDuration delta)
    {
        ChronicleDuration result = time._value + delta;
        if (result.WholeSeconds < 0)
            throw new OverflowException("The timestamp would precede its origin.");
        return new ChronicleTimestamp(result.WholeSeconds, result.FractionalSecond);
    }

    /// <summary>Measures the signed interval between two instants.</summary>
    /// <param name="left">The ending instant.</param>
    /// <param name="right">The starting instant from the same origin and lifetime.</param>
    /// <returns>The exact difference, negative when the ending instant precedes the start.</returns>
    public static ChronicleDuration operator -(ChronicleTimestamp left, ChronicleTimestamp right) =>
        left._value - right._value;

    /// <summary>Tests whether the left instant exactly equals the right instant.</summary>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand from the same origin and lifetime.</param>
    /// <returns>True when the left operand exactly equals the right operand.</returns>
    public static bool operator ==(ChronicleTimestamp left, ChronicleTimestamp right) => left.Equals(right);
    /// <summary>Tests whether the left instant differs from the right instant.</summary>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand from the same origin and lifetime.</param>
    /// <returns>True when the left operand differs from the right operand.</returns>
    public static bool operator !=(ChronicleTimestamp left, ChronicleTimestamp right) => !left.Equals(right);
    /// <summary>Tests whether the left instant is less than the right instant.</summary>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand from the same origin and lifetime.</param>
    /// <returns>True when the left operand is less than the right operand.</returns>
    public static bool operator <(ChronicleTimestamp left, ChronicleTimestamp right) => left.CompareTo(right) < 0;
    /// <summary>Tests whether the left instant is less than or equal to the right instant.</summary>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand from the same origin and lifetime.</param>
    /// <returns>True when the left operand is less than or equal to the right operand.</returns>
    public static bool operator <=(ChronicleTimestamp left, ChronicleTimestamp right) => left.CompareTo(right) <= 0;
    /// <summary>Tests whether the left instant is greater than the right instant.</summary>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand from the same origin and lifetime.</param>
    /// <returns>True when the left operand is greater than the right operand.</returns>
    public static bool operator >(ChronicleTimestamp left, ChronicleTimestamp right) => left.CompareTo(right) > 0;
    /// <summary>Tests whether the left instant is greater than or equal to the right instant.</summary>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand from the same origin and lifetime.</param>
    /// <returns>True when the left operand is greater than or equal to the right operand.</returns>
    public static bool operator >=(ChronicleTimestamp left, ChronicleTimestamp right) => left.CompareTo(right) >= 0;
}
