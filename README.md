# Chronicler

![Chronicler Icon](https://raw.githubusercontent.com/mrdav30/Chronicler/main/icon.png)

**Deterministic state transfer for .NET runtimes that own their objects and
their schemas.**

[![build-and-test](https://github.com/mrdav30/Chronicler/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/mrdav30/Chronicler/actions/workflows/build-and-test.yml)
[![Branch Coverage](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fmrdav30.github.io%2FChronicler%2Fcoverage%2FSummary.json&query=%24.summary.branchcoverage&suffix=%25&label=branch%20coverage&color=brightgreen)](https://mrdav30.github.io/Chronicler/coverage/)
[![NuGet](https://img.shields.io/nuget/v/Chronicler.Core.svg)](https://www.nuget.org/packages/Chronicler.Core)
[![License](https://img.shields.io/github/license/mrdav30/Chronicler.svg)](https://github.com/mrdav30/Chronicler/blob/main/LICENSE)
[![API](https://img.shields.io/badge/docs-API-f4511e)](https://mrdav30.github.io/Chronicler/)
[![Discord](https://img.shields.io/badge/discord-join%20community-5865F2?logo=discord&logoColor=white)](https://discord.gg/mhwK2QFNBA)

Chronicler lets a type declare its state once, then use that same explicit
schema for JSON, MemoryPack, restore workflows, stable runtime links, and
deterministic record hashes. Your host constructs the object graph; Chronicler
transfers state into it without taking ownership of your runtime.

## Why Chronicler?

- **Schemas stay in your code.** `RecordData(...)` makes names, defaults, order,
  and ownership visible and reviewable.
- **Restore into live objects.** Populate initialized runtime shells instead of
  asking a serializer to invent your graph.
- **Keep references stable.** Record external or runtime-owned objects by IDs
  through a session-scoped registry.
- **Compare deterministic state.** Compute replay and conformance signals from
  the recording schema without hashing a transport payload.
- **Choose your dependency surface.** Use JSON and MemoryPack together, or take
  the Lean package when the built-in MemoryPack transport is unnecessary.

## Install

```bash
dotnet add package Chronicler.Core
```

`Chronicler.Core` targets `netstandard2.1` and `net8.0` and includes the JSON
and MemoryPack transports.

## Quick start

```csharp
using Chronicler;

public sealed class PlayerSnapshot : IRecordable
{
    public int Health = 100;
    public WeaponSnapshot Weapon = new();

    public void RecordData(IChronicler chronicler)
    {
        RecordValues.Look(chronicler, ref Health, "health", 100);
        RecordDeep.Look(chronicler, ref Weapon, "weapon");
    }
}

public sealed class WeaponSnapshot : IRecordable
{
    public int Ammo = 30;

    public void RecordData(IChronicler chronicler)
    {
        RecordValues.Look(chronicler, ref Ammo, "ammo", 30);
    }
}

PlayerSnapshot source = new() { Health = 72 };
string json = JsonRecordSerializer.Serialize(source, writeIndented: true);

PlayerSnapshot restored = new(); // The host creates and initializes the shell.
JsonRecordSerializer.Populate(restored, json);
```

The same `RecordData(...)` implementation works with
`MemoryPackRecordSerializer` in the standard package.

## Choose a package

| Package                                                                                 | Use it when                                                                                                                                 |
| --------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------- |
| [`Chronicler.Core`](https://www.nuget.org/packages/Chronicler.Core)                     | You want the core recording model plus built-in JSON and MemoryPack transports.                                                             |
| [`Chronicler.Core.Lean`](https://www.nuget.org/packages/Chronicler.Core.Lean)           | You want the core recording model and JSON without the MemoryPack dependency or built-in MemoryPack transport.                              |
| [`Chronicler.MemoryPackShim`](https://www.nuget.org/packages/Chronicler.MemoryPackShim) | A Lean library keeps MemoryPack annotations in its public metadata. This package supplies compatibility attributes; it is not a serializer. |

## Learn more

- [Getting started](https://mrdav30.github.io/Chronicler/guides/getting-started.html)
- [Understand values, owned state, and links](https://mrdav30.github.io/Chronicler/guides/serialization-model.html)
- [Use deterministic record hashes](https://mrdav30.github.io/Chronicler/guides/record-hashes.html)
- [Browse the API reference](https://mrdav30.github.io/Chronicler/api/Chronicler.html)
- [View test coverage](https://mrdav30.github.io/Chronicler/coverage/)

## Development

```bash
dotnet build Chronicler.slnx --configuration Release
dotnet test tests/Chronicler.Tests/Chronicler.Tests.csproj --configuration Release --no-build
```

See the
[contributor guide](https://github.com/mrdav30/Chronicler/blob/main/CONTRIBUTING.md)
for the full workflow.

## Community and license

Open an [issue](https://github.com/mrdav30/Chronicler/issues) for bugs and
feature requests, or join the
[LSF Discord community](https://discord.gg/mhwK2QFNBA).

Chronicler is available under the
[MIT License](https://github.com/mrdav30/Chronicler/blob/main/LICENSE). See the
[notice](https://github.com/mrdav30/Chronicler/blob/main/NOTICE) for the
repository's branding and redistribution terms.
