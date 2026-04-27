# Chronicler

![Chronicler Icon](https://raw.githubusercontent.com/mrdav30/Chronicler/main/icon.png)

[![.NET CI](https://github.com/mrdav30/Chronicler/actions/workflows/dotnet.yml/badge.svg)](https://github.com/mrdav30/Chronicler/actions/workflows/dotnet.yml)
[![Coverage](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fmrdav30.github.io%2FChronicler%2FSummary.json&query=%24.summary.linecoverage&suffix=%25&label=coverage&color=brightgreen)](https://mrdav30.github.io/Chronicler/)
[![NuGet](https://img.shields.io/nuget/v/Chronicler.Core.svg)](https://www.nuget.org/packages/Chronicler.Core)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Chronicler.Core.svg)](https://www.nuget.org/packages/Chronicler.Core)
[![License](https://img.shields.io/github/license/mrdav30/Chronicler.svg)](https://github.com/mrdav30/Chronicler/blob/main/LICENSE)
[![Frameworks](https://img.shields.io/badge/frameworks-netstandard2.1%20%7C%20net8.0-512BD4.svg)](https://github.com/mrdav30/Chronicler)

**Chronicler** is a deterministic, transport-neutral serialization library for lockstep simulation, snapshot/restore workflows, and runtime state transfer into existing object graphs.

Unlike attribute-only serializers, Chronicler makes each type explicitly own its serialized shape through `RecordData(IChronicler chronicler)`.

---

## Key Features

- **Deterministic record passes** for systems that care about stable, explicit state flow.
- **Transport-neutral core API** with built-in JSON and MemoryPack support.
- **Type-owned schemas** through `IRecordable`, without requiring serializer attributes on most domain types.
- **Canonical default handling** so omitted entries load into known values instead of ambient runtime state.
- **Deep graph support** for nested owned objects and structs already present in the runtime shell.
- **Stable link support** for runtime-owned or external references through a session-scoped registry.
- **Designed for lockstep and restore flows** where the host owns construction and Chronicler owns state transfer.

---

## Installation

### NuGet

```bash
dotnet add package Chronicler.Core
```

### Source

```bash
git clone https://github.com/mrdav30/Chronicler.git
```

Then reference `src/Chronicler/Chronicler.csproj` directly or build the package locally.

---

## API Overview

Chronicler is built around three primary recording lanes:

- **`RecordValues`** for leaf values such as primitives, enums, and small deterministic structs.
- **`RecordDeep` / `RecordDeepStruct` / `RecordNullableDeep`** for owned nested recordable state.
- **`RecordLinks`** for stable references to runtime-owned or external objects.

Core abstractions:

- `IRecordable`
- `IChronicler`
- `ChronicleContext`
- `ChronicleLinkRegistry`

Built-in transports:

- `JsonRecordSerializer`
- `MemoryPackRecordSerializer`

Important design note:

- Chronicler loads into an existing initialized runtime shell. It does not construct arbitrary object graphs from transport data alone.

---

## Quick Start

### Define a recordable type

```csharp
using Chronicler;

public sealed class PlayerSnapshot : IRecordable
{
    public int Health = 100;
    public int Mana = 50;
    public WeaponState Weapon = new();

    public void RecordData(IChronicler chronicler)
    {
        RecordValues.Look(chronicler, ref Health, "health", 100);
        RecordValues.Look(chronicler, ref Mana, "mana", 50);
        RecordDeep.Look(chronicler, ref Weapon, "weapon");
    }
}

public sealed class WeaponState : IRecordable
{
    public int Ammo = 30;

    public void RecordData(IChronicler chronicler)
    {
        RecordValues.Look(chronicler, ref Ammo, "ammo", 30);
    }
}
```

### Serialize and populate with JSON

```csharp
PlayerSnapshot source = new();
string json = JsonRecordSerializer.Serialize(source, writeIndented: true);

PlayerSnapshot target = new()
{
    Weapon = new WeaponState()
};

JsonRecordSerializer.Populate(target, json);
```

### Serialize and populate with MemoryPack

```csharp
PlayerSnapshot source = new();
byte[] data = MemoryPackRecordSerializer.Serialize(source);

PlayerSnapshot target = new()
{
    Weapon = new WeaponState()
};

MemoryPackRecordSerializer.Populate(target, data);
```

---

## Stable Links

Use `RecordLinks` when a field points to a runtime-owned or external object that should not be serialized inline.

```csharp
public sealed class ActorState : IRecordable
{
    public RuntimeEntity? Target;

    public void RecordData(IChronicler chronicler)
    {
        RuntimeEntity? target = Target;
        RecordLinks.Look(chronicler, ref target, "target");

        if (chronicler.Mode == SerializationMode.Loading)
            Target = target;
    }
}
```

Resolve those links through the session context:

```csharp
ChronicleContext context = new();
context.Links.RegisterInstance("player-42", runtimeEntity);
```

For cases where the target object is only available after the rest of the graph loads, use deferred link resolution with `RecordLinks.LookDeferred(...)`.

---

## Design Rules

- Pass canonical declared defaults to `RecordValues.Look(...)`.
- Use `RecordDeep` for owned nested objects that already exist in the runtime shell.
- Use `RecordDeepStruct` for non-nullable nested recordable structs.
- Use `RecordNullableDeep` for optional nested recordable structs.
- Use `RecordLinks` for runtime-owned or external references.
- Keep object construction and framework bootstrap outside the base Chronicler contract.

---

## Development

Build the solution:

```bash
dotnet build Chronicler.slnx -c Release
```

Run the unit tests:

```bash
dotnet test tests/Chronicler.Tests/Chronicler.Tests.csproj -c Release --no-build
```

Run tests with coverage:

```bash
dotnet test tests/Chronicler.Tests/Chronicler.Tests.csproj -c Release --collect:"XPlat Code Coverage" --settings coverlet.runsettings
```

---

## Compatibility

- `netstandard2.1`
- `net8.0`
- Windows, Linux, and macOS

JSON support is provided through `System.Text.Json`, and binary transport support is provided through `MemoryPack`.

---

## Contributing

We welcome contributions. Please see [CONTRIBUTING](https://github.com/mrdav30/Chronicler/blob/main/CONTRIBUTING.md) for contribution guidance and project expectations.

---

## Contributors

- **mrdav30** - Lead Developer
- Contributions are welcome through issues and pull requests.

---

## Community & Support

For bug reports or feature requests, please open an issue in this repository.

For general discussion and support, join the official Discord community:

👉 **[Join the Discord Server](https://discord.gg/mhwK2QFNBA)**

---

## License

This project is licensed under the MIT License.

See the following files for details:

- `LICENSE` - standard MIT license
- `NOTICE` - additional terms regarding project branding and redistribution
- `COPYRIGHT` - authorship information
