# Chronicler Agent Instructions

## Purpose

Chronicler is a small, focused serialization library for deterministic state
transfer.

Its primary use cases are:

- lockstep simulation
- snapshot and restore workflows
- loading serialized state into existing runtime object graphs
- stable reference restoration for runtime-owned or external objects

This project is intentionally narrow in scope. Favor clarity, determinism, and
explicit contracts over convenience features.

## Project Snapshot

- Standard package ID: `Chronicler.Core`
- Lean package ID: `Chronicler.Core.Lean`
- Compatibility package ID: `Chronicler.MemoryPackShim`
- Assembly name: `Chronicler`
- Root namespace: `Chronicler`
- Target frameworks: `netstandard2.1`, `net8.0`
- Standard transports: `System.Text.Json`, `MemoryPack`
- Lean transport: `System.Text.Json`; MemoryPack-specific source is compiled out
- Nullable reference types: enabled
- Release symbols: portable PDBs

Important: keep the public namespace as `Chronicler`. Even though files are
organized into folders, avoid introducing sub-namespaces unless there is an
explicit request to do so.

## Start Here

Read these in order before making non-trivial changes:

1. [`README.md`](README.md) for package orientation and the public entry path.
2. [`src/Chronicler/Chronicler.csproj`](src/Chronicler/Chronicler.csproj) and
   [`src/Chronicler.MemoryPackShim/Chronicler.MemoryPackShim.csproj`](src/Chronicler.MemoryPackShim/Chronicler.MemoryPackShim.csproj)
   for target frameworks, package variants, and dependency boundaries.
3. The relevant source area under [`src/Chronicler`](src/Chronicler).
4. The matching tests under [`tests/Chronicler.Tests`](tests/Chronicler.Tests)
   and, for shim work,
   [`tests/Chronicler.MemoryPackShim.Tests`](tests/Chronicler.MemoryPackShim.Tests).
5. [`docs/api/guides/getting-started.md`](docs/api/guides/getting-started.md)
   and
   [`docs/api/guides/serialization-model.md`](docs/api/guides/serialization-model.md)
   for the user-facing state model. Read
   [`docs/api/guides/record-hashes.md`](docs/api/guides/record-hashes.md) when
   changing hash behavior.
6. [`docs/complexity-exceptions.md`](docs/complexity-exceptions.md) before
   restructuring deterministic paths only to satisfy a metric.
7. Completed design records under
   [`docs/feature-work/done`](docs/feature-work/done) when a task touches record
   hashes or the Lean compatibility shim.

## Source Of Truth

When code, README text, generated docs, and workflow scaffolding disagree,
prefer source, project files, tests, and workflows. Keep these aligned whenever
public behavior, package shape, serialization contracts, or developer workflow
changes:

- [`README.md`](README.md), which stays concise, product-focused, and safe for
  NuGet rendering;
- [`AGENTS.md`](AGENTS.md) and [`CONTRIBUTING.md`](CONTRIBUTING.md);
- [`docs/api`](docs/api), which owns conceptual guides, DocFX configuration,
  namespace overrides, logo, repository link, and custom theme;
- [`src/Chronicler`](src/Chronicler) and
  [`src/Chronicler.MemoryPackShim`](src/Chronicler.MemoryPackShim);
- both test projects under [`tests`](tests);
- relevant workflows under [`.github/workflows`](.github/workflows).

Chronicler does not publish a GitHub Wiki. Keep conceptual documentation in the
DocFX site under [`docs/api/guides`](docs/api/guides).

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

Do not drift toward reflection-heavy, attribute-driven, or auto-discovery-based
serialization behavior unless that is a deliberate product decision.

### 2. Determinism is the priority

This library exists for deterministic flows. Changes should preserve:

- stable field naming
- explicit ordering through `RecordData(IChronicler chronicler)`
- canonical default behavior when entries are omitted
- equivalent behavior across supported transports

Avoid features that depend on ambient runtime state, unordered iteration, or
serializer-specific quirks.

### 3. Chronicler populates an existing runtime shell

Chronicler is not an arbitrary object graph constructor. The intended model is:

- the host/framework owns object creation
- Chronicler owns state transfer
- nested owned objects already exist when deep-loading into them, unless the
  specific API explicitly models optional/null cases

Do not add APIs that quietly turn Chronicler into a general-purpose object
materializer without an explicit product decision.

### 4. Links are for runtime-owned or external references

Use `RecordLinks` and the session-scoped `ChronicleContext` /
`ChronicleLinkRegistry` for references that should not be serialized inline.

Keep the distinction clear:

- values: leaf data
- deep: owned nested state
- links: externally owned identities/references

## Repository Layout

- `src/Chronicler/Abstractions` Core interfaces such as `IRecordable` and
  `IChronicler`.
- `src/Chronicler/Context` Session context and shared state for a serialization
  pass.
- `src/Chronicler/Links` Stable link recording, resolution, and registry
  support.
- `src/Chronicler/Recording` High-level recording helpers and serialization mode
  concepts.
- `src/Chronicler/Serialization` Shared serialization infrastructure.
- `src/Chronicler/Serialization/Json` JSON transport implementation.
- `src/Chronicler/Serialization/MemoryPack` MemoryPack transport implementation.
- `src/Chronicler.MemoryPackShim` Public MemoryPack compatibility attributes for
  annotated Lean assemblies. It is not a transport or serializer.
- `tests/Chronicler.Tests` Transport-parity and behavior-focused tests grouped
  by feature area.
- `tests/Chronicler.MemoryPackShim.Tests` Package-boundary and compatibility
  tests for the shim.
- `docs/api` DocFX configuration, branded landing page, conceptual guides,
  namespace overrides, and theme. Generated output under `docs/api/obj` is
  ignored.

Keep the library structure simple. Prefer adding to these buckets over creating
many new conceptual layers.

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
- Treat `ChronicleHash` and `ChronicleHashSerializer` as deterministic
  replay/conformance signals over `RecordData(...)`, not as transport payloads
  or cryptographic hashes.

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
dotnet build Chronicler.slnx -c ReleaseLean
dotnet test tests/Chronicler.Tests/Chronicler.Tests.csproj -c ReleaseLean --no-build
dotnet test tests/Chronicler.Tests/Chronicler.Tests.csproj -c Release --collect:"XPlat Code Coverage" --settings tests/Chronicler.Tests/coverlet.runsettings
dotnet tool restore
dotnet tool run docfx docs/api/docfx.json --warningsAsErrors
```

If packaging or publishing changes are involved, also verify:

- NuGet package metadata is still correct
- standard, Lean, and shim package IDs remain correct
- Release builds emit portable PDBs

CI runs Release and ReleaseLean builds and tests on Linux and Windows through
`.github/workflows/build-and-test.yml`. After a successful push to `main`, the
coverage workflow builds the DocFX site and coverage report from the exact
tested commit, validates generated assets and local links, and deploys them as
one GitHub Pages artifact with coverage under `/coverage/`.

## README And Packaging Alignment

Keep these aligned whenever public behavior changes:

- `README.md`
- `AGENTS.md`
- `CONTRIBUTING.md`
- `docs/api`
- `src/Chronicler/Chronicler.csproj`
- `src/Chronicler.MemoryPackShim/Chronicler.MemoryPackShim.csproj`
- tests that demonstrate the intended behavior

The README should describe the library as a standalone package, not as a future
extraction from another framework. Keep it focused on what the library is, why
to use it, installation, one small accurate example, and routes into the guides
and API reference. Put schema detail and advanced workflows in
`docs/api/guides`.

## Safe Change Heuristics For Agents

Good changes:

- improving determinism or transport parity
- tightening docs and package metadata
- expanding targeted tests
- clarifying guard behavior
- reorganizing files without changing the public namespace
- improving the DocFX guides, API landing page, or generated-site guardrails

Changes that need extra caution:

- changing serialized field names
- changing default value semantics
- changing link resolution behavior
- introducing new dependencies
- expanding the library toward automatic object construction
- introducing namespace sprawl

When in doubt, bias toward preserving the current explicit model:

`RecordData` defines the schema, the host owns construction, and Chronicler
transfers state deterministically.

Do not stage, commit, tag, push, publish packages, or create releases unless the
user explicitly requests it. Preserve unrelated dirty files and keep the diff
scoped to the current task.
