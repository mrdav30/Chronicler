using Chronicler.Serialization;
using FluentAssertions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Chronicler.Tests;

public sealed class OrderedStringMapTests
{
    [Fact]
    public void Map_ShouldPreserveOrderAndRepairLookupsAfterReplacementAndRemoval()
    {
        Type mapType = typeof(JsonRecordSerializer).Assembly
            .GetType("Chronicler.Serialization.OrderedStringMap`1", throwOnError: true)!
            .MakeGenericType(typeof(int));
        object map = Activator.CreateInstance(mapType, new object[] { 1, StringComparer.OrdinalIgnoreCase })!;
        PropertyInfo indexer = mapType.GetProperty("Item")!;
        MethodInfo remove = mapType.GetMethod("Remove")!;
        MethodInfo tryGetValue = mapType.GetMethod("TryGetValue")!;

        Action nullKey = () => indexer.SetValue(map, 1, new object?[] { null });
        nullKey.Should().Throw<TargetInvocationException>()
            .Which.InnerException.Should().BeOfType<ArgumentNullException>()
            .Which.ParamName.Should().Be("key");

        indexer.SetValue(map, 1, new object[] { "First" });
        indexer.SetValue(map, 2, new object[] { "Second" });
        indexer.SetValue(map, 3, new object[] { "Third" });
        indexer.SetValue(map, 20, new object[] { "SECOND" });
        ((IEnumerable<KeyValuePair<string, int>>)map).Should().Equal(
            new KeyValuePair<string, int>("First", 1),
            new KeyValuePair<string, int>("Second", 20),
            new KeyValuePair<string, int>("Third", 3));

        remove.Invoke(map, new object[] { "First" }).Should().Be(true);
        remove.Invoke(map, new object[] { "First" }).Should().Be(false);
        object?[] missing = { "First", null };
        tryGetValue.Invoke(map, missing).Should().Be(false);
        missing[1].Should().Be(0);
        object?[] remaining = { "Third", null };
        tryGetValue.Invoke(map, remaining).Should().Be(true);
        remaining[1].Should().Be(3);

        indexer.SetValue(map, 4, new object[] { "Fourth" });
        mapType.GetProperty("Count")!.GetValue(map).Should().Be(3);
        IEnumerator enumerator = ((IEnumerable)map).GetEnumerator();
        foreach (var expected in new[]
        {
            new KeyValuePair<string, int>("Second", 20),
            new KeyValuePair<string, int>("Third", 3),
            new KeyValuePair<string, int>("Fourth", 4)
        })
        {
            enumerator.MoveNext().Should().BeTrue();
            enumerator.Current.Should().Be(expected);
        }
        enumerator.MoveNext().Should().BeFalse();
    }
}
