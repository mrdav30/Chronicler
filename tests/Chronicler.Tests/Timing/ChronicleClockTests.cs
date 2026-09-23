using System;
using Chronicler.Timing;
using Xunit;

namespace Chronicler.Tests;

public sealed class ChronicleClockTests
{
    [Fact]
    public void StepChangeAffectsOnlySubsequentAdvances()
    {
        var clock = new ChronicleClock(new ChronicleDuration(0, 0x80000000));
        clock.Advance();
        clock.SetStepDuration(new ChronicleDuration(0, 0x40000000));
        Assert.Equal(1L, clock.FrameCount);
        Assert.Equal(new ChronicleTimestamp(0, 0x80000000), clock.ElapsedTime);
        clock.Advance();
        Assert.Equal(2L, clock.FrameCount);
        Assert.Equal(new ChronicleTimestamp(0, 0xC0000000), clock.ElapsedTime);
    }

    [Theory]
    [InlineData(0L, 0u)]
    [InlineData(-1L, uint.MaxValue)]
    [InlineData(long.MinValue, 0u)]
    public void NonpositiveStepsRejectBeforeChangingRunningClock(long seconds, uint fraction)
    {
        var invalid = new ChronicleDuration(seconds, fraction);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChronicleClock(invalid));
        var step = new ChronicleDuration(0, 1);
        var clock = new ChronicleClock(step);
        clock.Advance();
        Assert.Throws<ArgumentOutOfRangeException>(() => clock.SetStepDuration(invalid));
        Assert.Equal(step, clock.StepDuration);
        clock.Advance();
        Assert.Equal(2L, clock.FrameCount);
        Assert.Equal(new ChronicleTimestamp(0, 2), clock.ElapsedTime);
    }

    [Fact]
    public void ResetStartsANewTimelineButPreservesCurrentStep()
    {
        var clock = new ChronicleClock(new ChronicleDuration(3_155_760_000, 0));
        clock.Advance();
        var step = new ChronicleDuration(0, uint.MaxValue);
        clock.SetStepDuration(step);
        clock.Reset();
        Assert.Equal(0L, clock.FrameCount);
        Assert.Equal(ChronicleTimestamp.Zero, clock.ElapsedTime);
        Assert.Equal(step, clock.StepDuration);
        clock.Advance();
        clock.Advance();
        Assert.Equal(2L, clock.FrameCount);
        Assert.Equal(new ChronicleTimestamp(1, uint.MaxValue - 1), clock.ElapsedTime);
    }

    [Fact]
    public void DeadlineUsesWideCheckedFrameOffsetsWithoutAdvancing()
    {
        var clock = new ChronicleClock(new ChronicleDuration(1, 0));
        Assert.Equal(0L, clock.GetDeadlineFrame(0));
        Assert.Equal(long.MaxValue, clock.GetDeadlineFrame(long.MaxValue));
        clock.Advance();
        Assert.Equal(1L, clock.GetDeadlineFrame(0));
        Assert.Equal(long.MaxValue, clock.GetDeadlineFrame(long.MaxValue - 1));
        Assert.Throws<OverflowException>(() => clock.GetDeadlineFrame(long.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => clock.GetDeadlineFrame(-1));
        Assert.Equal(1L, clock.FrameCount);
        Assert.Equal(new ChronicleTimestamp(1, 0), clock.ElapsedTime);
    }

    [Fact]
    public void TimestampExhaustionLeavesAllClockStateUnchanged()
    {
        var largestStep = new ChronicleDuration(long.MaxValue, uint.MaxValue);
        var clock = new ChronicleClock(largestStep);
        clock.Advance();
        Assert.Throws<OverflowException>(() => clock.Advance());
        Assert.Equal(1L, clock.FrameCount);
        Assert.Equal(new ChronicleTimestamp(long.MaxValue, uint.MaxValue), clock.ElapsedTime);
        Assert.Equal(largestStep, clock.StepDuration);
    }

    [Fact]
    public void WarmedClockOperationsAllocateNothing()
    {
        var step = new ChronicleDuration(0, 0x40000000);
        var clock = new ChronicleClock(step);
        long deadlineSum = 0;
        int comparisons = 0;
        Action exercise = () =>
        {
            clock.Reset();
            deadlineSum = 0;
            comparisons = 0;
            for (int i = 0; i < 1024; i++)
            {
                clock.SetStepDuration(step);
                var before = clock.ElapsedTime;
                clock.Advance();
                deadlineSum += clock.GetDeadlineFrame(2);
                if (clock.ElapsedTime > before)
                    comparisons++;
            }
        };
        Assert.Equal(0, AllocationTestHelper.MeasureAfterWarmup(exercise, exercise));
        Assert.Equal(1024L, clock.FrameCount);
        Assert.Equal(new ChronicleTimestamp(256, 0), clock.ElapsedTime);
        Assert.Equal(526848L, deadlineSum);
        Assert.Equal(1024, comparisons);
    }
}
