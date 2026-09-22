---
title: Simulation time values
description: Represent long-lived simulation instants and exact signed intervals without floating point.
---

# Simulation time values

An instant tells you **when** something happened. A duration tells you **how
much time** separates two instants. Keeping those concepts distinct avoids
squeezing a long-running simulation's entire history into the numeric type
used for one physics step.

<xref:Chronicler.ChronicleTimestamp> is a nonnegative instant measured from a
simulation origin. <xref:Chronicler.ChronicleDuration> is a signed interval.
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
using Chronicler;

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

The value types do not implement `IRecordable` or supply a Fixed64 conversion
API. Their in-memory layout is not a serialization format. Simulation-loop,
recording, and numerical-conversion policies are separate from this value
arithmetic contract.
