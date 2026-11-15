//=======================================================================
// ChronicleClock.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

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
    /// <param name="stepDuration">The positive duration added to elapsed time on each advance.</param>
    /// <exception cref="ArgumentOutOfRangeException">The step is zero or negative.</exception>
    public ChronicleClock(ChronicleDuration stepDuration)
    {
        SetStepDuration(stepDuration);
    }

    /// <summary>Gets the completed advance count.</summary>
    /// <value>A nonnegative count since the current timeline's origin.</value>
    public long FrameCount { get; private set; }

    /// <summary>Gets accumulated simulation time.</summary>
    /// <value>The sum of all completed step durations relative to the timeline origin.</value>
    public ChronicleTimestamp ElapsedTime { get; private set; }

    /// <summary>Gets the next step's duration.</summary>
    /// <value>A positive interval. Changing it leaves previously accumulated time intact.</value>
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

    /// <summary>Sets the duration of future advances.</summary>
    /// <param name="stepDuration">The positive duration to use starting with the next advance.</param>
    /// <exception cref="ArgumentOutOfRangeException">The step is zero or negative. The previous step remains in effect.</exception>
    public void SetStepDuration(ChronicleDuration stepDuration)
    {
        ValidateStepDuration(stepDuration);
        StepDuration = stepDuration;
    }

    /// <summary>Calculates a frame deadline without scheduling work.</summary>
    /// <param name="framesFromNow">A nonnegative count of advances from the current frame. Zero selects the current frame.</param>
    /// <returns>The current frame plus the requested offset.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The offset is negative.</exception>
    /// <exception cref="OverflowException">The deadline exceeds <see cref="long.MaxValue"/>.</exception>
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

    /// <summary>Transfers the clock through its version-one schema.</summary>
    /// <param name="chronicler">The active pass for reading or writing clock state.</param>
    /// <remarks>The load stages all fields and validates them before updating the clock. The host stops advancement and invalidates pending work before restoring.</remarks>
    /// <exception cref="InvalidOperationException">A required schema is absent or unsupported, the frame count is negative, or exactly one of frame and elapsed time is zero.</exception>
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
