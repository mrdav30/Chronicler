using System;

namespace Chronicler.Timing;

/// <summary>
/// A host-advanced simulation clock with an exact elapsed timeline and a positive step.
/// </summary>
/// <remarks>
/// This clock has no wall-time source or thread synchronization. Its owner controls
/// advancement and invalidates retained work when resetting or restoring a timeline.
/// </remarks>
public sealed class ChronicleClock : IRecordable
{
    /// <summary>Creates a clock at frame and time zero.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The step is not positive.</exception>
    public ChronicleClock(ChronicleDuration stepDuration)
    {
        SetStepDuration(stepDuration);
    }

    /// <summary>Gets the number of completed advances in this timeline.</summary>
    public long FrameCount { get; private set; }

    /// <summary>Gets accumulated time, preserving the history of step changes.</summary>
    public ChronicleTimestamp ElapsedTime { get; private set; }

    /// <summary>Gets the duration added by the next advance.</summary>
    public ChronicleDuration StepDuration { get; private set; }

    /// <summary>Advances one frame and one configured step, or leaves all state unchanged.</summary>
    /// <exception cref="InvalidOperationException">The frame counter is exhausted.</exception>
    /// <exception cref="OverflowException">The next elapsed timestamp cannot be represented.</exception>
    public void Advance()
    {
        if (FrameCount == long.MaxValue)
            throw new InvalidOperationException("The simulation clock's frame counter is exhausted.");
        long nextFrame = FrameCount + 1;
        ChronicleTimestamp nextTime = ElapsedTime + StepDuration;
        FrameCount = nextFrame;
        ElapsedTime = nextTime;
    }

    /// <summary>Changes future steps without changing the frame or accumulated elapsed time.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The step is not positive.</exception>
    public void SetStepDuration(ChronicleDuration stepDuration)
    {
        ValidateStepDuration(stepDuration);
        StepDuration = stepDuration;
    }

    /// <summary>Gets a future frame without scheduling work; zero selects the current frame.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The offset is negative.</exception>
    /// <exception cref="OverflowException">The deadline exceeds the frame range.</exception>
    public long GetDeadlineFrame(long framesFromNow)
    {
        if (framesFromNow < 0)
            throw new ArgumentOutOfRangeException(nameof(framesFromNow));
        return checked(FrameCount + framesFromNow);
    }

    /// <summary>Starts a new zero-based timeline, preserving the configured step.</summary>
    /// <remarks>The owner must invalidate work associated with the previous timeline.</remarks>
    public void Reset()
    {
        FrameCount = 0;
        ElapsedTime = ChronicleTimestamp.Zero;
    }

    /// <summary>Records this clock or transactionally populates it from schema version one.</summary>
    /// <remarks>
    /// Failed loading leaves the entire clock unchanged. The owner must quiesce its
    /// simulation and invalidate old work before restoring; no host services are reset here.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The schema or frame/elapsed relationship is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The elapsed timestamp is negative or the step is not positive.</exception>
    public void RecordData(IChronicler chronicler)
    {
        int schemaVersion = 1;
        long frameCount = FrameCount;
        ChronicleTimestamp elapsedTime = ElapsedTime;
        ChronicleDuration stepDuration = StepDuration;
        RecordValues.Look(chronicler, ref schemaVersion, "SchemaVersion", 0);
        RecordValues.Look(chronicler, ref frameCount, nameof(FrameCount), 0L);
        RecordChronicleTime.Look(chronicler, ref elapsedTime, nameof(ElapsedTime));
        RecordChronicleTime.Look(chronicler, ref stepDuration, nameof(StepDuration));

        if (chronicler.Mode == SerializationMode.Loading)
        {
            if (schemaVersion != 1)
                throw new InvalidOperationException("The clock schema is missing or unsupported.");
            if (frameCount < 0 || (frameCount == 0) != (elapsedTime == ChronicleTimestamp.Zero))
                throw new InvalidOperationException("Clock frames and elapsed time must be nonnegative and agree at zero.");
            ValidateStepDuration(stepDuration);
            FrameCount = frameCount;
            ElapsedTime = elapsedTime;
            StepDuration = stepDuration;
        }
    }

    private static void ValidateStepDuration(ChronicleDuration stepDuration)
    {
        if (stepDuration <= ChronicleDuration.Zero)
            throw new ArgumentOutOfRangeException(nameof(stepDuration), "The simulation step must be positive.");
    }
}
