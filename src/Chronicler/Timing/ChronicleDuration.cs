//=======================================================================
// ChronicleDuration.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

using System;

namespace Chronicler.Timing;

/// <summary>
/// An exact signed interval with 64-bit whole seconds and 32 binary fractional bits.
/// </summary>
/// <remarks>
/// Whole seconds use mathematical floor. Negative one fractional unit is <c>(-1, uint.MaxValue)</c>.
/// The range is <c>(long.MinValue, 0)</c> through <c>(long.MaxValue, uint.MaxValue)</c>.
/// Arithmetic throws when the final result exceeds this range.
/// </remarks>
public readonly struct ChronicleDuration : IEquatable<ChronicleDuration>, IComparable<ChronicleDuration>
{
    /// <summary>Creates an interval from floor seconds and an unsigned fraction.</summary>
    /// <param name="wholeSeconds">The mathematical floor of the interval in seconds.</param>
    /// <param name="fractionalSecond">The nonnegative remainder in units of 2^-32 seconds.</param>
    public ChronicleDuration(long wholeSeconds, uint fractionalSecond)
    {
        WholeSeconds = wholeSeconds;
        FractionalSecond = fractionalSecond;
    }

    /// <summary>Gets the zero interval.</summary>
    /// <value>Both components are zero, which is also the default value.</value>
    public static ChronicleDuration Zero => default;

    /// <summary>Gets floor seconds for this interval.</summary>
    /// <value>The signed mathematical floor, including for negative fractions.</value>
    public long WholeSeconds { get; }

    /// <summary>Gets the remainder after floor seconds.</summary>
    /// <value>A nonnegative count of 2^-32-second units, from zero through 2^32 − 1.</value>
    public uint FractionalSecond { get; }

    /// <summary>Orders this interval relative to another.</summary>
    /// <param name="other">The interval to order against.</param>
    /// <returns>A negative value for less, zero for equal, and a positive value for greater.</returns>
    public int CompareTo(ChronicleDuration other)
    {
        int seconds = WholeSeconds.CompareTo(other.WholeSeconds);
        return seconds != 0 ? seconds : FractionalSecond.CompareTo(other.FractionalSecond);
    }

    /// <summary>Tests exact interval equality.</summary>
    /// <param name="other">The interval to compare by component.</param>
    /// <returns>True when both components match.</returns>
    public bool Equals(ChronicleDuration other) =>
        WholeSeconds == other.WholeSeconds && FractionalSecond == other.FractionalSecond;

    /// <summary>Tests equality with a boxed interval.</summary>
    /// <param name="obj">The object to compare. Null or another type compares unequal.</param>
    /// <returns>True when the object has this value type and both components match.</returns>
    public override bool Equals(object? obj) => obj is ChronicleDuration other && Equals(other);

    /// <summary>Hashes the interval for use in collections.</summary>
    /// <returns>A deterministic 32-bit mix of both components.</returns>
    public override int GetHashCode() => unchecked(
        ((int)WholeSeconds ^ (int)(WholeSeconds >> 32)) * 397 ^ (int)FractionalSecond);

    /// <summary>Adds two exact intervals.</summary>
    /// <param name="left">The initial interval.</param>
    /// <param name="right">The interval to add, which may be negative.</param>
    /// <returns>The exact accumulated duration.</returns>
    /// <exception cref="OverflowException">The final interval is outside the signed whole-second range.</exception>
    public static ChronicleDuration operator +(ChronicleDuration left, ChronicleDuration right)
    {
        ulong fraction = (ulong)left.FractionalSecond + right.FractionalSecond;
        long seconds = unchecked(left.WholeSeconds + right.WholeSeconds + (long)(fraction >> 32));
        // Test the final sign after carrying; the intermediate sum may overflow and recover.
        if ((left.WholeSeconds ^ right.WholeSeconds) >= 0 && (left.WholeSeconds ^ seconds) < 0)
            throw new OverflowException("The duration sum is outside the supported range.");
        return new ChronicleDuration(seconds, unchecked((uint)fraction));
    }

    /// <summary>Subtracts one interval from another.</summary>
    /// <param name="left">The initial interval.</param>
    /// <param name="right">The interval to subtract, which may be negative.</param>
    /// <returns>The exact remaining duration.</returns>
    /// <exception cref="OverflowException">The final interval is outside the signed whole-second range.</exception>
    public static ChronicleDuration operator -(ChronicleDuration left, ChronicleDuration right)
    {
        ulong fraction = unchecked((ulong)left.FractionalSecond - right.FractionalSecond);
        // Fractional underflow sets bit 63; subtract that borrow before testing the final sign.
        long seconds = unchecked(left.WholeSeconds - right.WholeSeconds - (long)(fraction >> 63));
        if ((left.WholeSeconds ^ right.WholeSeconds) < 0 && (left.WholeSeconds ^ seconds) < 0)
            throw new OverflowException("The duration difference is outside the supported range.");
        return new ChronicleDuration(seconds, unchecked((uint)fraction));
    }

    /// <summary>Tests whether the left interval exactly equals the right interval.</summary>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand of the comparison.</param>
    /// <returns>True when the left operand exactly equals the right operand.</returns>
    public static bool operator ==(ChronicleDuration left, ChronicleDuration right) => left.Equals(right);
    /// <summary>Tests whether the left interval differs from the right interval.</summary>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand of the comparison.</param>
    /// <returns>True when the left operand differs from the right operand.</returns>
    public static bool operator !=(ChronicleDuration left, ChronicleDuration right) => !left.Equals(right);
    /// <summary>Tests whether the left interval is less than the right interval.</summary>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand of the comparison.</param>
    /// <returns>True when the left operand is less than the right operand.</returns>
    public static bool operator <(ChronicleDuration left, ChronicleDuration right) => left.CompareTo(right) < 0;
    /// <summary>Tests whether the left interval is less than or equal to the right interval.</summary>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand of the comparison.</param>
    /// <returns>True when the left operand is less than or equal to the right operand.</returns>
    public static bool operator <=(ChronicleDuration left, ChronicleDuration right) => left.CompareTo(right) <= 0;
    /// <summary>Tests whether the left interval is greater than the right interval.</summary>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand of the comparison.</param>
    /// <returns>True when the left operand is greater than the right operand.</returns>
    public static bool operator >(ChronicleDuration left, ChronicleDuration right) => left.CompareTo(right) > 0;
    /// <summary>Tests whether the left interval is greater than or equal to the right interval.</summary>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand of the comparison.</param>
    /// <returns>True when the left operand is greater than or equal to the right operand.</returns>
    public static bool operator >=(ChronicleDuration left, ChronicleDuration right) => left.CompareTo(right) >= 0;
}
