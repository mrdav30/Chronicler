using System;
using System.Collections.Generic;
using System.Numerics;
using Chronicler.Timing;
using Xunit;

namespace Chronicler.Tests;

public sealed class ChronicleTimestampTests
{
    [Fact]
    public void DefaultIsTheOriginAndNegativeWholeSecondsAreRejected()
    {
        Assert.Equal(new ChronicleTimestamp(0, 0), default);
        Assert.Equal(default, ChronicleTimestamp.Zero);
        Assert.Equal("wholeSeconds", Assert.Throws<ArgumentOutOfRangeException>(
            () => new ChronicleTimestamp(-1, uint.MaxValue)).ParamName);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChronicleTimestamp(long.MinValue, 0));
    }

    [Fact]
    public void FullRangeSubtractionWorksInBothDirections()
    {
        var last = new ChronicleTimestamp(long.MaxValue, uint.MaxValue);
        Assert.Equal(new ChronicleDuration(long.MinValue, 1), ChronicleTimestamp.Zero - last);
        Assert.Equal(new ChronicleDuration(long.MaxValue, uint.MaxValue), last - ChronicleTimestamp.Zero);
        Assert.Equal(ChronicleDuration.Zero, last - last);
        Assert.Equal(ChronicleTimestamp.Zero, last + (ChronicleTimestamp.Zero - last));
        Assert.Equal(last, ChronicleTimestamp.Zero + (last - ChronicleTimestamp.Zero));
        Assert.Throws<OverflowException>(() => last + new ChronicleDuration(0, 1));
        Assert.Throws<OverflowException>(() => ChronicleTimestamp.Zero + new ChronicleDuration(-1, uint.MaxValue));
        Assert.Throws<OverflowException>(() => new ChronicleTimestamp(1, 0) + new ChronicleDuration(long.MinValue, 0));
    }

    [Fact]
    public void ShortIntervalsAreIndependentOfSimulationAge()
    {
        var quarterSecond = new ChronicleDuration(0, 0x40000000);
        foreach (long seconds in new[] { 0L, (long)int.MaxValue + 1, 3155760000L, long.MaxValue })
        {
            var start = new ChronicleTimestamp(seconds, 0x40000000);
            var end = start + quarterSecond;
            Assert.Equal(quarterSecond, end - start);
            Assert.Equal(new ChronicleDuration(-1, 0xc0000000), start - end);
        }
    }

    [Fact]
    public void GuideExampleMeasuresAQuarterSecondAfterOneHundredJulianYears()
    {
        var start = new ChronicleTimestamp(3_155_760_000, 0);
        var end = start + new ChronicleDuration(0, 0x40000000);
        ChronicleDuration elapsed = end - start;
        Assert.Equal(0, elapsed.WholeSeconds);
        Assert.Equal(0x40000000u, elapsed.FractionalSecond);
    }

    [Fact]
    public void OrderingAndArithmeticMatchIntegerUnitsAtEveryBoundary()
    {
        var values = new List<ChronicleTimestamp>();
        foreach (long seconds in new[] { 0L, 1L, (long)int.MaxValue + 1, long.MaxValue - 1, long.MaxValue })
            foreach (uint fraction in new[] { 0u, 1u, 0x80000000u, uint.MaxValue })
                values.Add(new ChronicleTimestamp(seconds, fraction));
        foreach (var left in values)
            foreach (var right in values)
            {
                BigInteger l = Units(left), r = Units(right);
                ChronicleDuration difference = left - right;
                Assert.Equal(l - r, ((BigInteger)difference.WholeSeconds << 32) + difference.FractionalSecond);
                Assert.Equal(left, right + difference);
                Assert.Equal(Math.Sign(l.CompareTo(r)), Math.Sign(left.CompareTo(right)));
                Assert.Equal(l == r, left.Equals(right));
                Assert.Equal(l == r, left == right);
                Assert.Equal(l != r, left != right);
                Assert.Equal(l < r, left < right);
                Assert.Equal(l <= r, left <= right);
                Assert.Equal(l > r, left > right);
                Assert.Equal(l >= r, left >= right);
            }

        var deltas = new[] { new ChronicleDuration(long.MinValue, 0), new ChronicleDuration(-1, 0),
            new ChronicleDuration(-1, uint.MaxValue), ChronicleDuration.Zero,
            new ChronicleDuration(0, 1), new ChronicleDuration(1, 0), new ChronicleDuration(long.MaxValue, uint.MaxValue) };
        BigInteger max = ((BigInteger)long.MaxValue << 32) + uint.MaxValue;
        foreach (var time in values)
            foreach (var delta in deltas)
            {
                BigInteger expected = Units(time) + ((BigInteger)delta.WholeSeconds << 32) + delta.FractionalSecond;
                if (expected < 0 || expected > max)
                    Assert.Throws<OverflowException>(() => time + delta);
                else
                    Assert.Equal(expected, Units(time + delta));
            }
    }

    [Fact]
    public void EqualityAndHashingUseValuesNotRuntimeIdentity()
    {
        var value = new ChronicleTimestamp(1, 7);
        Assert.True(value.Equals((object)new ChronicleTimestamp(1, 7)));
        Assert.False(value.Equals((object)new ChronicleTimestamp(1, 8)));
        Assert.False(value.Equals(null));
        Assert.False(value.Equals((object)new ChronicleDuration(1, 7)));
        Assert.True(((IEquatable<ChronicleTimestamp>)value).Equals(new ChronicleTimestamp(1, 7)));
        Assert.True(((IComparable<ChronicleTimestamp>)value).CompareTo(ChronicleTimestamp.Zero) > 0);
        Assert.Equal(394, value.GetHashCode());
        Assert.Equal(0, ChronicleTimestamp.Zero.GetHashCode());
        Assert.Equal(int.MaxValue, new ChronicleTimestamp(long.MaxValue, uint.MaxValue).GetHashCode());
    }

    [Fact]
    public void WarmedSuccessfulTimestampOperationsAllocateNothing()
    {
        var origin = new ChronicleTimestamp(3155760000, 0);
        var step = new ChronicleDuration(0, 0x40000000);
        var result = origin;
        int comparisons = 0;
        Action exercise = () =>
        {
            result = origin;
            comparisons = 0;
            for (int i = 0; i < 1024; i++)
            {
                var next = result + step;
                comparisons += next > result && result < next && next >= result && result <= next
                    && next != result && next - result == step && next.Equals(next) ? 1 : 0;
                _ = next.GetHashCode();
                result = next;
            }
        };
        Assert.Equal(0, AllocationTestHelper.MeasureAfterWarmup(exercise, exercise));
        Assert.Equal(new ChronicleTimestamp(3155760256, 0), result);
        Assert.Equal(1024, comparisons);
    }

    private static BigInteger Units(ChronicleTimestamp value) =>
        ((BigInteger)value.WholeSeconds << 32) + value.FractionalSecond;
}

