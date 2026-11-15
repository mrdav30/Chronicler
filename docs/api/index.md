---
title: Chronicler documentation
description: Guides and API reference for explicit deterministic state transfer and simulation timing in .NET.
---

<div class="chr-hero">
  <p class="chr-kicker">SIMULATION TOOLKIT FOR .NET</p>
  <h1>Save, resume, and verify your simulation</h1>
  <p>Define what goes into a snapshot and restore it into objects your
  application already owns. Advance an exact simulation clock, compare
  record hashes across runs, and route diagnostics to a sink you control.</p>
  <div class="chr-actions">
    <a href="guides/getting-started.md">Restore a snapshot</a>
    <a href="xref:Chronicler">Browse the API</a>
  </div>
</div>

<a id="one-schema-several-jobs"></a>
<a id="keep-deterministic-state-connected"></a>

## Guides

<div class="chr-card-grid">
  <a class="chr-card" href="guides/getting-started.md">
    <strong class="chr-card-title">Restore your first snapshot</strong>
    <p>Save a player with 72 health and restore it into an existing object
    while keeping its weapon instance.</p>
  </a>
  <a class="chr-card" href="guides/serialization-model.md">
    <strong class="chr-card-title">Choose what to record</strong>
    <p>Decide when to record a value, populate an owned object, or store
    a stable ID.</p>
  </a>
  <a class="chr-card" href="guides/record-hashes.md">
    <strong class="chr-card-title">Check a replay checkpoint</strong>
    <p>Compare the recorded state of two runtimes with deterministic
    record hashes.</p>
  </a>
  <a class="chr-card" href="guides/simulation-timing.md">
    <strong class="chr-card-title">Advance simulation time</strong>
    <p>Save and restore an exact clock across changes in step duration.</p>
  </a>
  <a class="chr-card" href="guides/diagnostics.md">
    <strong class="chr-card-title">Route diagnostics and guard inputs</strong>
    <p>Send warnings to a sink you control and reject out-of-range
    arguments.</p>
  </a>
</div>

## Package family

Both Core variants include the same dependency-free simulation timing APIs.

| Package                                                                                 | Includes                                                                     |
| --------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------- |
| [`Chronicler.Diagnostics`](https://www.nuget.org/packages/Chronicler.Diagnostics)       | Independent diagnostic channels, lazy logging, and exception guards          |
| [`Chronicler.Core`](https://www.nuget.org/packages/Chronicler.Core)                     | Core recording APIs plus JSON and MemoryPack transports                      |
| [`Chronicler.Core.Lean`](https://www.nuget.org/packages/Chronicler.Core.Lean)           | Core recording APIs and JSON, without the MemoryPack dependency or transport |
| [`Chronicler.MemoryPackShim`](https://www.nuget.org/packages/Chronicler.MemoryPackShim) | Attributes for Lean assemblies with MemoryPack metadata                      |

## Part of the LSF ecosystem

Chronicler is designed for deterministic libraries, games, simulations, and
tools throughout the Lockstep Simulation Framework ecosystem:

- [FixedMathSharp](https://github.com/mrdav30/FixedMathSharp)
- [SwiftCollections](https://github.com/mrdav30/SwiftCollections)
- [GridForge](https://github.com/mrdav30/GridForge)
- [Gravitas](https://github.com/mrdav30/Gravitas)
- [Trailblazer](https://github.com/mrdav30/Trailblazer)

## Resources

- [Migration guide](https://github.com/mrdav30/Chronicler/blob/main/docs/MIGRATION.md)
  covers upgrade steps and compatibility changes.
- [Source, issues, and releases](https://github.com/mrdav30/Chronicler)
- [NuGet packages](https://www.nuget.org/packages/Chronicler.Core)
- [Test coverage](https://mrdav30.github.io/Chronicler/coverage/)

The API reference is generated from the Core, Diagnostics, and compatibility
shim assemblies and their XML documentation. The guides explain how those types
work together.
