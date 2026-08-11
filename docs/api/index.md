---
title: Chronicler documentation
description: Guides and API reference for explicit deterministic state transfer in .NET.
---

<div class="chr-hero">
  <p class="chr-kicker">EXPLICIT STATE TRANSFER FOR .NET</p>
  <h1>Record state on your terms.</h1>
  <p>Chronicler gives each type one visible schema for JSON, MemoryPack,
  restoring existing runtime objects, stable links, and deterministic replay
  signals.</p>
  <div class="chr-actions">
    <a href="guides/getting-started.md">Get started</a>
    <a href="xref:Chronicler">Browse the API</a>
  </div>
</div>

## One schema, several jobs

<div class="chr-card-grid">
  <div class="chr-card">
    <h3><a href="xref:Chronicler.IRecordable">Own the schema</a></h3>
    <p>Make field names, defaults, order, and ownership explicit in
    <code>RecordData(...)</code>.</p>
  </div>
  <div class="chr-card">
    <h3><a href="xref:Chronicler.JsonRecordSerializer">Choose a transport</a></h3>
    <p>Use built-in JSON everywhere, with built-in MemoryPack available in the
    standard package.</p>
  </div>
  <div class="chr-card">
    <h3><a href="xref:Chronicler.RecordDeep">Populate your runtime</a></h3>
    <p>Transfer state into initialized objects while the host keeps ownership of
    construction and lifecycle.</p>
  </div>
</div>

## Keep deterministic state connected

<div class="chr-card-grid">
  <div class="chr-card">
    <h3><a href="xref:Chronicler.RecordLinks">Restore stable links</a></h3>
    <p>Represent runtime-owned and external references with stable IDs instead
    of serializing them inline.</p>
  </div>
  <div class="chr-card">
    <h3><a href="xref:Chronicler.ChronicleHashSerializer">Compare recorded state</a></h3>
    <p>Build deterministic replay and conformance signals directly from the
    recording schema.</p>
  </div>
  <div class="chr-card">
    <h3><a href="xref:Chronicler.IStateBacked`1">Expose canonical helper state</a></h3>
    <p>Integrate state-backed helper types with System.Text.Json when
    reconstruction from one state value is intentional.</p>
  </div>
</div>

## Package family

| Package                     | Includes                                                                     |
| --------------------------- | ---------------------------------------------------------------------------- |
| `Chronicler.Core`           | Core recording APIs plus JSON and MemoryPack transports                      |
| `Chronicler.Core.Lean`      | Core recording APIs and JSON, without the MemoryPack dependency or transport |
| `Chronicler.MemoryPackShim` | Compatibility attributes for annotated Lean assemblies; not a serializer     |

## Guides

- [Getting started](guides/getting-started.md) defines a schema and moves it
  through the built-in transports.
- [Values, owned state, and links](guides/serialization-model.md) explains
  object ownership, defaults, link resolution, and package boundaries.
- [Deterministic record hashes](guides/record-hashes.md) documents the replay
  and conformance hash contract.

## Part of the LSF ecosystem

Chronicler is designed for deterministic libraries, games, simulations, and
tools throughout the Lockstep Simulation Framework ecosystem:

- [FixedMathSharp](https://github.com/mrdav30/FixedMathSharp)
- [SwiftCollections](https://github.com/mrdav30/SwiftCollections)
- [GridForge](https://github.com/mrdav30/GridForge)
- [Gravitas](https://github.com/mrdav30/Gravitas)

## Resources

- [Source, issues, and releases](https://github.com/mrdav30/Chronicler)
- [NuGet packages](https://www.nuget.org/packages/Chronicler.Core)
- [Core test-suite coverage](https://mrdav30.github.io/Chronicler/coverage/)

The API reference is generated from the standard library and compatibility shim
XML documentation. The guides explain how those types work together in
task-oriented prose.
