---
title: Getting started
description: Define an explicit Chronicler schema and move state through JSON or MemoryPack.
---

# Getting started

Chronicler transfers state between an explicit recording schema and an object
graph your runtime already owns. A type implements
<xref:Chronicler.IRecordable>, then records its fields in a deliberate order
through `RecordData(...)`.

## 1. Install a package

The standard package is the best starting point:

```bash
dotnet add package Chronicler.Core
```

It targets `netstandard2.1` and `net8.0` and includes both built-in transports.
Choose `Chronicler.Core.Lean` when you want the same core recording model and
JSON support without the MemoryPack dependency or transport.

## 2. Define the schema

Use <xref:Chronicler.RecordValues> for leaf values and
<xref:Chronicler.RecordDeep> for an owned nested object:

```csharp
using Chronicler;

public sealed class PlayerSnapshot : IRecordable
{
    public int Health = 100;
    public int Mana = 50;
    public WeaponSnapshot Weapon = new();

    public void RecordData(IChronicler chronicler)
    {
        RecordValues.Look(chronicler, ref Health, "health", 100);
        RecordValues.Look(chronicler, ref Mana, "mana", 50);
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
```

The names, call order, and declared defaults are part of the schema. Keep them
stable unless you intend to change the serialized contract.

## 3. Save and restore JSON

<xref:Chronicler.JsonRecordSerializer.Serialize(Chronicler.IRecordable,System.Boolean)>
returns a JSON string. `Populate(...)` applies that state to an existing object:

```csharp
PlayerSnapshot source = new()
{
    Health = 72,
    Mana = 18,
    Weapon = new WeaponSnapshot { Ammo = 7 }
};

string json = JsonRecordSerializer.Serialize(source, writeIndented: true);

PlayerSnapshot restored = new();
JsonRecordSerializer.Populate(restored, json);
```

`restored.Weapon` already exists because the host constructed the runtime shell.
Chronicler populates that owned object; it does not act as a general-purpose
object graph factory.

## 4. Choose another transport when needed

The standard package exposes <xref:Chronicler.MemoryPackRecordSerializer> over
the same `RecordData(...)` schema:

```csharp
byte[] payload = MemoryPackRecordSerializer.Serialize(source);

PlayerSnapshot restored = new();
MemoryPackRecordSerializer.Populate(restored, payload);
```

The Lean package intentionally does not contain this type.

## 5. Use the right recording lane

| State kind                                      | Helper                               |
| ----------------------------------------------- | ------------------------------------ |
| Leaf data, enums, and small serializable values | <xref:Chronicler.RecordValues>       |
| Owned class state already present in the shell  | <xref:Chronicler.RecordDeep>         |
| Owned recordable struct state                   | <xref:Chronicler.RecordDeepStruct>   |
| Optional recordable struct state                | <xref:Chronicler.RecordNullableDeep> |
| Runtime-owned or external identity              | <xref:Chronicler.RecordLinks>        |

Always pass the canonical declared default to `RecordValues.Look(...)`. If an
entry is absent during loading, Chronicler applies that value instead of keeping
whatever happened to be in the target object.

## Next steps

- Read [Values, owned state, and links](serialization-model.md) for object
  ownership, stable IDs, deferred links, and state-backed JSON helpers.
- Read [Deterministic record hashes](record-hashes.md) for replay and
  conformance signals.
- Browse the <xref:Chronicler> API reference for every public type.
