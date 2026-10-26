using Chronicler.Serialization;
#if !CHRONICLER_DISABLE_MEMORYPACK
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Chronicler.Tests;

public sealed class MemoryPackEnvelopeReflectionTests
{
    [Fact]
    public void EnvelopeMethods_ShouldHandleMissingEntryTable()
    {
        Type envelopeType = GetChroniclerType("Chronicler.Serialization.MemoryPackRecordEnvelope");
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

        object?[] createdArgs = { "created", null };
        tryGetEntry.Invoke(envelope, createdArgs).Should().Be(true);
        ((byte[])createdArgs[1]!).Should().Equal(payload);
    }

    [Fact]
    public void EntryTableState_ShouldReplaceAndClearEntriesThroughSetter()
    {
        Type tableType = GetChroniclerType("Chronicler.Serialization.MemoryPackRecordEntryTable");
        Type stateType = GetChroniclerType("Chronicler.Serialization.MemoryPackRecordEntryTableState");

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

        FieldInfo itemsField = stateType.GetField("Items")!;
        var items = new[]
        {
            new KeyValuePair<string, byte[]?>("first", new byte[] { 1, 2 }),
            new KeyValuePair<string, byte[]?>("second", null)
        };
        object populatedState = Activator.CreateInstance(stateType, new object[] { items })!;
        stateProperty.SetValue(table, populatedState);
        var stored = (KeyValuePair<string, byte[]?>[])itemsField.GetValue(stateProperty.GetValue(table))!;
        stored.Should().Equal(items);

        var replacement = new[] { new KeyValuePair<string, byte[]?>("replacement", new byte[] { 3 }) };
        stateProperty.SetValue(table, Activator.CreateInstance(stateType, new object[] { replacement }));
        ((KeyValuePair<string, byte[]?>[])itemsField.GetValue(stateProperty.GetValue(table))!)
            .Should().Equal(replacement);

        stateProperty.SetValue(table, nullState);
        ((KeyValuePair<string, byte[]?>[])itemsField.GetValue(stateProperty.GetValue(table))!)
            .Should().BeEmpty();
    }

    private static Type GetChroniclerType(string typeName)
    {
        return typeof(MemoryPackRecordSerializer).Assembly.GetType(typeName, throwOnError: true)!;
    }
}
#endif
