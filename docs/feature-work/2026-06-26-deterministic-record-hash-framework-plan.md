# Deterministic Record Hash Framework Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` for independent review tasks, or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a reusable deterministic record-hash framework to Chronicler, then layer FixedMathSharp-specific hash codecs in a separate FixedMathSharp package so Gravitas and future LSF libraries can share replay/conformance hashing without duplicating infrastructure.

**Architecture:** Chronicler owns only dependency-free hash primitives and an `IRecordable` traversal backend. FixedMathSharp owns fixed-point, vector, matrix, quaternion, transform, and geometry hash writer extensions in a new package that depends on released `Chronicler.Core`. Gravitas keeps domain-specific replay policy, ordering, and inclusion modes, but migrates the generic writer/hash value logic to the lower-stack APIs after package releases.

**Tech Stack:** .NET `netstandard2.1`/`net8.0`, `Chronicler.Core`, `IRecordable`, `IChronicler`, xUnit, FixedMathSharp companion package, downstream LSF package release flow.

---

**Date:** 2026-06-26  
**Status:** Planned  
**Primary Repository:** `F:\gamedevrepos\Chronicler`  
**Related Repositories:** `F:\gamedevrepos\FixedMathSharp`, `F:\gamedevrepos\SwiftCollections`, `F:\gamedevrepos\GridForge`, `F:\gamedevrepos\Gravitas`

## Purpose

Gravitas now has a deterministic replay hash harness with useful generic pieces:

- a fixed-width deterministic hash value.
- a small allocation-free hash writer.
- explicit primitive writes with stable byte order.
- section markers for schema/version boundaries.
- a natural opportunity to traverse `IRecordable.RecordData(...)` without serializing to JSON or MemoryPack first.

Those pieces should not stay Gravitas-specific. They are useful to upstream and sibling LSF libraries such as Trailblazer, future deterministic animation packages, and any conformance test harness that wants stable runtime-state hashes.

The extraction should happen in package order:

1. Implement and release dependency-free Chronicler hash infrastructure.
2. Create and release a new FixedMathSharp companion package that depends on released Chronicler.
3. Bump downstream dependencies in SwiftCollections and GridForge as needed.
4. Migrate Gravitas replay hashing to the released lower-stack APIs.

## Non-Goals

- Do not make `Chronicler.Core` depend on FixedMathSharp, SwiftCollections, GridForge, Gravitas, MemoryPack-only APIs, or any engine package.
- Do not hash JSON, MemoryPack, or another transport payload as the canonical replay signal. The hash should come from the deterministic record pass itself.
- Do not add reflection-heavy schema discovery or automatic object construction.
- Do not replace Gravitas-specific replay inclusion policy. Gravitas still owns which bodies, colliders, pairs, caches, and diagnostics are included for a given replay hash mode.
- Do not keep duplicate public APIs when the lower-stack API cleanly replaces them. This is a pre-public-release hardening pass; clean names beat compatibility adapters.

## Design Rules

- Hash writes use explicit little-endian primitive byte order.
- Hash sections include stable ASCII tags and integer schema versions.
- The writer never uses `object.GetHashCode()`, runtime type handle hashes, unordered dictionary iteration, current culture formatting, wall-clock state, or platform-dependent layout.
- The steady-state writer path should be allocation-free after normal JIT/runtime warmup.
- Unsupported `RecordValues.Look<T>(...)` leaf types fail with a clear `NotSupportedException` instead of silently hashing unstable data.
- `IRecordable.RecordData(...)` order is the schema order. The hash backend should preserve that order rather than sorting fields.
- `RecordLinks` should hash stable link IDs and slot names, not object identity.
- Release and ReleaseLean builds must expose the same hash API shape.

## Proposed Chronicler API Shape

Names may be revised during implementation if repo review finds a clearer fit, but the intended shape is:

```csharp
namespace Chronicler;

public readonly struct ChronicleHash : IEquatable<ChronicleHash>
{
    public ChronicleHash(ulong low, ulong high);
    public ulong Low { get; }
    public ulong High { get; }
    public override string ToString();
}
```

```csharp
namespace Chronicler;

public struct ChronicleHashWriter
{
    public ChronicleHashWriter();
    public void WriteSection(string tag, int version);
    public void WriteBool(bool value);
    public void WriteByte(byte value);
    public void WriteSByte(sbyte value);
    public void WriteInt16(short value);
    public void WriteUInt16(ushort value);
    public void WriteInt32(int value);
    public void WriteUInt32(uint value);
    public void WriteInt64(long value);
    public void WriteUInt64(ulong value);
    public void WriteChar(char value);
    public void WriteString(string? value);
    public void WriteEnum<TEnum>(TEnum value) where TEnum : struct, Enum;
    public ChronicleHash ToHash();
}
```

```csharp
namespace Chronicler;

public static class ChronicleHashSerializer
{
    public static ChronicleHash Compute(IRecordable target);
    public static ChronicleHash Compute(IRecordable target, ChronicleContext context);
    public static void Contribute(IRecordable target, ref ChronicleHashWriter writer);
    public static void Contribute(IRecordable target, ChronicleContext context, ref ChronicleHashWriter writer);
}
```

The serializer name should stay transport-neutral. It is a Chronicler record pass that produces a hash, not a JSON/MemoryPack transport.

## Proposed FixedMathSharp Package Shape

Create a new package in FixedMathSharp rather than adding a Chronicler dependency to the core math package:

- Project: `src/FixedMathSharp.Chronicler/FixedMathSharp.Chronicler.csproj`
- Package ID: `FixedMathSharp.Chronicler`
- Namespace: `FixedMathSharp.Chronicler`
- Dependencies: released `FixedMathSharp`, released `Chronicler.Core`
- Target frameworks: match FixedMathSharp (`netstandard2.1;net8.0`)
- ReleaseLean behavior: compile without MemoryPack assumptions; this package should not need MemoryPack.

Initial API:

```csharp
namespace FixedMathSharp.Chronicler;

public static class FixedMathChronicleHashWriterExtensions
{
    public static void WriteFixed64(this ref ChronicleHashWriter writer, Fixed64 value);
    public static void WriteVector2d(this ref ChronicleHashWriter writer, Vector2d value);
    public static void WriteVector3d(this ref ChronicleHashWriter writer, Vector3d value);
    public static void WriteVector4d(this ref ChronicleHashWriter writer, Vector4d value);
    public static void WriteQuaternion(this ref ChronicleHashWriter writer, FixedQuaternion value);
    public static void WriteTransform(this ref ChronicleHashWriter writer, FixedTransform value);
    public static void WriteFixed3x3(this ref ChronicleHashWriter writer, Fixed3x3 value);
    public static void WriteFixed4x4(this ref ChronicleHashWriter writer, Fixed4x4 value);
    public static void WriteBoundBox(this ref ChronicleHashWriter writer, FixedBoundBox value);
    public static void WriteBoundArea(this ref ChronicleHashWriter writer, FixedBoundArea value);
    public static void WriteBoundingSphere(this ref ChronicleHashWriter writer, BoundingSphere value);
    public static void WriteRay(this ref ChronicleHashWriter writer, FixedRay value);
    public static void WritePlane(this ref ChronicleHashWriter writer, FixedPlane value);
}
```

The exact geometry list should be limited to FixedMathSharp primitives already used by downstream deterministic runtime state. The package can grow later through measured need.

## Workstream 1: Chronicler Hash Value And Primitive Writer

**Goal:** Move the generic fixed-width hash value and primitive writer concept into Chronicler without adding dependencies.

**Files:**

- Create: `src/Chronicler/Recording/ChronicleHash.cs`
- Create: `src/Chronicler/Recording/ChronicleHashWriter.cs`
- Create: `tests/Chronicler.Tests/Recording/ChronicleHashWriterTests.cs`
- Modify: `README.md`

**Tasks:**

- [ ] Add `ChronicleHash` as a public immutable 128-bit value with `Low`, `High`, equality, operators, deterministic `GetHashCode()`, and lowercase 32-character hex `ToString()`.
- [ ] Add `ChronicleHashWriter` as a public mutable struct with explicit primitive write methods.
- [ ] Port the current Gravitas lane algorithm only if review confirms it is adequate for Chronicler; otherwise choose a simple deterministic 128-bit non-cryptographic mixing algorithm with documented constants and stable output tests.
- [ ] Require ASCII-only section tags in `WriteSection(...)` and throw `ArgumentException` for null, empty, or non-ASCII tags.
- [ ] Encode strings as nullable marker plus length-prefixed UTF-16 code units or UTF-8 bytes. Pick one canonical encoding and lock it with tests.
- [ ] Add tests proving primitive writes are little-endian and order-sensitive.
- [ ] Add tests proving equal write streams produce equal hashes and different write streams produce different hashes.
- [ ] Add tests for section tag validation and stable `ToString()` format.
- [ ] Add an allocation assertion for a warmed primitive writer path.
- [ ] Document the writer as deterministic record-hash infrastructure, not a security or cryptographic hash.

**Validation:**

```bash
dotnet test tests/Chronicler.Tests/Chronicler.Tests.csproj -c Release --filter ChronicleHashWriter
```

## Workstream 2: Chronicler `IRecordable` Hash Backend

**Goal:** Add a hash backend that can run an existing `RecordData(IChronicler)` schema and produce a deterministic `ChronicleHash`.

**Files:**

- Create: `src/Chronicler/Recording/ChronicleHashSerializer.cs`
- Create: `src/Chronicler/Recording/ChronicleHashChronicler.cs`
- Create: `tests/Chronicler.Tests/Recording/ChronicleHashSerializerTests.cs`
- Modify: `README.md`

**Tasks:**

- [ ] Add `ChronicleHashSerializer.Compute(IRecordable target)` and `Compute(IRecordable target, ChronicleContext context)`.
- [ ] Add `ChronicleHashSerializer.Contribute(...)` overloads for callers that own a larger domain writer and want to embed a recordable subtree.
- [ ] Implement `ChronicleHashChronicler : IChronicler` with `SerializationMode.Saving`.
- [ ] For `LookValue<T>`, include the field name, value type category, null marker, and canonical leaf value.
- [ ] Support leaf values for `bool`, `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `char`, `string`, and enums with 1/2/4/8-byte underlying values.
- [ ] Decide float and double policy explicitly during implementation. If supported, hash IEEE bit patterns only. If rejected, throw `NotSupportedException` with a message that names the field and type.
- [ ] For `LookDeep<T>`, include the field name, null marker, runtime-independent declared type name, and nested `RecordData(...)` payload. Null class deep values should hash as null and should not instantiate objects.
- [ ] For `LookDeepStruct<T>`, include the field name and nested `RecordData(...)` payload.
- [ ] For `LookNullableDeep<T>`, include the field name, presence marker, and nested payload only when present.
- [ ] For `LookLink<T>`, include the field name, optional slot, resolve mode, and stable link ID from `ChronicleContext.Links.TryGetReferenceId(...)`.
- [ ] Throw a clear exception when a non-null link value cannot be resolved to a stable ID.
- [ ] Add tests for equivalent JSON/MemoryPack save data producing equivalent hash when `RecordData(...)` writes the same fields in the same order.
- [ ] Add tests that changing field order, field name, nested value, nullable presence, or link ID changes the hash.
- [ ] Add tests that unsupported leaf values fail loudly.
- [ ] Add warmed allocation tests for a simple recordable and a nested recordable graph.

**Validation:**

```bash
dotnet test tests/Chronicler.Tests/Chronicler.Tests.csproj -c Release --filter ChronicleHashSerializer
```

## Workstream 3: Chronicler Docs, Lean Build, And Release Prep

**Goal:** Finish Chronicler as a releasable package before any downstream package depends on the new API.

**Files:**

- Modify: `README.md`
- Modify: `AGENTS.md` only if contributor guidance needs a short note about record hashes.
- Modify: `src/Chronicler/Chronicler.csproj` only if package metadata, XML docs, or conditional compilation needs an update.

**Tasks:**

- [ ] Add README examples showing primitive writer use and `IRecordable` hash computation.
- [ ] Document that record hashes are conformance/replay signals, not transport payloads and not cryptographic hashes.
- [ ] Verify the standard package exposes the hash APIs.
- [ ] Verify the lean package exposes the same hash APIs without MemoryPack dependency assumptions.
- [ ] Generate concise release notes for the Chronicler release after tests pass.

**Validation:**

```bash
dotnet build Chronicler.slnx -c Release
dotnet test tests/Chronicler.Tests/Chronicler.Tests.csproj -c Release --no-build
dotnet build Chronicler.slnx -c ReleaseLean
dotnet test tests/Chronicler.Tests/Chronicler.Tests.csproj -c ReleaseLean --no-build
```

## Workstream 4: FixedMathSharp Companion Package

**Goal:** Add FixedMathSharp-specific hash writer extensions in a separate package after the Chronicler release is available.

**Repository:** `F:\gamedevrepos\FixedMathSharp`

**Files:**

- Create: `src/FixedMathSharp.Chronicler/FixedMathSharp.Chronicler.csproj`
- Create: `src/FixedMathSharp.Chronicler/FixedMathChronicleHashWriterExtensions.cs`
- Create: `tests/FixedMathSharp.Chronicler.Tests/FixedMathSharp.Chronicler.Tests.csproj`
- Create: `tests/FixedMathSharp.Chronicler.Tests/FixedMathChronicleHashWriterExtensionsTests.cs`
- Modify: `FixedMathSharp.slnx`
- Modify: `README.md`

**Tasks:**

- [ ] Create the companion project with package ID `FixedMathSharp.Chronicler`.
- [ ] Reference released `Chronicler.Core` by package version, not a local project reference, for release validation.
- [ ] Reference the core FixedMathSharp project or package according to the local release workflow being used.
- [ ] Add extension methods for `Fixed64`, vectors, quaternion, transform, matrices, and commonly used FixedMathSharp geometry primitives.
- [ ] Hash `Fixed64` by raw fixed-point value.
- [ ] Hash vectors, matrices, quaternion, transform, and geometry types by writing fields in documented canonical order.
- [ ] Keep extension methods allocation-free and avoid generic reflection.
- [ ] Add tests proving canonical field order for each supported type.
- [ ] Add tests proving two mathematically different values with close display formatting still hash differently when raw fixed values differ.
- [ ] Add tests proving geometry types hash only deterministic fields and do not use `GetHashCode()`.
- [ ] Update FixedMathSharp README with a short package section and usage example.

**Validation:**

```bash
dotnet build FixedMathSharp.slnx -c Release
dotnet test tests/FixedMathSharp.Chronicler.Tests/FixedMathSharp.Chronicler.Tests.csproj -c Release --no-build
dotnet build FixedMathSharp.slnx -c ReleaseLean
dotnet test tests/FixedMathSharp.Chronicler.Tests/FixedMathSharp.Chronicler.Tests.csproj -c ReleaseLean --no-build
```

## Workstream 5: Downstream Dependency Bumps

**Goal:** Move the stack forward in package order so Gravitas can consume released packages instead of long-lived local links.

**Repositories:**

- `F:\gamedevrepos\SwiftCollections`
- `F:\gamedevrepos\GridForge`
- `F:\gamedevrepos\Gravitas`

**Tasks:**

- [ ] Bump SwiftCollections dependencies to the released FixedMathSharp package set when required by package graph changes.
- [ ] Run SwiftCollections Release and ReleaseLean validation.
- [ ] Release SwiftCollections if a dependency migration package is needed.
- [ ] Bump GridForge dependencies to the released SwiftCollections and FixedMathSharp package set.
- [ ] Run GridForge Release and ReleaseLean validation.
- [ ] Release GridForge if a dependency migration package is needed.
- [ ] Bump Gravitas dependencies to the released Chronicler, FixedMathSharp, SwiftCollections, and GridForge package set.
- [ ] Use local project references only during active migration, and add the matching local link to child test and benchmark projects when required by the current .NET restore behavior.
- [ ] Remove local project references before Gravitas release validation unless the repo owner explicitly asks to leave them in place.

**Validation:**

```bash
dotnet build SwiftCollections.slnx -c Release
dotnet test SwiftCollections.slnx -c Release
dotnet build SwiftCollections.slnx -c ReleaseLean
dotnet test SwiftCollections.slnx -c ReleaseLean

dotnet build GridForge.slnx -c Release
dotnet test GridForge.slnx -c Release
dotnet build GridForge.slnx -c ReleaseLean
dotnet test GridForge.slnx -c ReleaseLean
```

## Workstream 6: Gravitas Replay Hash Migration

**Goal:** Replace Gravitas-local generic hash infrastructure with the released Chronicler and FixedMathSharp APIs while keeping Gravitas-specific replay policy and performance guarantees.

**Repository:** `F:\gamedevrepos\Gravitas`

**Files:**

- Modify or remove: `src/Gravitas/Determinism/GravitasReplayHash.cs`
- Modify or remove: `src/Gravitas/Determinism/GravitasReplayHashWriter.cs`
- Modify: `src/Gravitas/Determinism/GravitasReplayHashService.cs`
- Modify: all `ContributeReplayHash(...)` partials and helpers that currently use `GravitasReplayHashWriter`
- Modify: determinism tests under `tests/Gravitas.Tests/Determinism`
- Modify: `tests/Gravitas.Benchmarks/Core/ReplayHashBenchmarks.cs`
- Modify: `docs/wiki/SERIALIZATION.md`, `docs/wiki/HOST_INTEGRATION.md`, and benchmark docs if public API names change

**Tasks:**

- [ ] Replace the local replay hash value with `ChronicleHash` unless review finds a strong reason for a Gravitas domain wrapper.
- [ ] Replace the local replay hash writer with `ChronicleHashWriter` plus `FixedMathSharp.Chronicler` extension methods.
- [ ] Keep `GravitasReplayHashMode` in Gravitas because inclusion modes are physics-domain policy.
- [ ] Update `GravitasWorldContext.ComputeReplayHash(...)` return type if the clean public API is `ChronicleHash`.
- [ ] Remove duplicate local primitive/fixed-math writer methods after the migration.
- [ ] Use `ChronicleHashSerializer.Contribute(...)` for recordable subtrees only where it preserves the existing authoritative replay signal and allocation profile.
- [ ] Keep manual Gravitas contributors for service-owned ordered collections, solver caches, pair tables, partition identity, and dimensional inclusion policy.
- [ ] Run existing replay hash determinism tests and update expected type/name assertions.
- [ ] Add a regression test proving `IRecordable` hash contribution matches manual primitive contribution for a representative Gravitas state shell.
- [ ] Re-run replay hash allocation tests and benchmarks to prove the lower-stack writer does not regress steady-state allocation behavior.

**Validation:**

```bash
dotnet test tests/Gravitas.Tests/Gravitas.Tests.csproj --configuration Release --filter Determinism
dotnet test Gravitas.slnx --configuration Release
dotnet test Gravitas.slnx --configuration ReleaseLean
dotnet build tests/Gravitas.Benchmarks/Gravitas.Benchmarks.csproj -c Release -f net8.0
dotnet tests/Gravitas.Benchmarks/bin/Release/net8.0/Gravitas.Benchmarks.dll replay-hash --filter "*" -j Short -i
```

## Final Acceptance Criteria

- Chronicler exposes a stable, dependency-free deterministic record hash API.
- Chronicler can compute a hash by traversing `IRecordable.RecordData(...)` without serializing to JSON or MemoryPack first.
- FixedMathSharp exposes fixed-point and geometry hash writer extensions from a separate package that depends on Chronicler.
- SwiftCollections and GridForge dependency graphs are compatible with the new released package set.
- Gravitas no longer owns generic hash value/writer infrastructure.
- Gravitas replay hashing remains deterministic, allocation-conscious, and benchmarked.
- Docs in each touched repository describe only the public package behavior that exists after release.
