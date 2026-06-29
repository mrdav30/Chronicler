# MemoryPack Lean Source Shim Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` for independent review tasks, or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace copied per-library Lean MemoryPack shim files with a Chronicler-owned source-only shim package that compiles internal placeholder attributes into each consuming Lean assembly without exporting fake `MemoryPack` APIs.

**Architecture:** Chronicler owns a small source-only NuGet package that contributes `MemoryPack.*` compatibility attributes through `buildTransitive` assets only when `DisableMemoryPack=true`. Standard builds continue to reference the real MemoryPack package and must not compile the shim. Downstream LSF libraries consume the shim package in their Lean item groups, remove local shim files, and validate Release/ReleaseLean package graphs in stack order.

**Tech Stack:** .NET `netstandard2.1`/`net8.0`, MSBuild `buildTransitive` assets, source-only NuGet packaging, MemoryPack attribute compatibility, Chronicler, FixedMathSharp, SwiftCollections, GridForge, Gravitas.

---

**Date:** 2026-06-28  
**Status:** In Progress - Workstreams 1-4 complete; SwiftCollections,
GridForge, and Gravitas migrations remain pending after the shim package is
released.  
**Primary Repository:** `F:\gamedevrepos\Chronicler`  
**Related Repositories:** `F:\gamedevrepos\FixedMathSharp`, `F:\gamedevrepos\SwiftCollections`, `F:\gamedevrepos\GridForge`, `F:\gamedevrepos\Gravitas`

## Purpose

Each LSF library currently carries its own conditional `MemoryPack.Disable.Shim.cs`.
That works for simple package builds, but it creates repeated maintenance and
fragile local-link behavior when one Lean project builds another Lean project
through `ProjectReference`. A shared runtime DLL with public fake
`MemoryPack.*` attributes would avoid copied files, but it would also leak a
false MemoryPack API surface into consumers and risk conflicts with the real
MemoryPack package.

The right direction is a Chronicler-owned source-only package. The package
should compile internal placeholder attributes into the consuming assembly only
when the consuming project has opted out of MemoryPack. Because the placeholder
types are internal source inside each consumer, public LSF types can keep their
normal MemoryPack annotations in source without exporting fake MemoryPack
types from Lean assemblies.

## Non-Goals

- Do not add a runtime `MemoryPack` shim DLL.
- Do not make `Chronicler.Core.Lean` export public fake `MemoryPack.*` types.
- Do not add a `Chronicler.Core` dependency to FixedMathSharp just to acquire
  shim source.
- Do not require downstream libraries to wrap every MemoryPack annotation in
  `#if !*_DISABLE_MEMORYPACK`.
- Do not change JSON, MemoryPack, or Chronicler serialization semantics.
- Do not preserve duplicate local shim files once the shared source package is
  adopted by a library.

## Design Rules

- The shim package contributes source only; it does not ship a runtime assembly
  that consumers reference at runtime.
- The shim source is compiled only when `DisableMemoryPack=true`.
- The shim attributes are `internal sealed` and live in namespace `MemoryPack`
  so existing annotated source continues to compile in Lean builds.
- Standard builds must keep referencing real `MemoryPack` and must not compile
  the shim source.
- Lean builds must not reference real `MemoryPack`.
- The source package should support the current attribute set used across LSF:
  `MemoryPackableAttribute`, `MemoryPackIncludeAttribute`,
  `MemoryPackIgnoreAttribute`, `MemoryPackConstructorAttribute`,
  `MemoryPackAllowSerializeAttribute`, and `MemoryPackOrderAttribute`.
- The package should be usable by FixedMathSharp without adding a dependency on
  `Chronicler.Core`.
- Local project references used during cross-repo migration must not leak parent
  `DefineConstants` into child projects.

## Proposed Package Shape

Create a source-only package from the Chronicler repository:

- Project: `src/Chronicler.MemoryPackShim/Chronicler.MemoryPackShim.csproj`
- Package ID: `Chronicler.MemoryPackShim`
- Runtime assemblies: none
- Package assets:
  - `buildTransitive/Chronicler.MemoryPackShim.targets`
  - `contentFiles/cs/any/MemoryPack.Disable.Shim.cs`
- Consumer reference shape:

```xml
<PropertyGroup>
  <ChroniclerMemoryPackShimVersion>0.3.0</ChroniclerMemoryPackShimVersion>
</PropertyGroup>

<ItemGroup Condition="'$(DisableMemoryPack)' == 'true'">
  <PackageReference Include="Chronicler.MemoryPackShim" Version="$(ChroniclerMemoryPackShimVersion)" PrivateAssets="all" />
</ItemGroup>
```

The package target should include the shim source only when the consumer has
opted into Lean behavior:

```xml
<Project>
  <ItemGroup Condition="'$(DisableMemoryPack)' == 'true' and '$(ChroniclerMemoryPackShimEnabled)' != 'false'">
    <Compile Include="$(MSBuildThisFileDirectory)..\contentFiles\cs\any\MemoryPack.Disable.Shim.cs"
             Link="Chronicler.Generated\MemoryPack.Disable.Shim.cs"
             Visible="false" />
  </ItemGroup>
</Project>
```

The source file should remain dependency-free:

```csharp
using System;

namespace MemoryPack
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    internal sealed class MemoryPackableAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    internal sealed class MemoryPackIncludeAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    internal sealed class MemoryPackIgnoreAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Constructor)]
    internal sealed class MemoryPackConstructorAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property)]
    internal sealed class MemoryPackAllowSerializeAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    internal sealed class MemoryPackOrderAttribute : Attribute
    {
        public MemoryPackOrderAttribute(ushort order)
        {
        }

        public MemoryPackOrderAttribute(int order)
        {
        }
    }
}
```

## Workstream 1: Chronicler Source-Only Shim Package

**Goal:** Add the package that owns the shared Lean MemoryPack compatibility
source without changing `Chronicler.Core` runtime behavior.

**Files:**

- Create: `src/Chronicler.MemoryPackShim/Chronicler.MemoryPackShim.csproj`
- Create: `src/Chronicler.MemoryPackShim/buildTransitive/Chronicler.MemoryPackShim.targets`
- Create: `src/Chronicler.MemoryPackShim/contentFiles/cs/any/MemoryPack.Disable.Shim.cs`
- Modify: `Chronicler.slnx`
- Modify: `README.md`

**Tasks:**

- [x] Create the `Chronicler.MemoryPackShim` project as a packaging-only
  project that produces a NuGet package and no runtime DLL.
- [x] Add package metadata that makes the package description explicit:
  source-only MemoryPack compatibility attributes for Lean builds.
- [x] Add `MemoryPack.Disable.Shim.cs` with the six internal attribute types
  listed in the proposed package shape.
- [x] Add `Chronicler.MemoryPackShim.targets` so the source is included only
  when `DisableMemoryPack=true` and `ChroniclerMemoryPackShimEnabled` is not
  `false`.
- [x] Add the project to `Chronicler.slnx`.
- [x] Document the package in `README.md` as a source-only Lean helper, not a
  serializer and not a runtime dependency.

**Validation:**

```bash
dotnet build Chronicler.slnx -c Release
dotnet build Chronicler.slnx -c ReleaseLean
```

## Workstream 2: Shim Consumer Fixtures

**Goal:** Prove the source package behaves correctly before migrating the
stack.

**Files:**

- Create: `tests/Chronicler.MemoryPackShim.Tests/Chronicler.MemoryPackShim.Tests.csproj`
- Create: `tests/Chronicler.MemoryPackShim.Tests/MemoryPackShimPackageTests.cs`
- Modify: `Chronicler.slnx`

**Tasks:**

- [x] Add tests that pack `Chronicler.MemoryPackShim` and create a temporary
  consumer project with `DisableMemoryPack=true`.
- [x] In the temporary consumer, compile a public annotated type:

  ```csharp
  using MemoryPack;

  [MemoryPackable]
  public partial class PublicAnnotatedLeanType
  {
      [MemoryPackInclude]
      public int Value;

      [MemoryPackIgnore]
      public int RuntimeOnly => Value + 1;
  }
  ```

- [x] Assert the Lean consumer compiles without a `MemoryPack` package
  reference.
- [x] Assert the Lean consumer output does not include a public
  `MemoryPack.MemoryPackableAttribute` type.
- [x] Add a standard consumer fixture with `DisableMemoryPack=false` that
  references real `MemoryPack` and proves the shim source is not compiled.
- [x] Add a fixture proving `ChroniclerMemoryPackShimEnabled=false` disables
  source injection for projects that intentionally provide their own shim.

**Validation:**

```bash
dotnet test tests/Chronicler.MemoryPackShim.Tests/Chronicler.MemoryPackShim.Tests.csproj -c Release
```

## Workstream 3: Chronicler Release Prep

**Goal:** Release the source-only shim before downstream libraries migrate.

**Files:**

- Modify: `README.md`
- Modify: `AGENTS.md` if contributor guidance needs a short note about the
  source-only shim package.
- Modify: `src/Chronicler.MemoryPackShim/Chronicler.MemoryPackShim.csproj` if
  package metadata changes during review.

**Tasks:**

- [x] Verify `Chronicler.Core` and `Chronicler.Core.Lean` package behavior is
  unchanged.
- [x] Verify `Chronicler.MemoryPackShim` package contents include only
  build/source assets and package metadata.
- [x] Generate release notes that call out the new source-only helper package.

**Validation:**

```bash
dotnet build Chronicler.slnx -c Release
dotnet test Chronicler.slnx -c Release
dotnet build Chronicler.slnx -c ReleaseLean
dotnet test Chronicler.slnx -c ReleaseLean
```

**Release Note Draft:**

- Added `Chronicler.MemoryPackShim`, a source-only Lean helper package that
  contributes internal `MemoryPack.*` compatibility attributes only when
  `DisableMemoryPack=true`.
- Added package fixture coverage proving Lean consumers compile annotated
  source without real MemoryPack, standard consumers keep using real MemoryPack,
  and the shim package ships no runtime assembly.

## Workstream 4: FixedMathSharp Migration

**Goal:** Remove FixedMathSharp's copied shim and consume the released
source-only package in Lean builds.

**Repository:** `F:\gamedevrepos\FixedMathSharp`

**Files:**

- Delete: `src/FixedMathSharp/Serialization/MemoryPack.Disable.Shim.cs`
- Modify: `Directory.Build.props`
- Modify: `src/FixedMathSharp/FixedMathSharp.csproj`
- Modify: `tests/FixedMathSharp.Tests/FixedMathSharp.Tests.csproj` if local
  test project references need explicit package flow.

**Tasks:**

- [x] Add `Chronicler.MemoryPackShim` to the Lean item group with
  `PrivateAssets="all"`.
- [x] Delete the local shim file.
- [x] Verify standard builds still reference real `MemoryPack`.
- [x] Verify Lean builds do not reference real `MemoryPack`.
- [x] Run the FixedMathSharp Release and ReleaseLean validation suites.

**Validation:**

```bash
dotnet build FixedMathSharp.slnx -c Release
dotnet test FixedMathSharp.slnx -c Release
dotnet build FixedMathSharp.slnx -c ReleaseLean
dotnet test FixedMathSharp.slnx -c ReleaseLean
```

**Implementation Notes:**

- `MemoryPackAllowSerializeAttribute` supports class, struct, field, and
  property targets. FixedMathSharp currently uses the attribute on field-backed
  deterministic data, so the shared shim needs that broader target set.
- FixedMathSharp now writes NuGet restore assets under configuration-specific
  project-extension paths. This prevents a standard `MemoryPack` restore graph
  from being reused by a `ReleaseLean --no-restore` build and producing a
  mislabeled Lean package.
- FixedMathSharp was validated before package release by packing
  `Chronicler.MemoryPackShim` locally to
  `F:\gamedevrepos\Chronicler\artifacts\local-packages` and passing that path
  through `RestoreSources` for Lean builds.
- FixedMathSharp's Lean package graph includes
  `Chronicler.MemoryPackShim` as a private build-time source helper, does not
  include real `MemoryPack`/`MemoryPack.Core`, and does not publish a runtime
  dependency on the shim package.

## Workstream 5: SwiftCollections Migration

**Goal:** Remove SwiftCollections' copied shim and consume the released
source-only package in Lean builds.

**Repository:** `F:\gamedevrepos\SwiftCollections`

**Files:**

- Delete: `src/SwiftCollections/Support/MemoryPack.Disable.Shim.cs`
- Modify: `src/SwiftCollections/SwiftCollections.csproj`
- Modify: `src/SwiftCollections.FixedMathSharp/SwiftCollections.FixedMathSharp.csproj` if package flow requires it.
- Modify: test project files if local project references require explicit
  Lean package flow during validation.

**Tasks:**

- [ ] Add `Chronicler.MemoryPackShim` to Lean item groups where annotated
  source compiles without real `MemoryPack`.
- [ ] Delete the local shim file.
- [ ] Keep `Chronicler.Core.Lean` references for actual Chronicler runtime APIs
  where they are already used.
- [ ] Verify standard builds still reference real `MemoryPack`.
- [ ] Verify Lean builds do not reference real `MemoryPack`.
- [ ] Run the SwiftCollections Release and ReleaseLean validation suites.

**Validation:**

```bash
dotnet build SwiftCollections.slnx -c Release
dotnet test SwiftCollections.slnx -c Release
dotnet build SwiftCollections.slnx -c ReleaseLean
dotnet test SwiftCollections.slnx -c ReleaseLean
```

## Workstream 6: GridForge Migration

**Goal:** Remove GridForge's copied shim and consume the released source-only
package in Lean builds.

**Repository:** `F:\gamedevrepos\GridForge`

**Files:**

- Delete: `src/GridForge/Support/MemoryPack.Disable.Shim.cs`
- Modify: `src/GridForge/GridForge.csproj`
- Modify: `tests/GridForge.Tests/GridForge.Tests.csproj` if local project
  references require explicit Lean package flow during validation.
- Modify: `tests/GridForge.Benchmarks/GridForge.Benchmarks.csproj` if local
  project references require explicit Lean package flow during validation.

**Tasks:**

- [ ] Add `Chronicler.MemoryPackShim` to the Lean item group with
  `PrivateAssets="all"`.
- [ ] Delete the local shim file.
- [ ] Keep MemoryPack annotations directly in source; do not wrap them in
  `#if !GRIDFORGE_DISABLE_MEMORYPACK`.
- [ ] Verify standard builds still reference real `MemoryPack`.
- [ ] Verify Lean builds do not reference real `MemoryPack`.
- [ ] Run the GridForge Release and ReleaseLean validation suites.

**Validation:**

```bash
dotnet build GridForge.slnx -c Release
dotnet test GridForge.slnx -c Release
dotnet build GridForge.slnx -c ReleaseLean
dotnet test GridForge.slnx -c ReleaseLean
```

## Workstream 7: Gravitas Migration And Local-Link Guardrails

**Goal:** Remove Gravitas's copied shim, consume the released source-only
package in Lean builds, and preserve clean local-link behavior during future
lower-stack work.

**Repository:** `F:\gamedevrepos\Gravitas`

**Files:**

- Delete: `src/Gravitas/Support/MemoryPack.Disable.Shim.cs`
- Modify: `src/Gravitas/Gravitas.csproj`
- Modify: `tests/Gravitas.Tests/Gravitas.Tests.csproj` when package flow or
  local project references require it.
- Modify: `tests/Gravitas.Benchmarks/Gravitas.Benchmarks.csproj` when package
  flow or local project references require it.
- Modify: `AGENTS.md` if local-link guidance has not already been added.

**Tasks:**

- [ ] Add `Chronicler.MemoryPackShim` to the Lean item group with
  `PrivateAssets="all"`.
- [ ] Delete the local shim file.
- [ ] Keep MemoryPack annotations directly in source; do not wrap them in
  `#if !GRAVITAS_DISABLE_MEMORYPACK`.
- [ ] When local project references are needed for sibling packages, add them
  to the main, test, and benchmark projects only as active validation scaffolding.
- [ ] For local project references, pass `DisableMemoryPack=$(DisableMemoryPack)`
  and remove parent `DefineConstants` from child builds so one library's Lean
  symbol does not leak into another library's compile.
- [ ] Leave local project references unstaged/uncommitted unless the repository
  owner explicitly asks to commit them.
- [ ] Remove local project references before package release validation unless
  the repository owner explicitly asks to keep them in place.
- [ ] Run the Gravitas Release and ReleaseLean validation suites.

**Validation:**

```bash
dotnet test tests/Gravitas.Tests/Gravitas.Tests.csproj --configuration Release
dotnet test tests/Gravitas.Tests/Gravitas.Tests.csproj --configuration ReleaseLean
dotnet build tests/Gravitas.Benchmarks/Gravitas.Benchmarks.csproj -c Release -f net8.0
```

## Final Acceptance Criteria

- `Chronicler.MemoryPackShim` ships as a source-only package with no runtime
  assembly dependency.
- Lean consumers compile public MemoryPack-annotated source without referencing
  real MemoryPack and without exporting fake public `MemoryPack.*` types.
- Standard consumers continue to use real MemoryPack.
- FixedMathSharp, SwiftCollections, GridForge, and Gravitas no longer carry
  copied `MemoryPack.Disable.Shim.cs` files after their migrations.
- Release and ReleaseLean validations pass in package order.
- Local project references remain temporary validation scaffolding rather than
  committed package graph changes unless explicitly requested.
