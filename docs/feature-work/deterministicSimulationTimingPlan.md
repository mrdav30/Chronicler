# Deterministic Simulation Timing Implementation Plan

> **For agentic workers:** Use `superpowers:executing-plans` for implementation,
> with independent correctness and simplification reviews at the integration
> boundaries. Use `superpowers:subagent-driven-development` if the owner chooses
> delegated implementation. Track progress with the checkboxes below.

**Date:** 2026-09-22  
**Status:** Phase 0 and Phase 1 complete; Phase 2 pending  
**Primary repository:** `F:\gamedevrepos\Chronicler`  
**Related repositories:** `FixedMathSharp`, `Gravitas`, `Trailblazer`  
**Origin:** Trailblazer `TRB-Issue-124` and the owner's request for a reusable,
optimized LSF timing contract  
**Goal:** Replace short-lived absolute frame/time representations with one
deterministic timing model that supports long-running simulations, exact bounded
fixed-point conversions, explicit state transfer, and existing host ownership.  
**Architecture:** Chronicler owns dependency-free time values and a small
explicitly advanced clock. The existing `FixedMathSharp.Chronicler` companion
owns Fixed64 interop. Gravitas and Trailblazer own simulation, presentation,
pending-work invalidation, and coordinated restore policy.  
**Tech stack:** C# 11, `netstandard2.1` and `net8.0`, xUnit v3, Chronicler's
existing JSON/MemoryPack record paths and Standard/Lean package families.  
**Spec:** The [design contract](#design-contract) in this document is the design
source of truth. The owner approved Phase 0 followed by Phase 1; later phases
remain pending.

## Why This Work Exists

Trailblazer's public `int FrameCount` becomes negative after `int.MaxValue`.
Publication requires nonnegative, strictly increasing frames and rejects after
the clock has already advanced. Its separate internal `ulong` LOS timeline does
not solve that containing-frame failure.

Gravitas also advances an `int` clock. Its collision history, grounding,
retained-partition lifecycle, coroutine waits, diagnostics, and replay hashes
consume frame stamps. In particular, retained-partition retirement uses negative
`EmptySinceFrame` values as an unset sentinel. This is source-inspected risk;
the owning end-to-end regression must be captured before claiming a fix.

Both libraries accumulate absolute seconds in `Fixed64`. Its Q32.32 range ends
after roughly 68 simulated years; saturating arithmetic can stop time advancing.
Widening frame numbers while retaining that accumulator is not the solution.

A signed `long` frame counter lasts about 9.13 billion simulated years at 32 Hz.
It is still finite: genuine exhaustion must fail before mutation. An `int`
counter plus rollover counter merely reimplements a wider integer and does not
eliminate exhaustion.

Current source searches found no equivalent simulation clock in GridForge,
SwiftCollections, FixedMathSharp core, or Chronicler. Do not add clocks to those
libraries just to standardize them. Unrelated version/identity counters are not
part of this work.

## Global Constraints

- Correctness, lockstep determinism, maintainability, then performance.
- C# 11; library targets `netstandard2.1` and `net8.0`; tests run on `net8.0`.
- Keep Chronicler's public namespace `Chronicler` and existing package IDs.
- No FixedMathSharp, SwiftCollections, GridForge, Gravitas, or Trailblazer
  dependency in Chronicler. No new package is needed for this feature.
- FixedMathSharp interop belongs in the existing `FixedMathSharp.Chronicler`
  package, which already references FixedMathSharp and Chronicler.Core.
- No runtime floating point, wall clocks, background timers, BigInteger,
  platform-sized integers, reflection-driven timing, or net8-only arithmetic.
- No periodic rebasing, automatic rollover epochs, general scheduler,
  coroutine framework, event bus, or rollback engine in Chronicler.
- No compatibility aliases preserving the old narrow absolute timeline.
- Preserve package-mode defaults. Coordinated development uses explicit
  `UseLocalLsfStack=true`; source-mode success is not released-package proof.
- Runtime construction and successful hot-path advance/comparison arithmetic
  must have clear ownership; warmed per-step arithmetic must allocate zero.
- Keep bounded counters/budgets as `int` where their documented limits fit.
  Widen absolute stamps, not every field whose name contains "Frame".
- Add meaningful behavior tests; no public API snapshots or existence-only tests.
- Do not stage, commit, push, tag, or publish without an explicit owner request.
  Suggested commit messages below are handoff text, not authorization.
- Keep feature-introduced regressions and their resolution in this plan.
  Record independently confirmed pre-existing defects in the owning tracker.

## Established Precedents

The owner asked how other systems handle a clock whose representation is wider
than an ordinary duration. The important distinction is between a wide stored
instant/interval and a narrower numerical view of that interval. No conversion
can make an out-of-range value fit without losing information or rejecting it.

- **Java `java.time`:** `Duration` stores signed long seconds plus a normalized
  nanosecond fraction, separately from `Instant`. `Duration.between` produces an
  interval; converting a large interval to a single long nanosecond count with
  `toNanos()` throws on overflow. This is a close precedent for wide duration
  storage plus an explicit checked narrow conversion. Our fraction is binary
  `2^-32` rather than decimal nanoseconds to preserve Fixed64 values exactly.
  [Oracle Duration documentation](https://docs.oracle.com/en/java/javase/21/docs/api/java.base/java/time/Duration.html)
- **.NET `Stopwatch`:** its elapsed-time API accepts counter timestamps and
  returns a `TimeSpan` interval. The .NET 8 implementation subtracts the counter
  readings before scaling the difference. This illustrates subtraction before
  representation conversion; it does not establish unlimited interval range or
  supply an LSF clock. Its hardware time source and floating-point scaling are
  not part of our deterministic implementation.
  [Microsoft API documentation](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.stopwatch.getelapsedtime?view=net-10.0),
  [.NET 8 implementation](https://github.com/dotnet/runtime/blob/v8.0.0/src/libraries/System.Private.CoreLib/src/System/Diagnostics/Stopwatch.cs)
- **Unity:** `timeAsDouble` exposes accumulated game time as a double, while
  `deltaTime` remains a float interval. Its documentation specifically motivates
  double precision for extended runtime. This demonstrates different
  representations for accumulated time and local steps, not a lockstep
  determinism guarantee. Neither floating-point type belongs in our solution.
  [Unity accumulated time](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Time-timeAsDouble.html),
  [Unity frame interval](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Time-deltaTime.html)

Our rule is therefore: subtract wide timestamps first, keep the interval wide
when necessary, and convert to Fixed64 only after checking its range. A 100-year
simulation can still have an exactly representable quarter-second interval. A
100-year interval cannot become Fixed64 seconds; keep ChronicleDuration or fail
the requested conversion explicitly. Do not convert both endpoints first.

## Design Contract

### Ownership and dependency direction

This intentionally broadens Chronicler from state transfer and record hashes
to include the small timing primitives needed to describe deterministic state
over time. It does not make Chronicler a simulation runner. Update its purpose
and documentation when the feature actually ships, not as if it exists today.

| Owner | Responsibility |
| --- | --- |
| Chronicler.Core / .Lean | Wide duration/timestamp values, explicit clock advancement, exact integer arithmetic, canonical recording |
| FixedMathSharp.Chronicler / .Lean | Exact Fixed64 conversions and bounded seconds-to-frame-count conversion |
| Gravitas / Trailblazer | Frame-rate policy, physics/navigation phases, visualization, pending work, transient lifetime identity, world restore ordering |
| Host | Input order, advancing each context once per step, coordinated pause/reset/restore and replay |

Reuse the dependency pattern of the completed
[record-hash plan](done/2026-06-26-deterministic-record-hash-framework-plan.md).
The timing code must not pull that companion package back into Chronicler.

### Values: timestamps are not durations

Proposed public types, each in its own file:

- `ChronicleDuration`: a signed elapsed interval.
- `ChronicleTimestamp`: a nonnegative instant measured from a simulation origin.
- `ChronicleClock`: a host-created reference object with one frame counter,
  timestamp, and positive step duration.

Time values use canonical **whole seconds plus 32 fractional bits**. The signed
duration stores the mathematical floor in `long WholeSeconds` and an unsigned
`uint FractionalSecond` in units of `2^-32` seconds. For example, negative one
fractional unit is `(-1, uint.MaxValue)`, not `(0, -1)`. Zero is `(0, 0)`.
Timestamps use the same precision but reject negative whole seconds.
They carry numerical time, not a world or lifetime identifier. Comparing two
timestamps as simulation instants requires the same origin/lifetime; context
owners enforce that condition for retained work rather than embedding runtime
object identity into every time value.

This representation is wider than Fixed64 without changing its fractional
resolution. `long` means whole seconds here, not a count of nanoseconds or raw
Q32.32 units. Storing raw units in a single `long` would retain the 68-year limit.

Keep the value structs readonly. Provide value equality, deterministic hash
codes, lexicographic comparison, and the following arithmetic only:

```csharp
// Proposed signatures, not existing runnable APIs.
ChronicleDuration(long wholeSeconds, uint fractionalSecond);
ChronicleTimestamp(long wholeSeconds, uint fractionalSecond);

ChronicleDuration operator +(ChronicleDuration left, ChronicleDuration right);
ChronicleDuration operator -(ChronicleDuration left, ChronicleDuration right);
ChronicleTimestamp operator +(ChronicleTimestamp time, ChronicleDuration delta);
ChronicleDuration operator -(ChronicleTimestamp left, ChronicleTimestamp right);
```

Both types also expose `Zero`, `WholeSeconds`, `FractionalSecond`, `CompareTo`,
`Equals`, and `==`, `!=`, `<`, `<=`, `>`, `>=`. Do not add multiply/divide,
calendar formatting, implicit numeric conversions, or arbitrary math operators
without a production consumer in this migration.
Use an explicitly fixed limb mix for `GetHashCode`, with expected-value test
vectors; do not use process-seeded `System.HashCode` or `HashCode.Combine`.
Collection hashing is still not a substitute for the canonical record hash.
Timestamp subtraction accepts either operand order: an earlier left operand
produces a negative duration, not an argument error. The duration representation
can express both directions across the complete nonnegative timestamp range.

Check the **final mathematical result**, including fractional carry/borrow.
An intermediate signed seconds operation must not reject a representable result:
`(-1, uint.MaxValue) + (long.MinValue, 1)` equals `(long.MinValue, 0)`.
Likewise `(long.MaxValue, 0) - (-1, 1)` is `(long.MaxValue, uint.MaxValue)`.
Use bounded unsigned limb arithmetic and explicit final sign/range checks;
do not introduce a general wide-number subsystem for these operations.

Out-of-range arithmetic throws `OverflowException`; invalid timestamp/step
arguments throw `ArgumentOutOfRangeException`. Chronicler uses ordinary explicit
guards; do not add SwiftCollections solely for its throw helper.

### Clock state and advancement

```csharp
// Proposed public surface; method bodies will be implemented with tests.
public sealed class ChronicleClock : IRecordable
{
    public ChronicleClock(ChronicleDuration stepDuration);
    public long FrameCount { get; }
    public ChronicleTimestamp ElapsedTime { get; }
    public ChronicleDuration StepDuration { get; }
    public void Advance();
    public void SetStepDuration(ChronicleDuration stepDuration);
    public long GetDeadlineFrame(long framesFromNow);
    public void Reset();
    public void RecordData(IChronicler chronicler);
}
```

- Construction starts at frame/time zero and rejects nonpositive steps.
- `Advance` calculates the next frame and timestamp into locals, validates both,
  then commits both. Exhaustion leaves frame, elapsed time, and step unchanged.
- `GetDeadlineFrame` rejects negative offsets and checked-adds to the current
  frame. Zero means the current frame. It does not enqueue work.
- `SetStepDuration` validates before mutation and affects subsequent advances
  only. It never reconstructs elapsed time using the latest step.
- `Reset` returns frame/time to zero and preserves the configured step.
- No default frame rate, visualization state, callbacks, thread safety, static
  singleton, or automatic host-loop integration is added to Chronicler.
- Frame exhaustion is an `InvalidOperationException` identifying an exhausted
  clock; timestamp/deadline arithmetic overflow is `OverflowException`.
- World owners must preflight advancement before any frame-associated mutation
  where their lifecycle currently mutates before clock advancement. Do not claim
  transactionality for arbitrary later simulation exceptions.

An explicit reset/restore is a lifetime boundary, not numerical overflow.
Context owners retain or introduce transient lifetime identity and invalidate
old waits, validation grants, guides, and pending commits. Identity is never
serialized, hashed, or ordered across peers. Standalone clock population does
not silently reset an entire host world; the host must quiesce it first.

### Fixed64 interop and seconds-based consumers

Add `FixedMathChronicleTime` to the existing companion package. Its proposed
static API is deliberately explicit:

```csharp
ChronicleDuration FromFixed64(Fixed64 value);
bool TryToFixed64(ChronicleDuration value, out Fixed64 result);
Fixed64 ToFixed64(ChronicleDuration value);
long GetFrameCountForDuration(Fixed64 duration, Fixed64 stepDuration);
```

Exact conversion is based on representation, not floating point:

```csharp
// Core of FromFixed64; namespace/imports omitted in this design sketch.
long raw = value.m_rawValue;
return new ChronicleDuration(raw >> 32, unchecked((uint)raw));

// After verifying int.MinValue <= value.WholeSeconds <= int.MaxValue:
long fixedRaw = unchecked((value.WholeSeconds << 32) | value.FractionalSecond);
return Fixed64.FromRaw(fixedRaw);
```

`TryToFixed64` returns false and a zero result outside that range. `ToFixed64`
throws `OverflowException`. No saturation, truncation of whole seconds, or
implicit timestamp conversion is allowed. All Fixed64 bit patterns round-trip,
including MinValue, MaxValue, and negative subsecond values.

`GetFrameCountForDuration` accepts nonnegative duration and positive step, then
returns `duration.m_rawValue / stepDuration.m_rawValue` using integer division.
This is the number of complete steps at that particular step size. It avoids
the old saturating Fixed64 multiplication by inverse delta time. It is not a
historical timestamp-to-frame lookup, and is not a ceiling-based wait scheduler.

Replace downstream `GetFrameFromTime` with the explicitly named
`GetFrameCountForDuration` contract. Document the intentional rounding correction
at non-binary step sizes; compare against exact raw division, not reciprocal
rounding. Do not retain a forwarding overload with the misleading old name.

Physics/navigation keep cached `Fixed64 DeltaTime` and inverse values for hot
integration. The world updates the wide clock once per step; bodies do not
convert the shared step on every use. Seconds-based waits compare wide
timestamps against `start + duration`, preserving elapsed-seconds behavior
across deliberate step changes. Compare wide durations before narrowing; an
already-expired long interval should not throw merely because it exceeds
Fixed64's range.

Example acceptance: subtract timestamps around 100 simulated years to obtain an
exact quarter-second interval, then convert **that interval** to Fixed64. Neither
absolute timestamp needs to fit Fixed64.

### Recording, hashes, and restore

- Add `RecordChronicleTime.Look` overloads for `ref ChronicleDuration` and
  `ref ChronicleTimestamp`. Reuse `RecordDeepStruct` with private recordable
  carriers; keep readonly values readonly and avoid transport-specific codecs.
- Carrier `RecordData` only reads/writes raw schema and component fields. Both
  existing transports initialize a deep struct by invoking that method against
  an empty reader before loading the real payload. Validate schema/components
  in `RecordChronicleTime.Look` **after** `RecordDeepStruct.Look` returns, then
  assign the caller's value. This permits normal present-payload loading while
  still rejecting a truly missing schema without changing serializer behavior.
- Value records have explicit `SchemaVersion = 1`, `WholeSeconds`, and
  `FractionalSecond` fields in that order. Missing whole/fraction values mean
  zero; missing or unsupported schema rejects. Saving must declare the schema
  default as zero so version one is actually emitted.
- Clock records contain, in order: `SchemaVersion = 1`, `FrameCount`, nested
  `ElapsedTime`, nested `StepDuration`. Use the same value recording helpers.
- During loading, stage every field locally. Validate schema, nonnegative
  frame/time, positive step, and frame-zero/time-zero consistency before live
  mutation. Do not infer elapsed time from frame count and current step; rate
  history may differ. Failed loading leaves the entire clock unchanged.
- A nonzero frame must have positive elapsed time; frame zero must have zero
  elapsed time. Do not require `ElapsedTime == FrameCount * StepDuration`.
- Both JSON and MemoryPack use this explicit record shape. Lean keeps JSON and
  identical timing behavior; no direct MemoryPack annotations are required.
- Hash through the existing ordered record path; never hash struct memory,
  process identity, or runtime `GetHashCode`. Field widths/order are contractual.
- Gravitas must explicitly version its affected domain replay hash; new expected
  bytes require explanation and cross-runtime conformance, not blind rebaselining.
- Existing downstream schemas containing absolute Fixed64 timestamps **or
  widened absolute frame stamps** must be versioned deliberately. This includes
  Gravitas body grounding-frame records, not just replay hashes. Reject obsolete
  shapes instead of guessing epochs or relying on one transport's numeric
  coercion. Standard/Lean and JSON/MemoryPack must agree on rejection.
- Do not expose a new live-world rewind API in this work. Recording the shared
  clock is supported; coordinated restoration remains the owner's responsibility.

## Review Focus

1. Negative fractions and carry/borrow at signed endpoints: final-result range,
   exact conversion, and comparison tests in Phase 1 and Phase 3.
2. A late invalid record field: no partial clock mutation, including schema
   omission and invalid step tests in Phase 2.
3. Old work after reset/restore, including a new run at the same frame number:
   lifetime rejection/cancellation tests in Phase 4 and Phase 5.
4. Seconds waits and jump windows after rate changes or very large elapsed time:
   interval-based tests in Phase 4 and Phase 5, not absolute-time narrowing.
5. Full-world exhaustion after some service has already mutated: boundary
   lifecycle regressions and no-mutation assertions in Phase 0, 4, and 5.

## Implementation Sequence

### Phase 0 - Capture boundaries and unchanged baselines

**Deliverable:** reproducible failure cases and a consumer inventory before
runtime changes. This phase is not permission to declare the migration done.

**Inspect:** Chronicler's `Recording`, `Serialization`, and existing tests;
FixedMathSharp's `Numerics/Scalars` and companion package; both consumer clocks,
contexts, and all callers of `FrameCount`, `TotalTime`, `GetFrameFromTime`,
`LogicalFrameIndex`, and frame-stamped records.

- [x] Record exact commits, runtime, package/source mode, configuration, and
  dirty-tree ownership for all affected repositories.
- [x] Add a Trailblazer context/publication regression that advances through
  `int.MaxValue` with a pending operation on each side. Confirm the current
  failure before changing the implementation. Seed boundary state through a
  focused test helper, not billions of simulated frames or a public setter.
- [x] Add Gravitas regressions through real retained-partition retirement and
  coroutine service execution across that boundary; distinguish existing
  wrap-safe waits from consumers that genuinely fail.
- [x] Capture Fixed64 elapsed-time saturation and narrow frame conversion with
  tests that will be replaced by the wide contract's behavior assertions.
- [x] Audit other per-step tokens, especially Gravitas `LateSimulateToken`,
  manifold update stamps, diagnostic fields, replay fields, and cached phase
  tokens. Include any consumed as absolute ordering/identity; retain bounded
  modular counters only with a documented safe contract and boundary tests.
- [x] Run existing Release/ReleaseLean suites and coverage before changes.
  Record pre-existing failures separately; do not attribute them to this feature.
- [x] Capture comparable existing Gravitas `WorldContextBenchmarks` and
  Trailblazer guided-frame/publication baselines. If a focused clock case is
  necessary, add it to an existing benchmark project before either baseline or
  implementation. No new regression project or CI benchmark service.

### Phase 1 - Canonical wide durations and timestamps

**Create:**
`src/Chronicler/Timing/ChronicleDuration.cs`,
`src/Chronicler/Timing/ChronicleTimestamp.cs`,
`tests/Chronicler.Tests/Timing/ChronicleDurationTests.cs`,
`tests/Chronicler.Tests/Timing/ChronicleTimestampTests.cs`.

**Produces:** the value constructors, comparisons, and operators in the design
contract; no downstream dependency is needed.

- [x] Write failing value/ordering and boundary arithmetic tests. Include this
  assertion in a Fact with `using System; using Xunit;`:

  ```csharp
  var left = new ChronicleDuration(-1, uint.MaxValue);
  var right = new ChronicleDuration(long.MinValue, 1);
  Assert.Equal(new ChronicleDuration(long.MinValue, 0), left + right);
  Assert.Throws<OverflowException>(() =>
  {
      _ = new ChronicleDuration(long.MaxValue, uint.MaxValue)
          + new ChronicleDuration(0, 1);
  });
  ```

- [x] Run `dotnet test tests/Chronicler.Tests/Chronicler.Tests.csproj -c Release
  --filter FullyQualifiedName~ChronicleDurationTests`; first establish the
  missing/incorrect contract, then implement the smallest bounded arithmetic.
- [x] Cover zero/default, equal values, each ordering limb, negative fractions,
  positive carry, negative borrow, valid intermediate-overflow cases, genuine
  positive/negative overflow, and timestamp results below zero.
  In particular, pin both subtraction directions across the full timestamp range:

  ```csharp
  var last = new ChronicleTimestamp(long.MaxValue, uint.MaxValue);
  Assert.Equal(new ChronicleDuration(long.MinValue, 1),
      ChronicleTimestamp.Zero - last);
  Assert.Equal(new ChronicleDuration(long.MaxValue, uint.MaxValue),
      last - ChronicleTimestamp.Zero);
  ```
- [x] Prove translation invariance: the same short interval is obtained near
  time zero and beyond `int.MaxValue` whole seconds. Check arithmetic against
  hand-derived boundary vectors and a test-only BigInteger oracle; never use
  that oracle in runtime code.
- [x] Run the focused Timing filter in Release and ReleaseLean, then measure
  warmed successful arithmetic allocations using the existing test helper.

**Suggested commit:** `feat: add deterministic wide time values`

### Phase 2 - Shared clock and explicit state transfer

**Create:**
`src/Chronicler/Timing/ChronicleClock.cs`,
`src/Chronicler/Recording/RecordChronicleTime.cs`,
`tests/Chronicler.Tests/Timing/ChronicleClockTests.cs`,
`tests/Chronicler.Tests/Timing/ChronicleTimeRecordingTests.cs`.

**Consumes:** Phase 1 values. **Produces:** the clock and recording contracts.

- [ ] Write step-change tests before implementation, including:

  ```csharp
  var clock = new ChronicleClock(new ChronicleDuration(0, 0x80000000u));
  clock.Advance();
  clock.SetStepDuration(new ChronicleDuration(0, 0x40000000u));
  clock.Advance();
  Assert.Equal(2L, clock.FrameCount);
  Assert.Equal(new ChronicleTimestamp(0, 0xC0000000u), clock.ElapsedTime);
  ```

- [ ] Implement staged advance, positive-step validation, deadline checks,
  reset-preserves-step behavior, and exact typed recording. Run the Timing filter
  after each red/green step in both configurations.
- [ ] Reuse `SerializationTestHarness` for each enabled transport. Round-trip
  zero/default time, negative durations, fractional endpoints, and a clock past
  `int.MaxValue` frames and whole seconds; continue advancing after load.
- [ ] Seed a clock at `long.MaxValue` frames through a valid record; assert
  advancement fails with frame/time/step unchanged. Separately seed timestamp
  exhaustion with a smaller frame value. Test zero/negative step and deadline
  offsets at zero, exact remaining capacity, and one beyond capacity.
- [ ] Corrupt the final step record and schema version; assert population leaves
  a previously running target unchanged. Cover absent nested records, missing
  schema, unsupported versions, and inconsistent zero-frame/elapsed pairs.
- [ ] Compare canonical record hashes after round-trip and step-by-step replay.
  Confirm changing one fractional unit, step, or high frame bits changes the
  recorded input. Hashing an unchanged record must remain stable across builds.
- [ ] Verify zero allocations for warmed `Advance`, `SetStepDuration`, deadline,
  and comparison success paths; state transfer need not be allocation-free.

**Suggested commit:** `feat: add recordable deterministic simulation clock`

### Phase 3 - FixedMathSharp bridge and coordinated source graph

**Create in FixedMathSharp:**
`src/FixedMathSharp.Chronicler/FixedMathChronicleTime.cs`,
`tests/FixedMathSharp.Chronicler.Tests/FixedMathChronicleTimeTests.cs`.

**Modify as needed for source validation:**
`src/FixedMathSharp.Chronicler/FixedMathSharp.Chronicler.csproj`,
`src/Gravitas/Gravitas.csproj`,
`src/Trailblazer/Trailblazer.csproj`, and adapter/test references that directly
consume Chronicler. Paths in this and later phases are relative to their named
repositories. Package references remain the default; local mode must resolve
one consistent Chronicler assembly transitively, not mix a new source assembly
with the published 0.4.0 copy.

- [ ] Add exact conversion tests, including:

  ```csharp
  var tiny = FixedMathChronicleTime.FromFixed64(Fixed64.FromRaw(-1));
  Assert.Equal(new ChronicleDuration(-1, uint.MaxValue), tiny);
  Assert.Equal(-1L, FixedMathChronicleTime.ToFixed64(tiny).m_rawValue);
  Assert.False(FixedMathChronicleTime.TryToFixed64(
      new ChronicleDuration((long)int.MaxValue + 1, 0), out var result));
  Assert.Equal(Fixed64.Zero, result);
  ```

- [ ] Verify all extreme/raw sign-transition vectors, exact integral seconds,
  negative subsecond values, and seeded representative raw payloads. Test the
  throwing conversion independently of the Try contract.
- [ ] Implement the explicit bridge without changing Fixed64 saturation or
  conversion semantics elsewhere. Run
  `dotnet test tests/FixedMathSharp.Chronicler.Tests/FixedMathSharp.Chronicler.Tests.csproj
  -c Release -p:UseLocalLsfStack=true --filter FullyQualifiedName~FixedMathChronicleTimeTests`
  and repeat for ReleaseLean.
- [ ] Prove the 100-year timestamp difference example narrows exactly to 0.25
  seconds while attempting to narrow a 100-year duration fails explicitly.
- [ ] Test `GetFrameCountForDuration` at zero, one fractional unit, an exact step,
  one unit below/above it, a result beyond `int.MaxValue`, invalid signs, and
  non-power-of-two frame rates. Expected values use raw integer division.
- [ ] Inspect restored project assets and built assembly references in both
  configurations. Ensure no Chronicler-to-FixedMathSharp dependency or mixed
  Standard/Lean family was introduced. Do not release an upstream package merely
  to make this development pass compile.

**Suggested commit:** `feat: bridge Chronicler timing with Fixed64`

### Phase 4 - Migrate Gravitas and prove real lifecycle boundaries

**Primary source:** `src/Gravitas/Runtime/GravitasClock.cs`,
`Runtime/GravitasWorldContext.cs`, `Partitions/RetainedPartitionLifecycle.cs`,
the 2D/3D/mixed partition and collision services, `CollisionHandling`,
`Support/Coroutines/LockedYieldInstructions`, `Diagnostics`, and
`Determinism/GravitasReplayHashService.cs`.

**Primary tests:** `tests/Gravitas.Tests/Runtime/GravitasClockTests.cs`,
`Runtime/GravitasWorldContextTests.cs`, `Support/Coroutines`, partition/contact
tests in `Core`, and `Determinism` replay/conformance suites.

- [ ] Make Phase 0 regressions red on the pre-migration implementation. Extend
  them for wide elapsed time, cached contact/grounding ages, mixed/2D paths,
  retained empty partitions, and diagnostics at the old boundary.
- [ ] Replace duplicated authoritative counter/time state with ChronicleClock.
  Keep Gravitas-specific visualization and frame-rate constraints local. Use
  `long` for absolute frame/phase stamps and wide timestamps for absolute time;
  keep step integration in Fixed64 and bounded sleep/retention durations bounded.
- [ ] Replace public `TotalTime` with `ChronicleTimestamp ElapsedTime`; migrate
  consumers instead of retaining a narrow alternate absolute-time property.
  Replace the historical-sounding frame conversion with the duration contract.
- [ ] Migrate seconds waits to timestamp deadlines and frame waits to long
  differences/deadlines. Test waits begun before a step-size change, zero waits,
  cancellation/disposal, and stale instructions after context reset. Repeated
  `KeepWaiting` reads must not advance a countdown or change behavior.
- [ ] Preserve lifetime invalidation without serializing process-local identity.
  Check no mutation precedes clock exhaustion in the containing lifecycle.
- [ ] Widen/version diagnostic and replay stamp fields, including any necessary
  late-simulation token change established by Phase 0. Verify consistent semantic
  replay, and document why old/new hash schemas intentionally differ.
- [ ] Audit body records independently of replay hashing. In particular,
  `Core/3D/SolidBody.Serialization.cs` records `_lastGroundCheckFrame` as an int
  today. Version affected body-record shapes when widening stamps, with owning
  2D/3D serialization tests for values beyond int range, old-width payload
  rejection, transport parity, and no live mutation when the new timing/schema
  preflight rejects. This does not expand into rewriting unrelated body records.
- [ ] Run focused Runtime/Coroutines/partition/determinism filters in both
  configurations, then the full Gravitas matrix. Compare the existing benchmark
  baseline with identical workload and runtime; explain any measurable regression.

**Suggested commit:** `fix: migrate Gravitas to long-lived simulation timing`

### Phase 5 - Migrate Trailblazer and the Gravitas adapter

**Primary source:** `src/Trailblazer/Runtime`,
`Pathing/Map/Operations`, `Pathing/Traversal/NavigationAreaPolicyCommitOperation.cs`,
`Pathing/Graph`, `Navigation/MovementGroups`, `Navigation/Steering`,
`Navigation/Motor`, `Navigation/Navigator/NavigationCommittedCellState.cs`,
and `src/Trailblazer.Gravitas/GravitasNavigator3D.cs`.

**Primary tests:** `tests/Trailblazer.Tests/Runtime`, `Pathing/Graph`,
`Navigation/MovementGroups`, `Navigation/Steering`, `Navigation/Motor`,
Navigator record tests, and `tests/Trailblazer.Gravitas.Tests`.

- [ ] Reproduce pending publication across the old boundary through public
  `context.Simulate`, not only the internal clock. Assert receipt status and
  exact long publication frames, including queued future work and rejection of
  late/nonmonotonic authoring.
- [ ] Replace authoritative clock state with ChronicleClock and remove the
  separate internal unsigned timeline. Widen operations, receipts, group history,
  grants, acquisition attempts, committed-cell notifications, motor transactions,
  and adapter prepare/commit stamps together. Preserve volatile publication
  semantics when changing receipt fields to long.
- [ ] Keep world-specific clock lifetime tokens and invalidation rules. Preserve
  FIFO/cohort scheduling, budgets, source-pin ownership, exactly-once result
  transfer, LOS phase fairness, and reset/cancellation behavior.
- [ ] Replace `TotalTime` with wide `ElapsedTime`, and move absolute jump-start
  timestamps to the wide contract. Keep ordinary cooldown durations in Fixed64.
  Test identical jump-hold behavior at early and late world times. Inspect
  `NavMotor.Traversal`'s current `(JumpStartTime + ExtraJumpHeight) / speed`
  expression: do not mechanically preserve mixed timestamp/height arithmetic.
  Capture any confirmed pre-existing units defect with an owning regression and
  tracker entry, then use an elapsed-duration comparison with defined zero-speed
  behavior. This is a correctness gate, not permission for unrelated motor work.
- [ ] Version affected locomotion/Navigator records and preserve transactional
  loading across JSON/MemoryPack. Old shapes reject; no epoch guessing or
  compatibility wrappers. Restore worlds before guided controllers as today.
- [ ] Verify long-frame group expiry, exactly-once motor commit, expired guide
  validation grants, adoption/recovery, reset to the same numeric frame, and
  exhausted-clock rejection with pending work unchanged.
- [ ] Run real Gravitas adapter prepare/resolve/commit across the old frame and
  elapsed-time limits. Navigation and physics each advance once; do not share
  one mutable clock that both contexts advance. Verify step agreement and reset
  ownership without changing the existing all-prepare/one-resolve/all-commit
  barrier.
- [ ] Run both core/adapter suites and existing guided-frame/publication
  benchmarks. Preserve allocation gates and account for widened retained-state
  layouts rather than weakening memory ceilings or hiding a regression.

**Suggested commit:** `fix: unify Trailblazer timing across publication and navigation`

### Phase 6 - Documentation, full validation, and closeout

**Chronicler docs:** update `README.md`, `AGENTS.md`, `CONTRIBUTING.md`,
`docs/api/index.md`, `docs/api/toc.yml`, `docs/api/overrides/Chronicler.md`,
and add `docs/api/guides/simulation-timing.md`. Existing API metadata already
includes Chronicler.dll; do not add another assembly or generated source tree.
Update `Description` and `PackageTags` in `src/Chronicler/Chronicler.csproj`
and FixedMathSharp's `src/FixedMathSharp.Chronicler/FixedMathSharp.Chronicler.csproj`
so the NuGet metadata describes timing and conversion support as well as the
existing functionality. Preserve package IDs, assembly names, and namespaces.

**Other docs:** FixedMathSharp companion README/API guidance; Gravitas runtime,
host-integration, serialization, diagnostics, and migration guidance; Trailblazer
Overview, Pathing, MapPublication, Serialization, Gravitas, migration guidance,
and affected XML comments. Keep evergreen docs free of feature-plan links.

- [ ] Compile documentation examples in behavior tests. Demonstrate a wide
  timestamp, bounded duration conversion, frame deadline, and rate change without
  a standalone sample project. Include owner-driven reset/restore rules.
- [ ] Build `Chronicler.slnx` and run all tests in Release and ReleaseLean,
  including shim tests. Collect coverage in each configuration:

  ```powershell
  dotnet build Chronicler.slnx -c Release
  dotnet test Chronicler.slnx -c Release --no-build
  dotnet test tests/Chronicler.Tests/Chronicler.Tests.csproj -c Release --no-build --collect:"XPlat Code Coverage" --settings tests/Chronicler.Tests/coverlet.runsettings --results-directory artifacts/timing/Release
  dotnet build Chronicler.slnx -c ReleaseLean
  dotnet test Chronicler.slnx -c ReleaseLean --no-build
  dotnet test tests/Chronicler.Tests/Chronicler.Tests.csproj -c ReleaseLean --no-build --collect:"XPlat Code Coverage" --settings tests/Chronicler.Tests/coverlet.runsettings --results-directory artifacts/timing/ReleaseLean
  dotnet build Chronicler.slnx -c Release
  dotnet tool restore
  dotnet tool run docfx docs/api/docfx.json --warningsAsErrors
  ```

- [ ] Build both library target frameworks and run the full affected downstream
  matrices with `UseLocalLsfStack=true` in Release and ReleaseLean. Validate
  Windows and Linux; distinguish independent builds from executing copied DLLs.
- [ ] Require 100% reachable line/branch/method coverage for introduced timing
  code and preserve existing downstream gates. Verify report assembly names,
  covered/total counts, and methods, not just rounded line percentages. Do not
  add coverage exclusions or hollow tests to reach the target.
- [ ] Require zero warmed timing allocations and retain comparable before/after
  containing-frame measurements. Use repeated matched runs if changes approach
  noise. Record data-layout costs and any accepted regression explicitly.
- [ ] Verify private package consumers with isolated caches, correct source
  mapping, and consistent Standard/Lean dependencies before release readiness
  claims. Do not install fake release versions into the normal cache.
- [ ] Obtain independent correctness and Ponytail simplification reviews of
  arithmetic, restore atomicity, dependency graph, and downstream lifetimes.
  Resolve findings and rerun affected gates before marking work complete.
- [ ] Resolve `TRB-Issue-124` only after the containing-frame regression passes
  and all stamp consumers have migrated. Update owning trackers for independently
  confirmed pre-existing issues. Planning alone closes nothing.
- [ ] Condense final evidence, limitations, and decisions into this document;
  move it to `docs/feature-work/done` only when the complete scope is finished.

**Suggested commit:** `docs: document shared simulation timing and migration`

## Release and Execution Boundaries

Implementation order is Chronicler, FixedMathSharp companion, Gravitas, then
Trailblazer/core adapter. No GridForge or SwiftCollections source work is planned.
When the owner eventually releases the stack, release/validate in that same
dependency order using actual packages. Do not assign versions or publish as
part of this plan; the owner is holding releases until the broader feature work
is finished.

Recommended execution is direct implementation phase-by-phase with independent
review at the shared arithmetic/record contract and downstream migration gates.
Most tasks share a small public contract, so parallel edits across that contract
would add avoidable coordination risk. The owner reviews this plan before work
begins; a separate task or branch is created only if requested.

## Progress and Evidence

- [x] Source inspection and dependency-boundary review for this proposal.
- [x] Draft design and implementation sequence captured in Chronicler.
- [x] Independent plan review approved after clarifying signed endpoint
  subtraction, fixed value hashing, widened body-record schemas, and package
  metadata. Deep-struct carrier validation was also checked against both
  current transport implementations. Later-phase runtime acceptance remains
  unexecuted.
- [x] Owner review of this plan; Phase 0 and Phase 1 authorized on 2026-09-22.
- [x] Phase 0: boundary regressions and baseline evidence.
- [x] Phase 1: canonical wide values.
- [ ] Phase 2: clock and explicit recording.
- [ ] Phase 3: FixedMathSharp bridge and source graph.
- [ ] Phase 4: Gravitas adoption.
- [ ] Phase 5: Trailblazer and adapter adoption.
- [ ] Phase 6: documentation, full validation, review, and closeout.

### Phase 0 / 1 execution record - 2026-09-22

All checkouts began clean on `develop`. No branches, commits, package releases,
dependency changes, or downstream runtime fixes were made. Evidence is under
Chronicler `artifacts/timing/phase0` and `artifacts/timing/phase1`.

| Repository | Baseline commit |
| --- | --- |
| Chronicler | `d2de217832019a39567185e892c5b3973b87d22d` |
| FixedMathSharp | `d5d8782d4e031a372bf39a79d9f46f73d8ed9aee` |
| Gravitas | `21a22ab1ac8ac2d16fb8a41fcf1f65659029378d` |
| Trailblazer | `fd6b4849a95e468a4464a95dbef0f432f610ebb6` |
| SwiftCollections (unchanged dependency) | `1ed2be3482798c7800d917fb46865a55dc222cbf` |
| GridForge (unchanged dependency) | `e7223aef007a9c74e830b1e820c59fc01e49b7d8` |

Windows x64 uses SDK 10.0.302 and .NET 8.0.29. Downstream baseline commands use
`UseLocalLsfStack=true`; their existing Chronicler dependency still resolves
published 0.4.0. Phase 1 values are not silently injected into those consumers.
The full baseline suites use `dotnet test <repo>.slnx -c <configuration>
-p:UseLocalLsfStack=true --collect:"XPlat Code Coverage"
--settings tests/<repo>.Tests/coverlet.runsettings --results-directory <output>`.
Chronicler omits the local-stack property. Trailblazer's adapter is also
collected separately with its own runsettings so core coverage cannot mask it.

**Execution decisions:** Keep the existing checkouts and leave changes
uncommitted, as authorized. Run intentionally red downstream probes temporarily
in their owning test assemblies, retain their source/output in ignored evidence,
then remove only those temporary files. Do not introduce skipped tests or tests
that bless broken behavior. Phases 4/5 must promote these assertions into normal
regressions with the fixes. After the probes confirmed the design, standalone
Phase 1 work overlapped the remaining baseline checks; it cannot affect the
unchanged downstream package graph. Timing captures run without concurrent
tests/builds. If that separation is broken, repeat the affected capture.

#### Reproduced boundaries

Each probe runs in the normal net8.0 xUnit test assembly, not PowerShell's CLR.
Use `dotnet test tests/<repo>.Tests/<repo>.Tests.csproj -c Release
-p:UseLocalLsfStack=true --filter FullyQualifiedName~TimingBoundaryProbeTests
--logger:"console;verbosity=normal"`. Only test setup uses reflection to seed
private clock state. No runtime setters were added.

- **Trailblazer publication:** Create an owned context with one area,
  capacity for two one-rule policies, and `MaxDependencyEntries = 5`; preserve
  other default settings. Seed `_clock._frameIndex = int.MaxValue - 1`.
  Admit distinct policies with sequences 1/2, both effective at `int.MaxValue`.
  On the first `Simulate`, the first receipt is Applied at `int.MaxValue`; the
  second is Pending because the budget fits only the first publication. On the
  second `Simulate`, expect the second to apply at 2,147,483,648. Actual:
  `NavigationOperationProcessor.ProcessFrame` throws `ArgumentException`
  after `FrameCount` becomes -2,147,483,648, leaving the receipt Pending.
  Control: seed zero and make both policies effective at 1; they publish at
  frames 1 and 2. A genuinely future effective frame above int.MaxValue cannot
  even be expressed by today's public constructor; test that new capability in
  Phase 5. `TRB-Issue-124` now records this containing-frame evidence.
- **Gravitas retirement:** Create a grid from (-2,-2,-2) to (2,2,2), initialize
  a mass-one sphere at zero, set retention TTL to 2 and sweep budget to 1024.
  Seed the clock's `FrameCount` to int.MaxValue, run `Simulate`/`LateSimulate`,
  deactivate the collider, then run two more complete steps. Expected retained
  partitions: zero; actual: 27. Control starting at zero retires every partition.
  The wrapped `EmptySinceFrame` is mistaken for the negative unset sentinel.
- **Gravitas coroutine control:** Seed int.MaxValue - 1; start a coroutine that
  yields `WaitForFrames(2)` on its first step. It resumes exactly once on the
  third step, even across signed wrap. Unsigned subtraction is sufficient for
  that bounded wait; this is not evidence that every frame consumer is safe.
- **Gravitas seconds wait:** Seed `TotalTime = Fixed64.MaxValue - DeltaTime`.
  On the next step, start a one-second coroutine wait. It resumes on the very
  next 1/32-second step instead of remaining pending: the absolute deadline
  saturated. This demonstrates early completion, not only frozen time.
- **Both clocks:** Seed TotalTime at Fixed64.MaxValue and step once. An assertion
  that time increases fails while frames advance. At 32 Hz,
  `GetFrameFromTime((Fixed64)67108864)` returns 2,147,483,647 instead of
  2,147,483,648. Expected values follow exact integer division.

Gravitas probes: two passing controls and four expected failures. Trailblazer:
one passing control and three expected failures. Logs are `gravitas-probes.log`
and `trailblazer-probes.log`; the adjacent `*TimingBoundaryProbeTests.cs` files
retain the full reproduction code. Gravitas tracks the confirmed pre-existing
defects as `GRV-Issue-076`. Neither issue is resolved by adding value types.

#### Consumer inventory and migration decisions

| Consumer | Classification / required handling |
| --- | --- |
| Both clocks and world contexts | Absolute frame/time state; replace narrow frame and Fixed64 total, preflight exhaustion before owner mutation |
| Trailblazer operation descriptors, receipt PublishedFrame, processors and graph maintenance | Absolute ordering; widen together, preserve atomic receipt publication and pending-work rules |
| Trailblazer LogicalFrameIndex, LOS attempt/grant frames and ClockLifetime | Existing unsigned absolute stamps plus transient identity; unify the stamp, retain lifetime invalidation |
| MovementGroupMember/Membership.LastSeenFrame and coordinator cutoffs | Absolute history/order; widen stamps, keep one-frame history duration bounded |
| NavMotor pending traversal, NavigationCommittedCellState, GravitasNavigator3D prepare frame | Absolute exactly-once ownership stamps; migrate core and adapter together |
| JumpLocomotion.JumpStartTime and its record | Absolute seconds; wide timestamp and explicit schema change; independently verify the existing mixed-unit hold formula in Phase 5 |
| Gravitas retained 2D/3D/mixed partitions | Absolute empty-since stamp and negative sentinel; wide stamp, bounded TTL/sweep budget |
| CollisionPair/2D/Mixed.LastFrame, 3D LastCollidedFrame, ContactManifold/2D.LastUpdatedFrame | Absolute contact history/identity, ages and replay fields; widen, preserve reset semantics |
| SolidBody grounding stamp and LastGroundCheckFrame record | Absolute cached age; widen/version the body record independently of replay hashing |
| Gravitas LateSimulateToken and CCD frame/handoff/query-refresh tokens | Per-phase identity, not a duration; equality-only does not prove unlimited stale-token safety. int.MinValue is also used for invalid trajectories. Widen with explicit exhaustion and reset tests |
| Diagnostic event/draw frame fields and GravitasReplayHashService | Externally observed absolute stamps; widen and deliberately version replay schema |
| WaitForFrames / WaitForNextSimulate | Bounded unsigned subtraction / equality currently survive immediate signed wrap; preserve exact service behavior, add wide duration/lifetime rules with migration |
| WaitForRealSeconds | Absolute deadline; construct from wide elapsed time and compare without narrowing |
| Sleep counters, ground-check intervals, retention TTLs, work budgets, solver iterations, LOS intervals | Bounded durations/counts; do not widen merely because the field mentions frames |

Current source searches found no FrameCount/TotalTime/GetFrameFromTime/DeltaTime
clock consumers in GridForge or SwiftCollections. Their unrelated topology,
publication and identity counters are not included in this migration.

#### Unchanged Windows baseline results

| Suite | Release tests | ReleaseLean tests | Baseline core coverage: lines / branches / methods |
| --- | ---: | ---: | --- |
| Chronicler | 143 + 4 shim | 104 + 4 shim | Release 931/935, 402/421, 197/197; Lean 694/756, 285/321, 143/165 |
| FixedMathSharp | 2,824 + 8 companion | 2,803 + 8 companion | Release 47,681/47,681, 8,898/8,898, 3,409/3,409; Lean 47,673/47,673, 8,898/8,898, 3,405/3,405 |
| Gravitas | 4,062 | 4,007 | Release 44,265/44,265, 13,030/13,030, 4,536/4,536; Lean 44,263/44,263, 13,030/13,030, 4,535/4,535 |
| Trailblazer | 3,187 + 84 adapter | 3,096 + 80 adapter | Both: 32,267/32,267, 13,248/13,248, 3,175/3,175 |

All existing suites passed, without skips. FixedMathSharp.Chronicler separately
reports 70/70 lines, 2/2 branches, 14/14 methods in each configuration;
Trailblazer.Gravitas reports 192/192, 30/30, 13/13. Do not interpret the adapter
test assembly's partial coverage of core as the core suite's coverage.

Chronicler's existing gaps precede this feature. No exclusions or weakened
assertions were added to conceal them. Shim package tests build external
consumers, so a core report containing an uninstrumented shim does not establish
shim coverage. Phase 1's exact new-code gate is separate from those baselines.

#### Phase 1 implementation and verification

Added only `ChronicleDuration` and `ChronicleTimestamp`; no clock, recording
helper or Fixed64 bridge exists yet. Timestamp stores the duration components
privately to reuse exact comparison, hashing and arithmetic. Signed overflow is
checked after fractional carry/borrow, without a generic wide-number helper.

The initial test build failed because both public value types were absent
(`timing-red.log`). Eighteen new cases then passed, including 2,025 endpoint
pairs and 2,048 seeded pairs checked against a test-only BigInteger oracle,
timestamp range/translation tests, fixed hash vectors and warmed allocation
checks. Both allocation cases perform 1,024 value-operation iterations and
assert the resulting time and ordering count, not merely API existence.

Windows full suites pass 161 core + 4 shim cases in Release and 122 + 4 in Lean.
Introduced timing code is **48/48 lines, 20/20 branches, 30/30 methods** in each
configuration: duration 26/14/14 and timestamp 22/6/16. Existing uncovered code
is unchanged. No runtime dependency, net8-only API or coverage exclusion was
introduced. Both library target frameworks build.

Linux/WSL independently builds both target frameworks and passes the same full
suite counts in both configurations (SDK 10.0.203, runtime 8.0.26). The first
Linux run passed the core suite but stalled inside unchanged shim package tests
after a nested build left a reused MSBuild worker alive. That run was terminated
and retained as incomplete, not counted as green. Repeating the full commands
with `MSBUILDDISABLENODEREUSE=1` passes all tests; no source change or increased
timeout was used. Logs: `linux-Release.log` (incomplete),
`linux-no-node-reuse-Release.log`, `linux-no-node-reuse-ReleaseLean.log`.

The public guide's complete example is exercised by a behavior test. A fresh
Windows Release build and `dotnet tool run docfx docs/api/docfx.json
--warningsAsErrors` pass with zero warnings/errors. Package metadata and docs
describe only the implemented values, not the future clock or interop helpers.

Independent correctness and Ponytail review approved the Phase 0/1 source,
tests, docs and probe-evidence approach with no findings or recommended
deletions. It independently checked 131,072 reduced-width arithmetic operations
without mismatches. Final benchmark/provenance acceptance remains the primary
executor's responsibility; Phase 2+ behavior and release readiness are not
claimed by that review.

#### Frozen performance baselines

These are unchanged-consumer baselines for the later migrations, not a Phase 1
performance improvement. All timing runs used the Windows machine described
above (Intel i7-9700K, eight physical/logical cores), with no concurrent test or
build workload. BenchmarkDotNet is 0.15.8. Build each existing benchmark project
in Release/net8.0 with `-p:UseLocalLsfStack=true`, then launch its built DLL from
the owning repository with environment `UseLocalLsfStack=true`.

Trailblazer commands after that build, with `<evidence>` denoting this capture's
output directory:

```powershell
dotnet tests/Trailblazer.Benchmarks/bin/Release/net8.0/Trailblazer.Benchmarks.dll navigation-guided-frame --filter '*Simulate64GuidedFrames(AgentCount: 100,*' '*Simulate64GuidedFrames(AgentCount: 500,*FlowField*' --launchCount 3 --warmupCount 1 --iterationCount 3 --keepFiles --exporters json --artifacts <evidence>/guided
./tests/Trailblazer.Benchmarks/AnalyzeGuidedFrames.ps1 -LogPath <evidence>/guided.log -OutputPath <evidence>/guided-summary.json
dotnet tests/Trailblazer.Benchmarks/bin/Release/net8.0/Trailblazer.Benchmarks.dll navigation-graph-lifecycle --filter '*PublishOnePhysicalCellChange*' --keepFiles --exporters json --artifacts <evidence>/publication
```

Capture the complete console output in the indicated log before analysis. The
existing Trailblazer launcher supplies `UsePrebuiltLocalLsfStack=true` and `/m:1`
to generated builds. Guided acquisition uses the original
`Simulate64GuidedFrames` case, not an instrumented phase variant. Each case has
three launches, one warmup and three actual 64-frame blocks per launch: nine
actual blocks / 576 frames per case. All nine children exited successfully;
the existing analyzer validated block-stage pairing and replay. Its nested
containing-frame results are:

| Case | Median ms | Observed P99 ms | Maximum ms | Measured allocated bytes |
| --- | ---: | ---: | ---: | ---: |
| AStar / 100 | 1.74665 | 4.746525 | 5.3656 | 0 |
| Flow / 100 | 1.30415 | 5.509075 | 6.0120 | 0 |
| Flow / 500 | 3.08190 | 15.166950 | 15.9739 | 0 |

No measured frame overlapped a collection or exceeded 31.25 ms. Launch drift,
especially Flow/100, prevents treating these few launches as a precise historical
comparison. Do not compare against TRB-Benchmark-005's older stopping-point
capture or divide the outer 64-frame benchmark mean into a gameplay-frame claim.
Raw logs, exported results and analyzer JSON remain beside this evidence.

Publication's existing canonical Monitoring job (three launches, 100 measured
iterations per launch) has medians **31.450 / 34.700 / 49.600 microseconds** for
1/16/128 maps and reports 5.35/5.52/5.68 KB allocated. The harness also runs its
separate short in-process job; do not blend those samples. All six configured
cases executed. Minimum-iteration/bimodal warnings remain in `publication.log`;
this capture is a reproducible reference, not a fine-grained optimization claim.

Gravitas's out-of-process local-stack benchmark did not produce a valid result.
The first generated build failed with CS2012 during a parallel intermediate DLL
write; a serial-build retry compiled but its child failed to load GridForge
9.1.0.0. The child emitted GridForge/SwiftCollections identities 0.0.0.0, unlike
the source-built parent. Both launchers misleadingly returned zero.
**GRV-Issue-077** records the pre-existing build-graph/result-handling defect.
Neither failed run is counted as a successful baseline.

The existing full in-process toolchain successfully captured the unchanged
Gravitas case (not a short job):

```powershell
dotnet tests/Gravitas.Benchmarks/bin/Release/net8.0/Gravitas.Benchmarks.dll world-context --filter '*RunEmptySimulationFrame*' --inProcess --exporters json --artifacts <evidence>/gravitas-inprocess
```

`RunEmptySimulationFrame` reports **63.063 ns mean**, 15 actual iterations,
0.010 ns standard deviation, and no allocated bytes. Its scope is an empty
containing simulation frame, not a populated physics workload. Phase 4 must
repeat this same toolchain or recapture the unchanged baseline before migration
if GRV-Issue-077 is fixed and out-of-process measurement is selected instead.
No benchmark source, runtime workaround, or new benchmark project was added.

`phase0/frozen-manifest.json` records SHA-256 for 364 frozen files: successful
Trailblazer generated build trees, the failed serial Gravitas child, and the
successful in-process Gravitas host assemblies. All copied hashes were verified.
The Trailblazer parent Chronicler assembly matches published 0.4.0, SHA-256
`EF1A69BFA0940A0C8593F06242EE189547E9455DA9C22FF34F8F7AA81A50CBBD`.
Logs, reports, reproduction sources and generated files are local ignored
evidence, not release artifacts; keep them through the migration comparison.

**Phase boundary:** Phase 0/1 are complete. Phase 2 is next; no clock,
serialization schema, Fixed64 conversion or consumer migration is implemented
by this change. TRB-Issue-124 and GRV-Issue-076 remain open until their owning
simulation regressions pass with those later changes. GRV-Issue-077 is a
separate benchmark-tooling follow-up, not a blocker for the standalone values.
