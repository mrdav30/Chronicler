//=======================================================================
// ChronicleHashWriter.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

using System;
using System.Runtime.CompilerServices;

namespace Chronicler.Hashing;

/// <summary>Accumulates a deterministic 128-bit record hash.</summary>
/// <remarks>
/// Each write appends to the current stream in call order. A default writer is equivalent to a newly
/// constructed writer. This non-cryptographic hash is intended for state comparison and replay validation.
/// </remarks>
public struct ChronicleHashWriter
{
    private const ulong FnvPrime = 1099511628211UL;
    private const ulong LowOffset = 14695981039346656037UL;
    private const ulong HighOffset = 7809847782465536322UL;

    private ulong _low;
    private ulong _high;
    private bool _initialized;

    /// <summary>
    /// Creates a writer seeded for an empty deterministic hash stream.
    /// </summary>
    public ChronicleHashWriter()
    {
        _low = LowOffset;
        _high = HighOffset;
        _initialized = true;
    }

    /// <summary>Appends a section header.</summary>
    /// <param name="tag">A nonempty ASCII identifier shared by writers of this schema.</param>
    /// <param name="version">The signed 32-bit schema revision to include in the hash.</param>
    /// <remarks>Writes the byte count, ASCII bytes, then version. Invalid tags leave the writer unchanged.</remarks>
    /// <exception cref="ArgumentNullException">The tag is null.</exception>
    /// <exception cref="ArgumentException">The tag is empty or contains a non-ASCII character.</exception>
    public void WriteSection(string tag, int version)
    {
        if (tag == null)
            throw new ArgumentNullException(nameof(tag));
        if (tag.Length == 0)
            throw new ArgumentException("Record hash section tags must not be empty.", nameof(tag));

        for (int i = 0; i < tag.Length; i++)
        {
            if (tag[i] > 0x7f)
                throw new ArgumentException("Record hash section tags must be stable ASCII.", nameof(tag));
        }

        WriteInt32(tag.Length);
        for (int i = 0; i < tag.Length; i++)
        {
            WriteByte((byte)tag[i]);
        }

        WriteInt32(version);
    }

    /// <summary>Appends one byte.</summary>
    /// <param name="value">False writes zero and true writes one.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBool(bool value)
    {
        WriteByte(value ? (byte)1 : (byte)0);
    }

    /// <summary>Appends one byte.</summary>
    /// <param name="value">All eight input bits are written unchanged.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(byte value)
    {
        EnsureInitialized();

        unchecked
        {
            _low ^= value;
            _low *= FnvPrime;

            _high ^= (ulong)value + 0x9e3779b97f4a7c15UL + (_low << 6) + (_low >> 2);
            _high *= FnvPrime;
        }
    }

    /// <summary>Appends one byte.</summary>
    /// <param name="value">The signed value is encoded in two's complement.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSByte(sbyte value)
    {
        WriteByte(unchecked((byte)value));
    }

    /// <summary>Appends two little-endian bytes.</summary>
    /// <param name="value">The signed value is encoded in two's complement.</param>
    public void WriteInt16(short value)
    {
        WriteUInt16(unchecked((ushort)value));
    }

    /// <summary>Appends two little-endian bytes.</summary>
    /// <param name="value">The full unsigned 16-bit range is accepted.</param>
    public void WriteUInt16(ushort value)
    {
        WriteByte((byte)value);
        WriteByte((byte)(value >> 8));
    }

    /// <summary>Appends four little-endian bytes.</summary>
    /// <param name="value">The signed value is encoded in two's complement.</param>
    public void WriteInt32(int value)
    {
        WriteUInt32(unchecked((uint)value));
    }

    /// <summary>Appends four little-endian bytes.</summary>
    /// <param name="value">The full unsigned 32-bit range is accepted.</param>
    public void WriteUInt32(uint value)
    {
        WriteByte((byte)value);
        WriteByte((byte)(value >> 8));
        WriteByte((byte)(value >> 16));
        WriteByte((byte)(value >> 24));
    }

    /// <summary>Appends eight little-endian bytes.</summary>
    /// <param name="value">The signed value is encoded in two's complement.</param>
    public void WriteInt64(long value)
    {
        WriteUInt64(unchecked((ulong)value));
    }

    /// <summary>Appends eight little-endian bytes.</summary>
    /// <param name="value">The full unsigned 64-bit range is accepted.</param>
    public void WriteUInt64(ulong value)
    {
        WriteByte((byte)value);
        WriteByte((byte)(value >> 8));
        WriteByte((byte)(value >> 16));
        WriteByte((byte)(value >> 24));
        WriteByte((byte)(value >> 32));
        WriteByte((byte)(value >> 40));
        WriteByte((byte)(value >> 48));
        WriteByte((byte)(value >> 56));
    }

    /// <summary>Appends one little-endian UTF-16 code unit.</summary>
    /// <param name="value">The single code unit to record, including an isolated surrogate.</param>
    public void WriteChar(char value)
    {
        WriteUInt16(value);
    }

    /// <summary>Appends a nullable sequence of UTF-16 code units.</summary>
    /// <param name="value">Null writes a zero presence byte. A string writes a one byte, its 32-bit code-unit count, then its little-endian code units.</param>
    public void WriteString(string? value)
    {
        if (value == null)
        {
            WriteBool(false);
            return;
        }

        WriteBool(true);
        WriteInt32(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            WriteChar(value[i]);
        }
    }

    /// <summary>Appends the enum's underlying bytes in little-endian order.</summary>
    /// <typeparam name="TEnum">An enum with a one-, two-, four-, or eight-byte integral representation.</typeparam>
    /// <param name="value">The numeric bits to record, including combinations or unnamed values.</param>
    public void WriteEnum<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        if (Unsafe.SizeOf<TEnum>() == 1)
        {
            WriteByte(Unsafe.As<TEnum, byte>(ref value));
            return;
        }

        if (Unsafe.SizeOf<TEnum>() == 2)
        {
            WriteUInt16(Unsafe.As<TEnum, ushort>(ref value));
            return;
        }

        if (Unsafe.SizeOf<TEnum>() == 4)
        {
            WriteUInt32(Unsafe.As<TEnum, uint>(ref value));
            return;
        }

        WriteUInt64(Unsafe.As<TEnum, ulong>(ref value));
    }

    /// <summary>Takes a snapshot of the running hash.</summary>
    /// <returns>The finalized hash of all bytes written so far.</returns>
    /// <remarks>The writer remains available for additional writes. Repeated snapshots of the same state match.</remarks>
    public ChronicleHash ToHash()
    {
        EnsureInitialized();

        ulong low = FinalizeLane(_low);
        ulong high = FinalizeLane(_high ^ low);
        return new ChronicleHash(low, high);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        _low = LowOffset;
        _high = HighOffset;
        _initialized = true;
    }

    private static ulong FinalizeLane(ulong value)
    {
        unchecked
        {
            value ^= value >> 33;
            value *= 0xff51afd7ed558ccdUL;
            value ^= value >> 33;
            value *= 0xc4ceb9fe1a85ec53UL;
            value ^= value >> 33;
            return value;
        }
    }
}
