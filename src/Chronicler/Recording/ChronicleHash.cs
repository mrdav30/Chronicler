using System;
using System.Globalization;

namespace Chronicler;

/// <summary>
/// Fixed-width deterministic hash value produced by a Chronicler record-hash pass.
/// </summary>
public readonly struct ChronicleHash : IEquatable<ChronicleHash>
{
    /// <summary>
    /// Creates a new hash value from its two 64-bit lanes.
    /// </summary>
    public ChronicleHash(ulong low, ulong high)
    {
        Low = low;
        High = high;
    }

    /// <summary>
    /// Gets the low 64-bit lane.
    /// </summary>
    public ulong Low { get; }

    /// <summary>
    /// Gets the high 64-bit lane.
    /// </summary>
    public ulong High { get; }

    /// <inheritdoc />
    public bool Equals(ChronicleHash other)
    {
        return Low == other.Low && High == other.High;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is ChronicleHash other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            ulong mixed = Low ^ (High * 0x9e3779b97f4a7c15UL);
            mixed ^= mixed >> 33;
            mixed *= 0xff51afd7ed558ccdUL;
            mixed ^= mixed >> 33;
            return (int)(mixed ^ (mixed >> 32));
        }
    }

    /// <summary>
    /// Returns a lowercase 32-character hexadecimal hash string as high lane followed by low lane.
    /// </summary>
    public override string ToString()
    {
        return High.ToString("x16", CultureInfo.InvariantCulture)
            + Low.ToString("x16", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Compares two hash values for equality.
    /// </summary>
    public static bool operator ==(ChronicleHash left, ChronicleHash right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two hash values for inequality.
    /// </summary>
    public static bool operator !=(ChronicleHash left, ChronicleHash right)
    {
        return !left.Equals(right);
    }
}
