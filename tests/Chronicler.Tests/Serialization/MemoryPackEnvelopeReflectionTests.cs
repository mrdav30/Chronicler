#if !CHRONICLER_DISABLE_MEMORYPACK
using FluentAssertions;
using System;
using System.Collections;
using System.Reflection;
using Xunit;

namespace Chronicler.Tests;

public sealed class MemoryPackEnvelopeReflectionTests
{
    [Fact]
    public void EnvelopeMethods_ShouldHandleMissingEntryTable()
    {
        Type envelopeType = GetChroniclerType("Chronicler.MemoryPackRecordEnvelope");
        object envelope = Activator.CreateInstance(
            envelopeType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: null,
            culture: null)!;

        envelopeType.GetProperty("Entries")!.SetValue(envelope, null);

        object entryMap = envelopeType
            .GetMethod("ToEntryMap", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(envelope, null)!;
        entryMap.GetType().GetProperty("Count")!.GetValue(entryMap).Should().Be(0);

        envelopeType
            .GetMethod("RemoveEntry", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(envelope, new object[] { "missing" })
            .Should().Be(false);

        MethodInfo tryGetEntry = envelopeType.GetMethod(
            "TryGetEntry",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        object?[] tryGetArgs = { "missing", null };

        tryGetEntry.Invoke(envelope, tryGetArgs).Should().Be(false);
        tryGetArgs[1].Should().BeNull();

        byte[] payload = { 1, 2, 3 };
        envelopeType
            .GetMethod("SetEntry", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(envelope, new object[] { "created", payload });

        envelopeType.GetProperty("Entries")!.GetValue(envelope).Should().NotBeNull();
    }

    [Fact]
    public void EntryTableState_ShouldRoundTripEmptyStateThroughSetter()
    {
        Type tableType = GetChroniclerType("Chronicler.MemoryPackRecordEntryTable");
        Type stateType = GetChroniclerType("Chronicler.MemoryPackRecordEntryTableState");

        object nullState = Activator.CreateInstance(
            stateType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object?[] { null },
            culture: null)!;

        object table = Activator.CreateInstance(
            tableType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new[] { nullState },
            culture: null)!;

        PropertyInfo stateProperty = tableType.GetProperty("State")!;

        stateProperty.GetValue(table).Should().NotBeNull();
        stateProperty.SetValue(table, nullState);
    }

    [Fact]
    public void OrderedStringMap_ShouldRejectNullKeysAndSupportNonGenericEnumeration()
    {
        Type mapType = GetChroniclerType("Chronicler.OrderedStringMap`1")
            .MakeGenericType(typeof(byte[]));

        object map = Activator.CreateInstance(
            mapType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object[] { 1, StringComparer.Ordinal },
            culture: null)!;

        PropertyInfo indexer = mapType.GetProperty("Item")!;
        Action nullKey = () => indexer.SetValue(map, new byte[] { 1 }, new object?[] { null });

        nullKey.Should().Throw<TargetInvocationException>()
            .Which.InnerException.Should().BeOfType<ArgumentNullException>()
            .Which.ParamName.Should().Be("key");

        indexer.SetValue(map, new byte[] { 1 }, new object[] { "value" });

        IEnumerator enumerator = ((IEnumerable)map).GetEnumerator();
        enumerator.MoveNext().Should().BeTrue();
    }

    private static Type GetChroniclerType(string typeName)
    {
        return typeof(MemoryPackRecordSerializer).Assembly.GetType(typeName, throwOnError: true)!;
    }
}
#endif
