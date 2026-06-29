using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Chronicler;

/// <summary>
/// Writes deterministic primitive payloads into a fixed-width record hash.
/// </summary>
/// <remarks>
/// This is a non-cryptographic conformance/replay hash writer. It is intended for deterministic
/// state comparison, not for security or tamper resistance.
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

    /// <summary>
    /// Writes a stable ASCII section tag and schema version.
    /// </summary>
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

    /// <summary>
    /// Writes a Boolean value as one canonical byte.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBool(bool value)
    {
        WriteByte(value ? (byte)1 : (byte)0);
    }

    /// <summary>
    /// Writes an unsigned 8-bit value.
    /// </summary>
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

    /// <summary>
    /// Writes a signed 8-bit value by its two's-complement byte.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSByte(sbyte value)
    {
        WriteByte(unchecked((byte)value));
    }

    /// <summary>
    /// Writes a signed 16-bit value in little-endian byte order.
    /// </summary>
    public void WriteInt16(short value)
    {
        WriteUInt16(unchecked((ushort)value));
    }

    /// <summary>
    /// Writes an unsigned 16-bit value in little-endian byte order.
    /// </summary>
    public void WriteUInt16(ushort value)
    {
        WriteByte((byte)value);
        WriteByte((byte)(value >> 8));
    }

    /// <summary>
    /// Writes a signed 32-bit value in little-endian byte order.
    /// </summary>
    public void WriteInt32(int value)
    {
        WriteUInt32(unchecked((uint)value));
    }

    /// <summary>
    /// Writes an unsigned 32-bit value in little-endian byte order.
    /// </summary>
    public void WriteUInt32(uint value)
    {
        WriteByte((byte)value);
        WriteByte((byte)(value >> 8));
        WriteByte((byte)(value >> 16));
        WriteByte((byte)(value >> 24));
    }

    /// <summary>
    /// Writes a signed 64-bit value in little-endian byte order.
    /// </summary>
    public void WriteInt64(long value)
    {
        WriteUInt64(unchecked((ulong)value));
    }

    /// <summary>
    /// Writes an unsigned 64-bit value in little-endian byte order.
    /// </summary>
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

    /// <summary>
    /// Writes a UTF-16 code unit in little-endian byte order.
    /// </summary>
    public void WriteChar(char value)
    {
        WriteUInt16(value);
    }

    /// <summary>
    /// Writes a nullable string as a presence marker, length, and UTF-16 code units.
    /// </summary>
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

    /// <summary>
    /// Writes an enum value by its underlying integral bytes in little-endian byte order.
    /// </summary>
    public void WriteEnum<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        ReadOnlySpan<TEnum> enumSpan = MemoryMarshal.CreateReadOnlySpan(ref value, 1);
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(enumSpan);

        if (BitConverter.IsLittleEndian)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                WriteByte(bytes[i]);
            }

            return;
        }

        for (int i = bytes.Length - 1; i >= 0; i--)
        {
            WriteByte(bytes[i]);
        }
    }

    /// <summary>
    /// Finalizes and returns the current hash value.
    /// </summary>
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
