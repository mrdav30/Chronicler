using System;

namespace Chronicler.Timing;

/// <summary>
/// An exact signed interval with 64-bit whole seconds and 32 binary fractional bits.
/// </summary>
/// <remarks>
/// Whole seconds are rounded toward negative infinity. Negative one fractional
/// unit is (-1, uint.MaxValue). Arithmetic throws on overflow rather than saturating.
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

    /// <summary>Gets the zero interval, also represented by the default value.</summary>
    public static ChronicleDuration Zero => default;

    /// <summary>Gets the mathematical floor of the interval in seconds.</summary>
    public long WholeSeconds { get; }

    /// <summary>Gets the nonnegative remainder in units of 2^-32 seconds.</summary>
    public uint FractionalSecond { get; }

    /// <inheritdoc />
    public int CompareTo(ChronicleDuration other)
    {
        int seconds = WholeSeconds.CompareTo(other.WholeSeconds);
        return seconds != 0 ? seconds : FractionalSecond.CompareTo(other.FractionalSecond);
    }

    /// <inheritdoc />
    public bool Equals(ChronicleDuration other) =>
        WholeSeconds == other.WholeSeconds && FractionalSecond == other.FractionalSecond;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ChronicleDuration other && Equals(other);

    /// <summary>Returns a deterministic collection hash of the value's components.</summary>
    public override int GetHashCode() => unchecked(
        ((int)WholeSeconds ^ (int)(WholeSeconds >> 32)) * 397 ^ (int)FractionalSecond);

    /// <summary>Adds two exact intervals.</summary>
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

    /// <summary>Subtracts two exact intervals.</summary>
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

    /// <summary>Tests exact equality.</summary>
    public static bool operator ==(ChronicleDuration left, ChronicleDuration right) => left.Equals(right);
    /// <summary>Tests exact inequality.</summary>
    public static bool operator !=(ChronicleDuration left, ChronicleDuration right) => !left.Equals(right);
    /// <summary>Tests whether the left interval is shorter than the right interval.</summary>
    public static bool operator <(ChronicleDuration left, ChronicleDuration right) => left.CompareTo(right) < 0;
    /// <summary>Tests whether the left interval is shorter than or equal to the right interval.</summary>
    public static bool operator <=(ChronicleDuration left, ChronicleDuration right) => left.CompareTo(right) <= 0;
    /// <summary>Tests whether the left interval is longer than the right interval.</summary>
    public static bool operator >(ChronicleDuration left, ChronicleDuration right) => left.CompareTo(right) > 0;
    /// <summary>Tests whether the left interval is longer than or equal to the right interval.</summary>
    public static bool operator >=(ChronicleDuration left, ChronicleDuration right) => left.CompareTo(right) >= 0;
}
