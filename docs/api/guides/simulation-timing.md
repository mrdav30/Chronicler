---
title: Simulation timing
description: Advance and restore exact simulation time without floating point or a wall clock.
---

# Simulation timing

An instant tells you **when** something happened. A duration tells you **how
much time** separates two instants. Keeping those concepts distinct avoids
squeezing a long-running simulation's entire history into the numeric type
used for one physics step.

<xref:Chronicler.Timing.ChronicleTimestamp> is a nonnegative instant measured from a
simulation origin. <xref:Chronicler.Timing.ChronicleDuration> is a signed interval.
Both are immutable values available in the Standard and Lean packages, with
no math-library or engine dependency.

## Seconds and fractions

Both types store `long WholeSeconds` plus `uint FractionalSecond`. Each
fractional unit is exactly `2^-32` seconds; `0x40000000` represents a quarter
second, and `0x80000000` represents half a second. Fractions never change
precision as the simulation ages.

Whole seconds are the mathematical floor, not truncation toward zero:

| Value | WholeSeconds | FractionalSecond |
| --- | ---: | ---: |
| Zero | 0 | 0 |
| Half a second | 0 | `0x80000000` |
| Negative half a second | -1 | `0x80000000` |
| Negative one fractional unit | -1 | `uint.MaxValue` |

Every duration component pair is canonical. Timestamps reject negative whole
seconds with `ArgumentOutOfRangeException`. The default value of either type
is zero.

## Subtract instants before narrowing an interval

This complete method measures a quarter second after 100 Julian years
(365.25 days per year):

```csharp
using Chronicler.Timing;

public static class TimingExample
{
    public static ChronicleDuration MeasureInterval()
    {
        var start = new ChronicleTimestamp(3_155_760_000, 0);
        var end = start + new ChronicleDuration(0, 0x40000000);
        return end - start; // WholeSeconds = 0; FractionalSecond = 0x40000000.
    }
}
```

The instants are large; their difference is small. Keep the difference wide
until a consumer explicitly checks whether its narrower representation can
hold it. A large interval cannot become a small numeric value without either
rejecting the conversion or losing information.

## Arithmetic and boundaries

Supported operations are duration addition/subtraction, timestamp plus a
signed duration, and timestamp subtraction. Subtracting an earlier instant
from a later one gives a positive duration; reversing them gives a negative
duration. Both directions work across the complete timestamp range.

Arithmetic throws `OverflowException` if its **final result** cannot fit.
It never wraps or saturates. A duration's range runs from `(long.MinValue, 0)`
through `(long.MaxValue, uint.MaxValue)`. A timestamp's range starts at zero
and has the same upper endpoint. Moving a timestamp before its origin is also
an overflow, even though negative durations themselves are valid.

Comparisons and equality use exact values. `GetHashCode()` uses a fixed,
process-independent component mix for collections; it is not a record hash
or a persisted schema.

## Ownership stays with the host

These values do not read a system clock, advance a simulation, or carry a
world/lifetime identifier. Compare or subtract timestamps only when the host
knows they share an origin and simulation lifetime. Resetting a world does
not make a timestamp retained from the old run meaningful in the new run.

The value types remain readonly and do not implement `IRecordable`. Their
in-memory layout is not a serialization format. Use the explicit recording
helper below instead. No Fixed64 conversion API is provided by Chronicler.

Applications using FixedMathSharp can use the optional
[`FixedMathSharp.Chronicler` companion](https://github.com/mrdav30/FixedMathSharp/blob/main/src/FixedMathSharp.Chronicler/README.md).
Its `FixedChronicleTime` widens Fixed64 seconds exactly and narrows a
duration only when it fits. Subtract wide timestamps first, then use
`TryToFixed64` or the throwing `ToFixed64` conversion. The companion also counts
complete fixed-size steps using raw integer division; that is not a wait
scheduler or a lookup of historical frame numbers. This dependency points
toward Chronicler, never from Chronicler to the math library. See the companion
guide for exact conversion examples.

## Advance a clock explicitly

<xref:Chronicler.Timing.ChronicleClock> starts at frame/time zero and advances
only when its owner calls `Advance()`. Supply a positive step; there is no
default frame rate. `FrameCount` is a signed `long`, and `ElapsedTime` accumulates
each actual step. Changing `StepDuration` affects future advances, never history.

This complete example changes from half-second to quarter-second steps, saves
the clock, restores a host-created shell, and continues:

```csharp
using Chronicler;
using Chronicler.Serialization;
using Chronicler.Timing;

public static class ClockExample
{
    public static ChronicleClock RestoreAndAdvance()
    {
        var clock = new ChronicleClock(new ChronicleDuration(0, 0x80000000));
        clock.Advance();
        clock.SetStepDuration(new ChronicleDuration(0, 0x40000000));
        clock.Advance();

        string snapshot = JsonRecordSerializer.Serialize(clock);
        var restored = new ChronicleClock(new ChronicleDuration(1, 0));
        JsonRecordSerializer.Populate(restored, snapshot);
        restored.Advance();
        return restored; // Frame 3, exactly 1 second elapsed, quarter-second step.
    }
}
```

`GetDeadlineFrame(offset)` checked-adds a nonnegative frame offset; zero selects
the current frame. It does not schedule anything. Frame offsets count advances,
not seconds, so step changes do not move an existing frame deadline.

This fragment is the body of a method; the deadline is reached after two
advances even though those advances use different step sizes:

```csharp
var clock = new ChronicleClock(new ChronicleDuration(0, 0x80000000));
long due = clock.GetDeadlineFrame(2);
clock.Advance(); // Frame 1: not due yet; 0.5 seconds elapsed.
clock.SetStepDuration(new ChronicleDuration(0, 0x40000000));
clock.Advance(); // Frame 2: due; 0.75 seconds elapsed.
bool isDue = clock.FrameCount >= due;
```

Use an elapsed timestamp deadline instead when the requirement is a number of
seconds rather than a number of advances. Neither form schedules work itself.

`Reset()` returns frame/time to zero while retaining the configured step.
`Advance()` rejects an exhausted frame counter with `InvalidOperationException`
or an unrepresentable timestamp with `OverflowException`, leaving all clock
state unchanged. A deadline overflow also throws `OverflowException`.
Zero/negative steps and negative deadline offsets throw
`ArgumentOutOfRangeException`.

The clock is not thread-safe and does not run your simulation. A reset or restore
starts a new owner-controlled lifetime: quiesce the host, invalidate old waits
and pending work, and coordinate other restored state. Clock transactionality
does not roll back unrelated host code or exceptions later in a simulation step.

For a coordinated restore, stop advancement first, discard old pending work,
restore the clock and the state that belongs to its origin, then resume ordered
input. Restoring only the clock is not a rollback of the world. A context that
owns a clock may deliberately expose no live-clock population API; use that
context's supported lifecycle instead of reaching into its private clock.

Gravitas and Trailblazer each own their clock. A host using both advances each
context once per gameplay step with matching durations; the navigation adapter
does not add another advance, synchronize rates, or repair a missed step. The
standalone `ChronicleClock` example above does not replace either context's loop.

## Record values and clock state

Use <xref:Chronicler.RecordChronicleTime> inside your type's `RecordData` for
either time value. This partial schema example assumes a `ChronicleDuration`
field named `cooldown` and a `ChronicleTimestamp` field named `startedAt`:

```csharp
RecordChronicleTime.Look(chronicler, ref cooldown, "Cooldown");
RecordChronicleTime.Look(chronicler, ref startedAt, "StartedAt");
```

Each value records `SchemaVersion` (int, version 1), `WholeSeconds` (long), and
`FractionalSecond` (uint), in that order. Missing components default to zero;
a missing nested record, missing schema, or unsupported version rejects.
Negative durations are valid; negative timestamps reject. A failed helper call
leaves its referenced value unchanged, not every field of the enclosing object.

The clock records version 1, `FrameCount`, nested `ElapsedTime`, then nested
`StepDuration`. Loading stages every field before applying any clock state.
Frames must be nonnegative, the step positive, and frame/time must either both
be zero or both positive. A missing frame defaults to zero; both nested schemas
are required. The clock does **not** require elapsed time to equal frame count
times the current step, because step sizes can change.

JSON and MemoryPack follow this same schema; Lean uses JSON. Missing/unsupported
schemas and inconsistent frame/time pairs throw `InvalidOperationException`;
negative timestamps and nonpositive steps throw `ArgumentOutOfRangeException`.
Malformed transport payloads can also raise their transport's parsing errors.
Any failed clock population leaves frame, elapsed time, and step unchanged.
<xref:Chronicler.Hashing.ChronicleHashSerializer> hashes the ordered schema, including
the full-width frame and fractions, without recording host identity.
