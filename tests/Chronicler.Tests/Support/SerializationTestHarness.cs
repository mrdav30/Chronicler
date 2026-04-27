using System;
using System.Collections.Generic;

namespace Chronicler.Tests;

public enum SerializationTransport
{
    Json,
    MemoryPack
}

public static class SerializationTransportData
{
    public static IEnumerable<object[]> All
    {
        get
        {
            yield return new object[] { SerializationTransport.Json };
            yield return new object[] { SerializationTransport.MemoryPack };
        }
    }
}

internal static class SerializationTestHarness
{
    public static object Serialize(
        IRecordable target,
        SerializationTransport transport,
        ChronicleContext? context = null)
    {
        return transport switch
        {
            SerializationTransport.Json => JsonRecordSerializer.Serialize(target, context, writeIndented: true),
#if !CHRONICLER_DISABLE_MEMORYPACK
            SerializationTransport.MemoryPack => MemoryPackRecordSerializer.Serialize(target, context),
#endif
            _ => throw new ArgumentOutOfRangeException(nameof(transport))
        };
    }

    public static void Populate(
        IRecordable target,
        object payload,
        SerializationTransport transport,
        ChronicleContext? context = null)
    {
        switch (transport)
        {
            case SerializationTransport.Json:
                JsonRecordSerializer.Populate(target, (string)payload, context);
                return;
#if !CHRONICLER_DISABLE_MEMORYPACK
            case SerializationTransport.MemoryPack:
                MemoryPackRecordSerializer.Populate(target, (byte[])payload, context);
                return;
#endif
            default:
                throw new ArgumentOutOfRangeException(nameof(transport));
        }
    }

    public static object RemoveEntry(object payload, SerializationTransport transport, params string[] path)
    {
        return transport switch
        {
            SerializationTransport.Json => SerializationPayloadEditor.RemoveJsonProperty((string)payload, path),
#if !CHRONICLER_DISABLE_MEMORYPACK
            SerializationTransport.MemoryPack => SerializationPayloadEditor.RemoveMemoryPackEntry((byte[])payload, path),
#endif
            _ => throw new ArgumentOutOfRangeException(nameof(transport))
        };
    }

    public static object SetValue<T>(object payload, SerializationTransport transport, T value, params string[] path)
    {
        return transport switch
        {
            SerializationTransport.Json => SerializationPayloadEditor.SetJsonValue((string)payload, value, path),
#if !CHRONICLER_DISABLE_MEMORYPACK
            SerializationTransport.MemoryPack => SerializationPayloadEditor.SetMemoryPackValue((byte[])payload, value, path),
#endif
            _ => throw new ArgumentOutOfRangeException(nameof(transport))
        };
    }
}
