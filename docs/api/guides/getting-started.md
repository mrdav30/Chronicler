---
title: Getting started
description: Define an explicit Chronicler schema and move state through JSON or MemoryPack.
---

# Getting started

Save a player's health to JSON, then restore it into a player your game has
already created. The saved player has 72 health and the default 30 rounds. After
loading, the target has those values and still owns the same weapon instance it
had before loading.

## 1. Install a package

Create a console app with the .NET 10 SDK and add the standard package.

```bash
dotnet new console -o ChroniclerQuickstart
cd ChroniclerQuickstart
dotnet add package Chronicler.Core
```

It targets `netstandard2.1` and `net10.0` and includes both built-in transports.
Choose `Chronicler.Core.Lean` when you want the same core recording model and
JSON support without the MemoryPack dependency or transport.

## 2. Define the schema

Implement <xref:Chronicler.IRecordable> on both types. Their `RecordData`
methods define the **schema**, the names, order, defaults, and helpers
Chronicler uses during both save and load. Keep these choices stable across
compatible versions.

Use <xref:Chronicler.RecordValues> for individual values such as `Health` and
`Ammo`. Use <xref:Chronicler.RecordDeep> for `Weapon`, an object owned and
initialized by `PlayerSnapshot`.

## 3. Save and restore JSON

Replace `Program.cs` with this example and run `dotnet run`.

```csharp
using System;
using Chronicler;
using Chronicler.Serialization;

PlayerSnapshot source = new() { Health = 72 };
string json = JsonRecordSerializer.Serialize(source, writeIndented: true);
Console.WriteLine(json);

PlayerSnapshot restored = new();
WeaponSnapshot existingWeapon = restored.Weapon;
existingWeapon.Ammo = 2;
JsonRecordSerializer.Populate(restored, json);

Console.WriteLine($"Health: {restored.Health}; ammo: {restored.Weapon.Ammo}");
Console.WriteLine($"Same weapon: {ReferenceEquals(existingWeapon, restored.Weapon)}");

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
```

<xref:Chronicler.Serialization.JsonRecordSerializer.Serialize(Chronicler.IRecordable,System.Boolean)>
returns the JSON below. The weapon record is present, and the writer omits
`ammo` because its value equals the declared default of 30.

```json
{
  "health": 72,
  "weapon": {}
}
```

`Populate` returns `void` and updates `restored` in place. Inside the present
`weapon` record, the missing `ammo` entry resets the existing weapon from 2
to 30. The last two lines show the restored values and confirm the weapon
identity.

```text
Health: 72; ammo: 30
Same weapon: True
```

## 4. Choose another transport when needed

The standard package exposes
<xref:Chronicler.Serialization.MemoryPackRecordSerializer> over the same
`RecordData` schema. Add these lines before the type declarations in
`Program.cs` to try the same state transfer as a binary payload.

```csharp
byte[] payload = MemoryPackRecordSerializer.Serialize(source);

PlayerSnapshot restoredFromBinary = new();
MemoryPackRecordSerializer.Populate(restoredFromBinary, payload);
Console.WriteLine(restoredFromBinary.Health); // 72
```

Use the standard package for this step. The Lean package includes the JSON
serializer and compiles out the MemoryPack transport.

<a id="5-use-the-right-recording-lane"></a>

## 5. Choose a recording helper

Use the
[recording helper table](serialization-model.md#choose-by-state-ownership) to
choose an API for each field and see how it handles a missing or null entry.

## Next steps

- Read [Values, owned state, and links](serialization-model.md) for object
  ownership, stable IDs, deferred links, and state-backed JSON helpers.
- Read [Deterministic record hashes](record-hashes.md) for replay and
  conformance signals.
- Read [Simulation timing](simulation-timing.md) for exact timestamps,
  durations, frame deadlines, and explicit clock advancement and restore.
- Read the
  [migration guide](https://github.com/mrdav30/Chronicler/blob/main/docs/MIGRATION.md)
  when upgrading an existing consumer.
- Browse the <xref:Chronicler> recording contracts,
  <xref:Chronicler.Serialization> transports, <xref:Chronicler.Hashing> hash
  APIs, and <xref:Chronicler.Timing> timing APIs.
