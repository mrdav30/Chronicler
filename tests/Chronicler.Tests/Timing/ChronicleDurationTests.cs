using System;
using System.Collections.Generic;
using System.Numerics;
using Chronicler.Timing;
using Xunit;

namespace Chronicler.Tests;

public sealed class ChronicleDurationTests
{
    [Fact]
    public void DefaultsAndNegativeFractionsAreCanonical()
    {
        Assert.Equal(new ChronicleDuration(0, 0), default);
        Assert.Equal(default, ChronicleDuration.Zero);
        var tinyNegative = new ChronicleDuration(-1, uint.MaxValue);
        Assert.Equal(-1, tinyNegative.WholeSeconds);
        Assert.Equal(uint.MaxValue, tinyNegative.FractionalSecond);
        Assert.Equal(ChronicleDuration.Zero, tinyNegative + new ChronicleDuration(0, 1));
        Assert.Equal(new ChronicleDuration(-1, 0), tinyNegative - new ChronicleDuration(0, uint.MaxValue));
    }

    [Fact]
    public void CarryAndBorrowUseTheFinalResultRange()
    {
        Assert.Equal(new ChronicleDuration(long.MinValue, 0),
            new ChronicleDuration(-1, uint.MaxValue) + new ChronicleDuration(long.MinValue, 1));
        Assert.Equal(new ChronicleDuration(long.MaxValue, uint.MaxValue),
            new ChronicleDuration(long.MaxValue, 0) - new ChronicleDuration(-1, 1));
        Assert.Equal(new ChronicleDuration(3, 0),
            new ChronicleDuration(1, uint.MaxValue) + new ChronicleDuration(1, 1));
        Assert.Equal(new ChronicleDuration(-2, uint.MaxValue),
            new ChronicleDuration(-1, 0) - new ChronicleDuration(0, 1));
        Assert.Throws<OverflowException>(() =>
            new ChronicleDuration(long.MaxValue, uint.MaxValue) + new ChronicleDuration(0, 1));
        Assert.Throws<OverflowException>(() =>
            new ChronicleDuration(long.MinValue, 0) - new ChronicleDuration(0, 1));
    }

    [Fact]
    public void ArithmeticAndOrderingMatchAnIndependentIntegerOracle()
    {
        var values = new List<ChronicleDuration>();
        foreach (long whole in new[] { long.MinValue, long.MinValue + 1, -2L, -1L, 0L, 1L, 2L, long.MaxValue - 1, long.MaxValue })
            foreach (uint fraction in new[] { 0u, 1u, 0x7fffffffu, 0x80000000u, uint.MaxValue })
                values.Add(new ChronicleDuration(whole, fraction));
        foreach (var left in values)
            foreach (var right in values)
                VerifyPair(left, right);

        var random = new Random(73421);
        var bytes = new byte[24];
        for (int i = 0; i < 2048; i++)
        {
            random.NextBytes(bytes);
            VerifyPair(new ChronicleDuration(BitConverter.ToInt64(bytes, 0), BitConverter.ToUInt32(bytes, 8)),
                new ChronicleDuration(BitConverter.ToInt64(bytes, 12), BitConverter.ToUInt32(bytes, 20)));
        }
    }

    [Fact]
    public void EqualitySupportsTypedAndBoxedValuesWithoutConflatingOtherTypes()
    {
        var value = new ChronicleDuration(-2, 17);
        Assert.True(value.Equals((object)new ChronicleDuration(-2, 17)));
        Assert.False(value.Equals((object)new ChronicleDuration(-2, 18)));
        Assert.False(value.Equals(null));
        Assert.False(value.Equals("not a duration"));
        Assert.False(value.Equals((object)new ChronicleTimestamp(2, 17)));
        Assert.True(((IEquatable<ChronicleDuration>)value).Equals(new ChronicleDuration(-2, 17)));
        Assert.True(((IComparable<ChronicleDuration>)value).CompareTo(ChronicleDuration.Zero) < 0);
    }

    [Theory]
    [InlineData(0L, 0u, 0)]
    [InlineData(1L, 0u, 397)]
    [InlineData(0L, 1u, 1)]
    [InlineData(-1L, uint.MaxValue, -1)]
    [InlineData(long.MinValue, 0u, int.MinValue)]
    [InlineData(long.MaxValue, uint.MaxValue, int.MaxValue)]
    public void HashIsAStableComponentMix(long whole, uint fraction, int expected)
    {
        Assert.Equal(expected, new ChronicleDuration(whole, fraction).GetHashCode());
    }

    [Fact]
    public void WarmedSuccessfulValueOperationsAllocateNothing()
    {
        var step = new ChronicleDuration(0, 0x40000000);
        var result = ChronicleDuration.Zero;
        int comparisons = 0;
        Action exercise = () =>
        {
            result = ChronicleDuration.Zero;
            comparisons = 0;
            for (int i = 0; i < 1024; i++)
            {
                var next = result + step;
                comparisons += next > result && result < next && next >= result && result <= next
                    && next != result && next - step == result && next.Equals(next) ? 1 : 0;
                _ = next.GetHashCode();
                result = next;
            }
        };
        Assert.Equal(0, AllocationTestHelper.MeasureAfterWarmup(exercise, exercise));
        Assert.Equal(new ChronicleDuration(256, 0), result);
        Assert.Equal(1024, comparisons);
    }

    private static void VerifyPair(ChronicleDuration left, ChronicleDuration right)
    {
        BigInteger l = Units(left), r = Units(right);
        VerifyArithmetic(l + r, () => left + right);
        VerifyArithmetic(l - r, () => left - right);
        int ordering = l.CompareTo(r);
        Assert.Equal(ordering, Math.Sign(left.CompareTo(right)));
        Assert.Equal(l == r, left.Equals(right));
        Assert.Equal(l == r, left == right);
        Assert.Equal(l != r, left != right);
        Assert.Equal(l < r, left < right);
        Assert.Equal(l <= r, left <= right);
        Assert.Equal(l > r, left > right);
        Assert.Equal(l >= r, left >= right);
        if (l == r) Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    private static BigInteger Units(ChronicleDuration value) =>
        ((BigInteger)value.WholeSeconds << 32) + value.FractionalSecond;

    private static void VerifyArithmetic(BigInteger expected, Func<ChronicleDuration> operation)
    {
        BigInteger minimum = (BigInteger)long.MinValue << 32;
        BigInteger maximum = ((BigInteger)long.MaxValue << 32) + uint.MaxValue;
        if (expected < minimum || expected > maximum)
            Assert.Throws<OverflowException>(() => operation());
        else
            Assert.Equal(expected, Units(operation()));
    }
}
