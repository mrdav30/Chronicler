# Chronicler.Diagnostics

Dependency-free diagnostic channels, lazy logging, and exception guards for
engine-agnostic .NET libraries and hosts. Targets `netstandard2.1` and `net10.0`.
The same package supports standard and Lean applications; it does not depend on
Chronicler.Core, JSON, or MemoryPack.

```csharp
using Chronicler.Diagnostics;

var channel = new DiagnosticChannel("MyLibrary")
{
    MinimumLevel = DiagnosticLevel.Warning,
    Sink = WriteDiagnostic
};

channel.Warn($"Buffer capacity reached {capacity}.");
ChronicleGuard.ThrowIfNull(settings);

static void WriteDiagnostic(in DiagnosticEvent diagnostic)
    => Console.WriteLine($"[{diagnostic.Level}] {diagnostic.Channel}: {diagnostic.Message}");
```

The example assumes the host supplies `capacity` and `settings`. Interpolated
messages skip formatted expressions when their channel level is disabled.
Interpolated exception messages are built only when the guard throws.

Channels start disabled with a no-op sink. `DiagnosticLogger` provides an optional
base for library-specific facades, with normal/debug channels and a configurable
formatter and output handler. Libraries retain ownership of their channel names.
Diagnostics are host-facing observations, not serialized state or replay hashes.

See the [diagnostics guide](https://mrdav30.github.io/Chronicler/guides/diagnostics.html)
and [migration guide](https://github.com/mrdav30/Chronicler/blob/main/docs/MIGRATION.md).
