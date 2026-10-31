---
title: Diagnostics and guards
description: Use dependency-free diagnostic channels, lazy messages, and exception guards.
---

# Diagnostics and guards

`Chronicler.Diagnostics` is an independent package for host-facing diagnostics
and exception guards. It targets `netstandard2.1` and `net10.0` without
depending on `Chronicler.Core`, JSON, MemoryPack, or an engine. Standard and
Lean consumers use the same package.

```bash
dotnet add package Chronicler.Diagnostics
```

## Configure a channel

<xref:Chronicler.Diagnostics.DiagnosticChannel> starts disabled with a no-op
sink. The host chooses the minimum level and output destination:

```csharp
using System;
using Chronicler.Diagnostics;

var channel = new DiagnosticChannel("MyLibrary")
{
    MinimumLevel = DiagnosticLevel.Warning,
    Sink = WriteDiagnostic
};

int capacity = 128;
channel.Warn($"Buffer capacity reached {capacity}.", source: "Buffer");

static void WriteDiagnostic(in DiagnosticEvent diagnostic)
    => Console.WriteLine($"[{diagnostic.Level}] {diagnostic.Channel}: {diagnostic.Message}");
```

`Info`, `Warn`, `Error`, and dynamic-level `Log` accept both strings and custom
interpolated-string handlers. With C# 10 or later, interpolated calls select the
handler overload and skip formatted expressions when disabled. String arguments
are already formatted when the helper receives them. When a source is omitted,
both paths derive it from caller file and method information.

### Older-language callers

C# 9 callers can use the same helper names through the string overloads. Their
interpolation expressions run before the call, even when that level is disabled.
Use an explicit level check to avoid that work:

```csharp
if (channel.IsEnabled(DiagnosticLevel.Warning))
    channel.Warn($"Buffer capacity reached {capacity}.", source: "Buffer");
```

The libraries are distributed as compiled assemblies. The `netstandard2.1`
implementation uses C# 11; callers do not need to compile its source syntax. The
.NET 10 implementation and tests use C# 14. Host runtime, AOT, and linker
support also depend on the consuming application's toolchain; validate those
environments as part of your integration.

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

<xref:Chronicler.Diagnostics.ChronicleGuard> offers common argument, range,
state, disposal, and key guards. It preserves parameter names and routes
exceptions through separate throw methods. Its interpolated overloads build
messages only when the guard fails:

```csharp
int capacity = 128;
int requested = 256;
ChronicleGuard.ThrowIfArgumentOutOfRange(
    requested > capacity, requested, nameof(requested),
    $"Requested {requested} entries; capacity is {capacity}.");
```

The handler overloads support the same lazy-message behavior on both target
frameworks when the caller uses C# 10 or later. Older callers can use the string
guard overloads and pass explicit parameter names where needed, for example
`ChronicleGuard.ThrowIfNull(settings, nameof(settings))`. A preformatted string
guard message is evaluated before the guard runs. The netstandard assembly
carries the public compiler attributes needed by downstream handler APIs.

See the
[migration guide](https://github.com/mrdav30/Chronicler/blob/main/docs/MIGRATION.md)
when upgrading from `SwiftCollections.Diagnostics`.
