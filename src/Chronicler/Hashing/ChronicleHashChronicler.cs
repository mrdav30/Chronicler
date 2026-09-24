using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Chronicler.Hashing;

internal sealed class ChronicleHashChronicler : IChronicler
{
    private enum FieldKind : byte
    {
        Value = 1,
        DeepClass = 2,
        DeepStruct = 3,
        NullableDeepStruct = 4,
        Link = 5
    }

    private enum LeafKind : byte
    {
        Bool = 1,
        Byte = 2,
        SByte = 3,
        Int16 = 4,
        UInt16 = 5,
        Int32 = 6,
        UInt32 = 7,
        Int64 = 8,
        UInt64 = 9,
        Char = 10,
        String = 11,
        Enum = 12
    }

    [ThreadStatic]
    private static ChronicleHashChronicler? _cached;

    private static readonly Dictionary<Type, LeafKind> LeafKindsByType = new()
    {
        [typeof(bool)] = LeafKind.Bool,
        [typeof(byte)] = LeafKind.Byte,
        [typeof(sbyte)] = LeafKind.SByte,
        [typeof(short)] = LeafKind.Int16,
        [typeof(ushort)] = LeafKind.UInt16,
        [typeof(int)] = LeafKind.Int32,
        [typeof(uint)] = LeafKind.UInt32,
        [typeof(long)] = LeafKind.Int64,
        [typeof(ulong)] = LeafKind.UInt64,
        [typeof(char)] = LeafKind.Char,
        [typeof(string)] = LeafKind.String
    };

    private static readonly Dictionary<Type, string> StableTypeNames = new();
    private static readonly object StableTypeNameSync = new();

    private ChronicleHashWriter _writer;
    private ChronicleContext? _context;
    private bool _isInUse;

    private ChronicleHashChronicler()
    {
    }

    public ChronicleContext Context
    {
        get
        {
            if (_context == null)
                throw new InvalidOperationException("Record hash chronicler context is not active.");

            return _context;
        }
    }

    public SerializationMode Mode => SerializationMode.Saving;

    public static void Contribute(IRecordable target, ChronicleContext context, ref ChronicleHashWriter writer)
    {
        ChronicleHashChronicler chronicler = Rent();

        chronicler._writer = writer;
        chronicler._context = context;

        try
        {
            chronicler.WriteRecordStart(target.GetType());
            target.RecordData(chronicler);
            chronicler.WriteRecordEnd();
            writer = chronicler._writer;
        }
        finally
        {
            chronicler._writer = default;
            chronicler._context = null;
            chronicler._isInUse = false;
        }
    }

    public void LookValue<T>(ref T value, string name, T? defaultValue = default)
    {
        WriteFieldHeader(name, FieldKind.Value, typeof(T));
        LeafKind kind = WriteLeafKind(typeof(T), name);
        WriteLeafValue(ref value, kind);

        T declaredDefault = defaultValue!;
        WriteLeafValue(ref declaredDefault, kind);
    }

    public void LookDeep<T>(ref T value, string name)
        where T : class, IRecordable
    {
        WriteFieldHeader(name, FieldKind.DeepClass, typeof(T));

        if (value == null)
        {
            _writer.WriteBool(false);
            return;
        }

        _writer.WriteBool(true);
        WriteRecordStart(typeof(T));
        value.RecordData(this);
        WriteRecordEnd();
    }

    public void LookDeepStruct<T>(ref T value, string name)
        where T : struct, IRecordable
    {
        WriteFieldHeader(name, FieldKind.DeepStruct, typeof(T));
        WriteRecordStart(typeof(T));
        value.RecordData(this);
        WriteRecordEnd();
    }

    public void LookNullableDeep<T>(ref T? value, string name)
        where T : struct, IRecordable
    {
        WriteFieldHeader(name, FieldKind.NullableDeepStruct, typeof(T));

        if (!value.HasValue)
        {
            _writer.WriteBool(false);
            return;
        }

        _writer.WriteBool(true);
        T nestedValue = value.Value;
        WriteRecordStart(typeof(T));
        nestedValue.RecordData(this);
        WriteRecordEnd();
    }

    public void LookLink<T>(
        ref T value,
        string name,
        string? slot = null,
        RecordLinkResolveMode resolveMode = RecordLinkResolveMode.Immediate,
        Action<T>? assignLoadedValue = null)
    {
        string? normalizedSlot = string.IsNullOrEmpty(slot) ? null : slot;
        WriteFieldHeader(name, FieldKind.Link, typeof(T));
        _writer.WriteString(normalizedSlot);
        _writer.WriteEnum(resolveMode);

        if (value is null)
        {
            _writer.WriteString(null);
            return;
        }

        if (!Context.Links.TryGetReferenceId(value, out string? id, normalizedSlot))
        {
            throw new InvalidOperationException(
                $"Unable to hash link '{name}' of type {typeof(T).Name} because no stable id could be produced{FormatSlot(normalizedSlot)}.");
        }

        _writer.WriteString(id);
    }

    private static ChronicleHashChronicler Rent()
    {
        ChronicleHashChronicler? cached = _cached;
        if (cached == null)
        {
            cached = new ChronicleHashChronicler();
            _cached = cached;
        }

        if (cached._isInUse)
            return new ChronicleHashChronicler { _isInUse = true };

        cached._isInUse = true;
        return cached;
    }

    private void WriteRecordStart(Type declaredType)
    {
        _writer.WriteSection("chronicler.record", 1);
        _writer.WriteString(GetStableTypeName(declaredType));
    }

    private void WriteRecordEnd()
    {
        _writer.WriteSection("chronicler.record.end", 1);
    }

    private void WriteFieldHeader(string name, FieldKind kind, Type declaredType)
    {
        if (name == null)
            throw new ArgumentNullException(nameof(name));

        _writer.WriteString(name);
        _writer.WriteByte((byte)kind);
        _writer.WriteString(GetStableTypeName(declaredType));
    }

    private LeafKind WriteLeafKind(Type declaredType, string name)
    {
        LeafKind kind = GetLeafKind(declaredType, name);
        _writer.WriteByte((byte)kind);

        if (kind == LeafKind.Enum)
            _writer.WriteString(GetStableTypeName(Enum.GetUnderlyingType(declaredType)));

        return kind;
    }

    private static LeafKind GetLeafKind(Type declaredType, string name)
    {
        if (declaredType.IsEnum)
            return LeafKind.Enum;

        if (LeafKindsByType.TryGetValue(declaredType, out LeafKind kind))
            return kind;

        throw new NotSupportedException($"Unsupported record-hash leaf value '{name}' of type {GetStableTypeName(declaredType)}.");
    }

    private void WriteLeafValue<T>(ref T value, LeafKind kind)
    {
        if (kind == LeafKind.String)
        {
            _writer.WriteString(value as string);
            return;
        }

        _writer.WriteBool(true);

        switch (kind)
        {
            case LeafKind.Bool:
                _writer.WriteBool(Unsafe.As<T, bool>(ref value));
                break;
            case LeafKind.Byte:
                _writer.WriteByte(Unsafe.As<T, byte>(ref value));
                break;
            case LeafKind.SByte:
                _writer.WriteSByte(Unsafe.As<T, sbyte>(ref value));
                break;
            case LeafKind.Int16:
                _writer.WriteInt16(Unsafe.As<T, short>(ref value));
                break;
            case LeafKind.UInt16:
                _writer.WriteUInt16(Unsafe.As<T, ushort>(ref value));
                break;
            case LeafKind.Int32:
                _writer.WriteInt32(Unsafe.As<T, int>(ref value));
                break;
            case LeafKind.UInt32:
                _writer.WriteUInt32(Unsafe.As<T, uint>(ref value));
                break;
            case LeafKind.Int64:
                _writer.WriteInt64(Unsafe.As<T, long>(ref value));
                break;
            case LeafKind.UInt64:
                _writer.WriteUInt64(Unsafe.As<T, ulong>(ref value));
                break;
            case LeafKind.Char:
                _writer.WriteChar(Unsafe.As<T, char>(ref value));
                break;
            default:
                // GetLeafKind validates the type; Enum is the only remaining kind.
                WriteEnumValue(ref value);
                break;
        }
    }

    private void WriteEnumValue<T>(ref T value)
    {
        if (Unsafe.SizeOf<T>() == 1)
        {
            _writer.WriteByte(Unsafe.As<T, byte>(ref value));
            return;
        }

        if (Unsafe.SizeOf<T>() == 2)
        {
            _writer.WriteUInt16(Unsafe.As<T, ushort>(ref value));
            return;
        }

        if (Unsafe.SizeOf<T>() == 4)
        {
            _writer.WriteUInt32(Unsafe.As<T, uint>(ref value));
            return;
        }

        _writer.WriteUInt64(Unsafe.As<T, ulong>(ref value));
    }

    private static string GetStableTypeName(Type type)
    {
        lock (StableTypeNameSync)
        {
            if (StableTypeNames.TryGetValue(type, out string? name))
                return name;

            name = BuildStableTypeName(type);
            StableTypeNames[type] = name;
            return name;
        }
    }

    private static string BuildStableTypeName(Type type)
    {
        if (type.IsArray)
            return BuildStableArrayTypeName(type);

        if (!type.IsGenericType)
            // Runtime records and typeof(T) supply closed types, never generic parameters.
            return type.FullName!;

        return BuildStableGenericTypeName(type);
    }

    private static string BuildStableArrayTypeName(Type type)
    {
        Type elementType = type.GetElementType()!;
        int rank = type.GetArrayRank();
        string suffix = rank == 1
            ? "[]"
            : "[" + new string(',', rank - 1) + "]";

        return GetStableTypeName(elementType) + suffix;
    }

    private static string BuildStableGenericTypeName(Type type)
    {
        Type genericTypeDefinition = type.GetGenericTypeDefinition();
        string baseName = genericTypeDefinition.FullName!;
        Type[] arguments = type.GetGenericArguments();

        var builder = new StringBuilder(baseName.Length + (arguments.Length * 16) + 2);
        builder.Append(baseName);
        builder.Append('<');

        for (int i = 0; i < arguments.Length; i++)
        {
            if (i > 0)
                builder.Append(',');

            builder.Append(GetStableTypeName(arguments[i]));
        }

        builder.Append('>');
        return builder.ToString();
    }

    private static string FormatSlot(string? slot)
    {
        return string.IsNullOrEmpty(slot)
            ? string.Empty
            : $" in slot '{slot}'";
    }
}
