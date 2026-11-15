//=======================================================================
// ChronicleHash.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

using System;
using System.Globalization;

namespace Chronicler.Hashing;

/// <summary>
/// Fixed-width deterministic hash value produced by a Chronicler record-hash pass.
/// </summary>
public readonly struct ChronicleHash : IEquatable<ChronicleHash>
{
    /// <summary>Creates a hash from two 64-bit components.</summary>
    /// <param name="low">The component emitted last in the hexadecimal representation.</param>
    /// <param name="high">The component emitted first in the hexadecimal representation.</param>
    public ChronicleHash(ulong low, ulong high)
    {
        Low = low;
        High = high;
    }

    /// <summary>Gets the low component.</summary>
    /// <value>The final 16 hexadecimal digits in <see cref="ToString"/>.</value>
    public ulong Low { get; }

    /// <summary>Gets the high component.</summary>
    /// <value>The first 16 hexadecimal digits in <see cref="ToString"/>.</value>
    public ulong High { get; }

    /// <summary>Tests exact hash equality.</summary>
    /// <param name="other">The hash to compare by component.</param>
    /// <returns>True when both components match.</returns>
    public bool Equals(ChronicleHash other)
    {
        return Low == other.Low && High == other.High;
    }

    /// <summary>Tests equality with a boxed hash.</summary>
    /// <param name="obj">The object to compare. Null or another type compares unequal.</param>
    /// <returns>True when the object has this value type and both components match.</returns>
    public override bool Equals(object? obj)
    {
        return obj is ChronicleHash other && Equals(other);
    }

    /// <summary>Hashes the hash for use in collections.</summary>
    /// <returns>A deterministic 32-bit mix of both components.</returns>
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

    /// <summary>Formats the hash for display or comparison.</summary>
    /// <returns>Exactly 32 lowercase hexadecimal digits, high component first, with leading zeroes and no separators.</returns>
    public override string ToString()
    {
        return High.ToString("x16", CultureInfo.InvariantCulture)
            + Low.ToString("x16", CultureInfo.InvariantCulture);
    }

    /// <summary>Tests whether the left hash exactly equals the right hash.</summary>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand of the comparison.</param>
    /// <returns>True when the left operand exactly equals the right operand.</returns>
    public static bool operator ==(ChronicleHash left, ChronicleHash right)
    {
        return left.Equals(right);
    }

    /// <summary>Tests whether the left hash differs from the right hash.</summary>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand of the comparison.</param>
    /// <returns>True when the left operand differs from the right operand.</returns>
    public static bool operator !=(ChronicleHash left, ChronicleHash right)
    {
        return !left.Equals(right);
    }
}
