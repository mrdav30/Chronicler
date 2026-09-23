using System;
using System.Collections.Generic;
using Chronicler.Timing;
using Xunit;

namespace Chronicler.Tests;

public sealed class ChronicleTimeRecordingTests
{
    [Theory]
    [MemberData(nameof(SerializationTransportData.All), MemberType = typeof(SerializationTransportData))]
    public void ValuesRoundTripExactlyAcrossTheirRanges(SerializationTransport transport)
    {
        foreach (var duration in new[] { ChronicleDuration.Zero, new ChronicleDuration(-1, uint.MaxValue),
            new ChronicleDuration(long.MinValue, 0), new ChronicleDuration(long.MaxValue, uint.MaxValue) })
        foreach (var timestamp in new[] { ChronicleTimestamp.Zero, new ChronicleTimestamp(0, 1),
            new ChronicleTimestamp(long.MaxValue, uint.MaxValue) })
        {
            var source = new TimeValues { Duration = duration, Timestamp = timestamp };
            var hash = ChronicleHashSerializer.Compute(source);
            object payload = SerializationTestHarness.Serialize(source, transport);
            var target = new TimeValues { Duration = new(7, 8), Timestamp = new(9, 10) };
            SerializationTestHarness.Populate(target, payload, transport);
            Assert.Equal(duration, target.Duration);
            Assert.Equal(timestamp, target.Timestamp);
            Assert.Equal(hash, ChronicleHashSerializer.Compute(target));
            Assert.Equal(hash, ChronicleHashSerializer.Compute(source));
        }
    }

    [Theory]
    [MemberData(nameof(SerializationTransportData.All), MemberType = typeof(SerializationTransportData))]
    public void MissingComponentsUseZeroButRetainRequiredSchema(SerializationTransport transport)
    {
        var source = new TimeValues { Duration = new(-1, 17), Timestamp = new(23, 29) };
        object payload = SerializationTestHarness.Serialize(source, transport);
        foreach (string name in new[] { "Duration", "Timestamp" })
        {
            payload = SerializationTestHarness.RemoveEntry(payload, transport, name, "WholeSeconds");
            payload = SerializationTestHarness.RemoveEntry(payload, transport, name, "FractionalSecond");
        }
        SerializationTestHarness.Populate(source, payload, transport);
        Assert.Equal(ChronicleDuration.Zero, source.Duration);
        Assert.Equal(ChronicleTimestamp.Zero, source.Timestamp);
    }

    public static IEnumerable<object[]> InvalidValueRecords
    {
        get
        {
            foreach (object[] transport in SerializationTransportData.All)
            foreach (string field in new[] { "Duration", "Timestamp" })
            foreach (string defect in new[] { "missing record", "missing schema", "unsupported schema" })
                yield return new object[] { transport[0], field, defect };
        }
    }

    [Theory]
    [MemberData(nameof(InvalidValueRecords))]
    public void InvalidValueSchemaLeavesThatValueUnchanged(SerializationTransport transport, string field, string defect)
    {
        var source = new TimeValues();
        object payload = SerializationTestHarness.Serialize(source, transport);
        payload = defect switch
        {
            "missing record" => SerializationTestHarness.RemoveEntry(payload, transport, field),
            "missing schema" => SerializationTestHarness.RemoveEntry(payload, transport, field, "SchemaVersion"),
            _ => SerializationTestHarness.SetValue(payload, transport, 2, field, "SchemaVersion")
        };
        var target = new TimeValues { Duration = new(-1, 13), Timestamp = new(19, 23) };
        Assert.Throws<InvalidOperationException>(() => SerializationTestHarness.Populate(target, payload, transport));
        if (field == "Duration")
            Assert.Equal(new ChronicleDuration(-1, 13), target.Duration);
        else
            Assert.Equal(new ChronicleTimestamp(19, 23), target.Timestamp);
    }

    [Theory]
    [MemberData(nameof(SerializationTransportData.All), MemberType = typeof(SerializationTransportData))]
    public void NegativeTimestampLeavesTargetValueUnchanged(SerializationTransport transport)
    {
        object payload = SerializationTestHarness.Serialize(new TimeValues(), transport);
        payload = SerializationTestHarness.SetValue(payload, transport, -1L, "Timestamp", "WholeSeconds");
        var target = new TimeValues { Timestamp = new(7, 9) };
        Assert.Throws<ArgumentOutOfRangeException>(() => SerializationTestHarness.Populate(target, payload, transport));
        Assert.Equal(new ChronicleTimestamp(7, 9), target.Timestamp);
    }

    [Theory]
    [MemberData(nameof(SerializationTransportData.All), MemberType = typeof(SerializationTransportData))]
    public void WideClockRestoresAndContinuesWithRecordedStep(SerializationTransport transport)
    {
        var source = LoadClock(transport, (long)int.MaxValue + 9, 3_155_760_000, uint.MaxValue,
            new ChronicleDuration(0, 1));
        object payload = SerializationTestHarness.Serialize(source, transport);
        var restored = RunningClock();
        SerializationTestHarness.Populate(restored, payload, transport);
        Assert.Equal((long)int.MaxValue + 9, restored.FrameCount);
        Assert.Equal(new ChronicleTimestamp(3_155_760_000, uint.MaxValue), restored.ElapsedTime);
        Assert.Equal(new ChronicleDuration(0, 1), restored.StepDuration);
        Assert.Equal(ChronicleHashSerializer.Compute(source), ChronicleHashSerializer.Compute(restored));
        restored.Advance();
        Assert.Equal((long)int.MaxValue + 10, restored.FrameCount);
        Assert.Equal(new ChronicleTimestamp(3_155_760_001, 0), restored.ElapsedTime);
    }

    [Theory]
    [MemberData(nameof(SerializationTransportData.All), MemberType = typeof(SerializationTransportData))]
    public void RestoredFrameExhaustionRejectsAdvanceWithoutMutation(SerializationTransport transport)
    {
        var clock = LoadClock(transport, long.MaxValue, 7, 3, new ChronicleDuration(0, 1));
        var before = ChronicleHashSerializer.Compute(clock);
        Assert.Equal(long.MaxValue, clock.GetDeadlineFrame(0));
        Assert.Throws<OverflowException>(() => clock.GetDeadlineFrame(1));
        Assert.Throws<InvalidOperationException>(() => clock.Advance());
        Assert.Equal(before, ChronicleHashSerializer.Compute(clock));
        clock.Reset();
        clock.Advance();
        Assert.Equal(1L, clock.FrameCount);
        Assert.Equal(new ChronicleTimestamp(0, 1), clock.ElapsedTime);
    }

    [Theory]
    [MemberData(nameof(SerializationTransportData.All), MemberType = typeof(SerializationTransportData))]
    public void RestoredTimestampExhaustionRejectsAdvanceWithoutMutation(SerializationTransport transport)
    {
        var clock = LoadClock(transport, 19, long.MaxValue, uint.MaxValue, new ChronicleDuration(0, 1));
        var before = ChronicleHashSerializer.Compute(clock);
        Assert.Throws<OverflowException>(() => clock.Advance());
        Assert.Equal(before, ChronicleHashSerializer.Compute(clock));
        Assert.Equal(19L, clock.FrameCount);
    }

    public static IEnumerable<object[]> InvalidClockRecords
    {
        get
        {
            foreach (object[] transport in SerializationTransportData.All)
            foreach (string defect in new[] { "root schema missing", "root schema unsupported", "negative frame",
                "elapsed missing", "elapsed schema missing", "elapsed schema unsupported", "negative elapsed",
                "step missing", "step schema missing", "step schema unsupported", "zero step", "negative step",
                "zero frame with elapsed", "nonzero frame without elapsed" })
                yield return new object[] { transport[0], defect };
        }
    }

    [Theory]
    [MemberData(nameof(InvalidClockRecords))]
    public void InvalidClockRecordNeverPartiallyPopulatesRunningTarget(SerializationTransport transport, string defect)
    {
        object payload = SerializationTestHarness.Serialize(new RawClockRecord(), transport);
        payload = defect switch
        {
            "root schema missing" => SerializationTestHarness.RemoveEntry(payload, transport, "SchemaVersion"),
            "root schema unsupported" => SerializationTestHarness.SetValue(payload, transport, 2, "SchemaVersion"),
            "negative frame" => SerializationTestHarness.SetValue(payload, transport, -1L, "FrameCount"),
            "elapsed missing" => SerializationTestHarness.RemoveEntry(payload, transport, "ElapsedTime"),
            "elapsed schema missing" => SerializationTestHarness.RemoveEntry(payload, transport, "ElapsedTime", "SchemaVersion"),
            "elapsed schema unsupported" => SerializationTestHarness.SetValue(payload, transport, 2, "ElapsedTime", "SchemaVersion"),
            "negative elapsed" => SerializationTestHarness.SetValue(payload, transport, -1L, "ElapsedTime", "WholeSeconds"),
            "step missing" => SerializationTestHarness.RemoveEntry(payload, transport, "StepDuration"),
            "step schema missing" => SerializationTestHarness.RemoveEntry(payload, transport, "StepDuration", "SchemaVersion"),
            "step schema unsupported" => SerializationTestHarness.SetValue(payload, transport, 2, "StepDuration", "SchemaVersion"),
            "zero step" => SerializationTestHarness.SetValue(payload, transport, 0u, "StepDuration", "FractionalSecond"),
            "negative step" => SerializationTestHarness.SetValue(payload, transport, -1L, "StepDuration", "WholeSeconds"),
            "zero frame with elapsed" => SerializationTestHarness.SetValue(payload, transport, 0L, "FrameCount"),
            _ => SerializationTestHarness.SetValue(payload, transport, 0L, "ElapsedTime", "WholeSeconds")
        };
        var target = RunningClock();
        var before = ChronicleHashSerializer.Compute(target);
        Type exception = defect is "negative elapsed" or "zero step" or "negative step"
            ? typeof(ArgumentOutOfRangeException) : typeof(InvalidOperationException);
        Assert.Throws(exception, () => SerializationTestHarness.Populate(target, payload, transport));
        Assert.Equal(2L, target.FrameCount);
        Assert.Equal(new ChronicleTimestamp(0, 0xC0000000), target.ElapsedTime);
        Assert.Equal(new ChronicleDuration(0, 0x40000000), target.StepDuration);
        Assert.Equal(before, ChronicleHashSerializer.Compute(target));
        target.Advance();
        Assert.Equal(new ChronicleTimestamp(1, 0), target.ElapsedTime);
    }

    [Theory]
    [MemberData(nameof(SerializationTransportData.All), MemberType = typeof(SerializationTransportData))]
    public void OriginRecordDefaultsDoNotDependOnExistingTargetState(SerializationTransport transport)
    {
        var source = new ChronicleClock(new ChronicleDuration(0, 1));
        object payload = SerializationTestHarness.Serialize(source, transport);
        payload = SerializationTestHarness.RemoveEntry(payload, transport, "FrameCount");
        payload = SerializationTestHarness.RemoveEntry(payload, transport, "ElapsedTime", "WholeSeconds");
        payload = SerializationTestHarness.RemoveEntry(payload, transport, "ElapsedTime", "FractionalSecond");
        payload = SerializationTestHarness.RemoveEntry(payload, transport, "StepDuration", "WholeSeconds");
        var target = RunningClock();
        SerializationTestHarness.Populate(target, payload, transport);
        Assert.Equal(0L, target.FrameCount);
        Assert.Equal(ChronicleTimestamp.Zero, target.ElapsedTime);
        Assert.Equal(ChronicleHashSerializer.Compute(source), ChronicleHashSerializer.Compute(target));
        target.Advance();
        Assert.Equal(new ChronicleTimestamp(0, 1), target.ElapsedTime);
    }

    [Theory]
    [MemberData(nameof(SerializationTransportData.All), MemberType = typeof(SerializationTransportData))]
    public void RestoredRateHistoryReplaysWithIdenticalHashes(SerializationTransport transport)
    {
        var source = RunningClock();
        var replay = new ChronicleClock(new ChronicleDuration(42, 0));
        SerializationTestHarness.Populate(replay, SerializationTestHarness.Serialize(source, transport), transport);
        foreach (var step in new[] { new ChronicleDuration(0, 1), new ChronicleDuration(3_155_760_000, 0),
            new ChronicleDuration(0, uint.MaxValue) })
        {
            source.SetStepDuration(step);
            replay.SetStepDuration(step);
            for (int i = 0; i < 4; i++)
            {
                source.Advance();
                replay.Advance();
                Assert.Equal(source.FrameCount, replay.FrameCount);
                Assert.Equal(source.ElapsedTime, replay.ElapsedTime);
                Assert.Equal(ChronicleHashSerializer.Compute(source), ChronicleHashSerializer.Compute(replay));
            }
        }
        Assert.Equal(14L, replay.FrameCount);
        Assert.Equal(new ChronicleTimestamp(12_623_040_004, 0xC0000000), replay.ElapsedTime);
    }

    [Fact]
    public void ClockHashMatchesVersionOneGoldenVector()
    {
        // Independently encode the documented field order/types/defaults through
        // the existing hash byte contract; do not regenerate from RecordData.
        Assert.Equal("3afb9a9549ef8913ef16fb64eb7dec31", ChronicleHashSerializer.Compute(RunningClock()).ToString());
    }

    [Fact]
    public void GuideClockExampleRestoresRateHistoryThenAdvances()
    {
        var restored = RestoreAndAdvance();
        Assert.Equal(3L, restored.FrameCount);
        Assert.Equal(new ChronicleTimestamp(1, 0), restored.ElapsedTime);
        Assert.Equal(new ChronicleDuration(0, 0x40000000), restored.StepDuration);
    }

    // Complete method shown in the public timing guide.
    private static ChronicleClock RestoreAndAdvance()
    {
        var clock = new ChronicleClock(new ChronicleDuration(0, 0x80000000));
        clock.Advance();
        clock.SetStepDuration(new ChronicleDuration(0, 0x40000000));
        clock.Advance();

        string snapshot = JsonRecordSerializer.Serialize(clock);
        var restored = new ChronicleClock(new ChronicleDuration(1, 0));
        JsonRecordSerializer.Populate(restored, snapshot);
        restored.Advance();
        return restored;
    }

    [Fact]
    public void ClockHashIncludesHighFrameBitsElapsedFractionAndStep()
    {
        var baseline = LoadClock(SerializationTransport.Json, 1, 7, 3, new ChronicleDuration(0, 5));
        var hash = ChronicleHashSerializer.Compute(baseline);
        foreach (var altered in new[] {
            LoadClock(SerializationTransport.Json, 1L + (1L << 32), 7, 3, new(0, 5)),
            LoadClock(SerializationTransport.Json, 1, 7, 4, new(0, 5)),
            LoadClock(SerializationTransport.Json, 1, 7 + (1L << 32), 3, new(0, 5)),
            LoadClock(SerializationTransport.Json, 1, 7, 3, new(0, 6)) })
            Assert.NotEqual(hash, ChronicleHashSerializer.Compute(altered));
    }

    private static ChronicleClock RunningClock()
    {
        var clock = new ChronicleClock(new ChronicleDuration(0, 0x80000000));
        clock.Advance();
        clock.SetStepDuration(new ChronicleDuration(0, 0x40000000));
        clock.Advance();
        return clock;
    }

    private static ChronicleClock LoadClock(SerializationTransport transport, long frame,
        long seconds, uint fraction, ChronicleDuration step)
    {
        var record = new RawClockRecord { FrameCount = frame,
            ElapsedTime = new RawTimeRecord(seconds, fraction),
            StepDuration = new RawTimeRecord(step.WholeSeconds, step.FractionalSecond) };
        var clock = RunningClock();
        SerializationTestHarness.Populate(clock, SerializationTestHarness.Serialize(record, transport), transport);
        return clock;
    }

    private sealed class TimeValues : IRecordable
    {
        public ChronicleDuration Duration;
        public ChronicleTimestamp Timestamp;
        public void RecordData(IChronicler chronicler)
        {
            RecordChronicleTime.Look(chronicler, ref Duration, nameof(Duration));
            RecordChronicleTime.Look(chronicler, ref Timestamp, nameof(Timestamp));
        }
    }

    // Independent wire authoring permits invalid payloads without weakening runtime setters.
    private sealed class RawClockRecord : IRecordable
    {
        public int SchemaVersion = 1;
        public long FrameCount = 17;
        public RawTimeRecord ElapsedTime = new(23, 0);
        public RawTimeRecord StepDuration = new(0, 1);
        public void RecordData(IChronicler chronicler)
        {
            RecordValues.Look(chronicler, ref SchemaVersion, nameof(SchemaVersion), 0);
            RecordValues.Look(chronicler, ref FrameCount, nameof(FrameCount), 0L);
            RecordDeepStruct.Look(chronicler, ref ElapsedTime, nameof(ElapsedTime));
            RecordDeepStruct.Look(chronicler, ref StepDuration, nameof(StepDuration));
        }
    }

    private struct RawTimeRecord : IRecordable
    {
        private int _schema;
        private long _seconds;
        private uint _fraction;
        public RawTimeRecord(long seconds, uint fraction) => (_schema, _seconds, _fraction) = (1, seconds, fraction);
        public void RecordData(IChronicler chronicler)
        {
            RecordValues.Look(chronicler, ref _schema, "SchemaVersion", 0);
            RecordValues.Look(chronicler, ref _seconds, "WholeSeconds", 0L);
            RecordValues.Look(chronicler, ref _fraction, "FractionalSecond", 0u);
        }
    }
}
