# AGENTS

## Purpose

Chronicler is a small, focused serialization library for deterministic state transfer.

Its primary use cases are:

- lockstep simulation
- snapshot and restore workflows
- loading serialized state into existing runtime object graphs
- stable reference restoration for runtime-owned or external objects

This project is intentionally narrow in scope. Favor clarity, determinism, and explicit contracts over convenience features.

## Project Snapshot

- Package ID: `Chronicler.Core`
- Assembly name: `Chronicler`
- Root namespace: `Chronicler`
- Target frameworks: `netstandard2.1`, `net8.0`
- Built-in transports: `System.Text.Json`, `MemoryPack`
- Nullable reference types: enabled
- Release symbols: portable PDBs

Important: keep the public namespace as `Chronicler`. Even though files are organized into folders, avoid introducing sub-namespaces unless there is an explicit request to do so.

## Core Design Principles

### 1. Types own their serialized shape

Serialization is explicit and code-driven through:

- `IRecordable`
- `IChronicler`
- `RecordValues`
- `RecordDeep`
- `RecordDeepStruct`
- `RecordNullableDeep`
- `RecordLinks`

Do not drift toward reflection-heavy, attribute-driven, or auto-discovery-based serialization behavior unless that is a deliberate product decision.

### 2. Determinism is the priority

This library exists for deterministic flows. Changes should preserve:

- stable field naming
- explicit ordering through `RecordData(IChronicler chronicler)`
- canonical default behavior when entries are omitted
- equivalent behavior across supported transports

Avoid features that depend on ambient runtime state, unordered iteration, or serializer-specific quirks.

### 3. Chronicler populates an existing runtime shell

Chronicler is not an arbitrary object graph constructor. The intended model is:

- the host/framework owns object creation
- Chronicler owns state transfer
- nested owned objects already exist when deep-loading into them, unless the specific API explicitly models optional/null cases

Do not add APIs that quietly turn Chronicler into a general-purpose object materializer without an explicit product decision.

### 4. Links are for runtime-owned or external references

Use `RecordLinks` and the session-scoped `ChronicleContext` / `ChronicleLinkRegistry` for references that should not be serialized inline.

Keep the distinction clear:

- values: leaf data
- deep: owned nested state
- links: externally owned identities/references

## Repository Layout

- `src/Chronicler/Abstractions`
  Core interfaces such as `IRecordable` and `IChronicler`.
- `src/Chronicler/Context`
  Session context and shared state for a serialization pass.
- `src/Chronicler/Links`
  Stable link recording, resolution, and registry support.
- `src/Chronicler/Recording`
  High-level recording helpers and serialization mode concepts.
- `src/Chronicler/Serialization`
  Shared serialization infrastructure.
- `src/Chronicler/Serialization/Json`
  JSON transport implementation.
- `src/Chronicler/Serialization/MemoryPack`
  MemoryPack transport implementation.
- `tests/Chronicler.Tests`
  Transport-parity and behavior-focused tests grouped by feature area.

Keep the library structure simple. Prefer adding to these buckets over creating many new conceptual layers.

## Coding Standards

### Public API guidance

- Keep APIs small, explicit, and transport-neutral.
- Prefer semantic clarity over cleverness.
- Public XML documentation should stay current.
- Preserve nullable intent rather than removing annotations to silence warnings.

### Behavioral guidance

- Pass canonical declared defaults to `RecordValues.Look(...)`.
- Keep JSON and MemoryPack behavior aligned.
- Prefer explicit guard clauses for invalid inputs and unsupported states.
- Preserve deterministic load/save semantics when adding new helpers.
- Do not hide important behavior in magic defaults.

### Dependency guidance

- Be conservative about adding dependencies.
- Avoid pulling in framework-specific concepts into the core library.
- Keep the library reusable by other projects higher in the stack.

### Style guidance

- Follow the existing simple, direct C# style.
- Avoid unnecessary abstraction layers in a library this small.
- Keep comments and docs useful, not verbose.

## Testing Expectations

Every meaningful behavior change should come with tests.

When possible:

- test both JSON and MemoryPack paths
- cover save and load behavior
- cover default/omission semantics
- cover failure and guard paths
- cover link resolution behavior when touching `RecordLinks` or registry code

Current quality bar:

- aim to preserve near-100% coverage on authored code
- avoid introducing high-CRAP methods
- treat CRAP scores above `30` as a smell that needs attention

## Verification Commands

Use these as the normal validation loop:

```bash
dotnet build Chronicler.slnx -c Release
dotnet test tests/Chronicler.Tests/Chronicler.Tests.csproj -c Release --no-build
dotnet test tests/Chronicler.Tests/Chronicler.Tests.csproj -c Release --collect:"XPlat Code Coverage" --settings coverlet.runsettings
```

If packaging or publishing changes are involved, also verify:

- NuGet package metadata is still correct
- package ID remains `Chronicler.Core`
- Release builds emit portable PDBs

## README And Packaging Alignment

Keep these aligned whenever public behavior changes:

- `README.md`
- `src/Chronicler/Chronicler.csproj`
- tests that demonstrate the intended behavior

The README should describe the library as a standalone package, not as a future extraction from another framework.

## Safe Change Heuristics For Agents

Good changes:

- improving determinism or transport parity
- tightening docs and package metadata
- expanding targeted tests
- clarifying guard behavior
- reorganizing files without changing the public namespace

Changes that need extra caution:

- changing serialized field names
- changing default value semantics
- changing link resolution behavior
- introducing new dependencies
- expanding the library toward automatic object construction
- introducing namespace sprawl

When in doubt, bias toward preserving the current explicit model:

`RecordData` defines the schema, the host owns construction, and Chronicler transfers state deterministically.
