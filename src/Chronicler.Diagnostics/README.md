# Chronicler.Diagnostics

Dependency-free diagnostic channels, lazy logging, and exception guards for
engine-agnostic .NET libraries and hosts. Targets `netstandard2.1` and
`net10.0`. The same package supports standard and Lean applications; it does not
depend on Chronicler.Core, JSON, or MemoryPack.

```csharp
using System;
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

The example assumes the host supplies `capacity` and `settings`. With C# 10 or
later, interpolated messages skip formatted expressions when their channel level
is disabled, and interpolated exception messages are built only when the guard
throws.

C# 9 callers use the string logging overloads. Their interpolation runs before
the call; guard it with `channel.IsEnabled(DiagnosticLevel.Warning)` when
formatting should be skipped. Pass explicit guard parameter names when needed:
`ChronicleGuard.ThrowIfNull(settings, nameof(settings))`.

Channels start disabled with a no-op sink. `DiagnosticLogger` provides an
optional base for library-specific facades, with normal/debug channels and a
configurable formatter and output handler. Libraries retain ownership of their
channel names. Diagnostics are host-facing observations, not serialized state or
replay hashes.

See the
[diagnostics guide](https://mrdav30.github.io/Chronicler/guides/diagnostics.html)
and
[migration guide](https://github.com/mrdav30/Chronicler/blob/main/docs/MIGRATION.md).
