---
title: Diagnostics and guards
description: Use dependency-free diagnostic channels, lazy messages, and exception guards.
---

# Diagnostics and guards

Warn when a buffer reaches its 128-entry capacity and route the warning to a
sink your application controls. Then use a guard to reject a request for 256
entries. Both tasks use the independent `Chronicler.Diagnostics` package.

```bash
dotnet add package Chronicler.Diagnostics
```

## Configure a channel

A <xref:Chronicler.Diagnostics.DiagnosticChannel> gives events a name and
filters them by level. It starts disabled with a no-op **sink**, the callback
that receives enabled events. Set its minimum level to `Warning` and send events
to the console. Run this in a console app that references the package.

```csharp
using System;
using Chronicler.Diagnostics;

var channel = new DiagnosticChannel("Buffer")
{
    MinimumLevel = DiagnosticLevel.Warning,
    Sink = WriteDiagnostic
};

int capacity = 128;
channel.Info($"Checking capacity {capacity}.", source: "Buffer");
channel.Warn($"Buffer capacity reached {capacity}.", source: "Buffer");

static void WriteDiagnostic(in DiagnosticEvent diagnostic)
{
    string label = $"[{diagnostic.Level}] {diagnostic.Channel}";
    Console.WriteLine($"{label}: {diagnostic.Message}");
}
```

Only the warning reaches the sink.

```text
[Warning] Buffer: Buffer capacity reached 128.
```

`Info`, `Warn`, `Error`, and dynamic-level `Log` accept both strings and custom
interpolated-string handlers. With C# 10 or later, interpolated calls select the
handler overload. The handler skips evaluating the disabled `Info` call's
`{capacity}` expression. String arguments are evaluated before the helper
receives them. When a source is omitted, both paths derive it from caller file
and method information.

### Older-language callers

C# 9 callers can use the same helper names through the string overloads. Their
interpolation expressions run before the call, even when that level is disabled.
Use an explicit level check to avoid that work:

```csharp
if (channel.IsEnabled(DiagnosticLevel.Warning))
    channel.Warn($"Buffer capacity reached {capacity}.", source: "Buffer");
```

The same package serves standard and Lean consumers. It targets `netstandard2.1`
and `net10.0` without Core, transport, or engine dependencies. Consumers
reference compiled assemblies. The `netstandard2.1` implementation uses C# 11
and carries the public compiler attributes needed by downstream handler APIs.
The .NET 10 implementation and tests use C# 14. Host runtime, AOT, and linker
support depend on the application's toolchain.

## Use a library-owned facade

<xref:Chronicler.Diagnostics.DiagnosticLogger> is an optional base for a
library-owned facade. It provides normal/debug channels and configurable
formatting and output. Its normal channel starts at Warning; debug logging is
disabled. Its default output writes errors to stderr and other levels to stdout.

Keep library-specific channel names and facades with their owning library. For
example, SwiftCollections retains `SwiftCollectionDiagnostics.Shared`, and
GridForge retains `GridForgeLogger`. Diagnostics do not become recorded state or
contribute to record hashes. Keep simulation decisions out of logging callbacks
and interpolation expressions.

## Guard invalid inputs

Use <xref:Chronicler.Diagnostics.ChronicleGuard> for argument, range, state,
disposal, and key checks. Insert this before the `WriteDiagnostic` declaration
to reuse the buffer capacity. The check throws `ArgumentOutOfRangeException`
with parameter name `requested`, actual value `256`, and a message that includes
the capacity of 128.

```csharp
int requested = 256;
ChronicleGuard.ThrowIfArgumentOutOfRange(
    requested > capacity, requested, nameof(requested),
    $"Requested {requested} entries with capacity {capacity}.");
```

With C# 10 or later, the handler builds the message only when the guard fails on
either target framework. Older callers can use string overloads and pass
explicit parameter names, for example
`ChronicleGuard.ThrowIfNull(settings, nameof(settings))`. The caller evaluates a
preformatted message before invoking the guard.

See the
[migration guide](https://github.com/mrdav30/Chronicler/blob/main/docs/MIGRATION.md)
when upgrading from `SwiftCollections.Diagnostics`.
